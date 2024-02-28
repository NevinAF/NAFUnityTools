
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq.Expressions;
	using System.Reflection;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System.Threading.Tasks;
	using System.Threading;
	using System.Collections;
	using NAF.ExpressionCompiler;
	using UnityEditor.Overlays;
	using System.Runtime.Remoting.Messaging;
	using System.Linq;
	using System.Collections.Concurrent;

#nullable enable

	public static class Assertions
	{
		public struct EndsSerializedProperty : IDisposable
		{
			private SerializedProperty? _property;
			private SerializedProperty? _endProperty;
			private readonly string _message;

			public EndsSerializedProperty(SerializedProperty property, string message)
			{
				_property = property;
				_endProperty = property.GetEndProperty();
				_message = message;

				AssemblyReloadEvents.beforeAssemblyReload += ClearProperty;
			}

			private void ClearProperty()
			{
				_property = null;
				_endProperty = null;
			}

			public void Dispose()
			{
				AssemblyReloadEvents.beforeAssemblyReload -= ClearProperty;

				if (_property == null || _endProperty == null)
					return;

				if (!SerializedProperty.DataEquals(_property, _endProperty))
				{
					Debug.LogError(_message);
				}
			}
		}
	}

	[CustomPropertyDrawer(typeof(object), true)]
	[CustomPropertyDrawer(typeof(string), true)]
	public class DefaultDrawer : PropertyDrawer
	{
		private Dictionary<string, PropertyTree> _trees = new Dictionary<string, PropertyTree>();
		// private PropertyTree? _tree;

		private PropertyTree Tree(SerializedProperty property)
		{
			string path = property.propertyPath;
			if (!_trees.TryGetValue(path, out PropertyTree tree))
			{
				tree = PropertyCache.TreePool.Get();
				tree.Reset(property.Copy());
				_trees.Add(path, tree);
			}
			return tree;
			// if (_tree == null)
			// {
			// 	_tree = PropertyCache.TreePool.Get();
			// 	_tree.Reset(property.Copy());
			// }
			// return _tree;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Tree(property).OnGUI(position, property, label);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			return Tree(property).IterateGetHeight(property, label);
		}

		~DefaultDrawer()
		{
			foreach (var tree in _trees.Values)
				tree.Return();

			// if (_tree != null)
			// 	PropertyCache.TreePool.Return(_tree);
		}
	}

	public class PropertyTree
	{
		public static class AttributeListBag
		{
			public static ConcurrentBag<List<PropertyAttribute>> Bag = new ConcurrentBag<List<PropertyAttribute>>();

			public static List<PropertyAttribute> Get()
			{
				if (Bag.TryTake(out var list))
					return list;
				return new List<PropertyAttribute>();
			}

			public static void Return(List<PropertyAttribute> list)
			{
				list.Clear();
				Bag.Add(list);
			}
		}

		public readonly static GUIContent _currentLabel = new GUIContent();

		private enum ExceptionLocation
		{
			None,
			Initialize,
		}

	#if NAF_DEBUG
		private string? _pathForValidation;
	#endif

		private FieldInfo? _fieldInfo;
		private readonly List<NAFPropertyDrawer> drawers = new List<NAFPropertyDrawer>();
		// private bool useBuiltinDrawer;
		private object? propertyHandler;
		private string? tooltip;
		private readonly List<PropertyTree> children = new List<PropertyTree>();
		private int maxDepth;
		private ArrayDrawer? _editor;

		public FieldInfo? FieldInfo => _fieldInfo;

		public void AddDrawer(NAFPropertyDrawer drawer)
		{
			if (drawers.Count != 0 && drawers[drawers.Count - 1].EndsDrawing)
			{
				UnityEngine.Debug.LogWarning("Drawer " + drawers[drawers.Count - 1].GetType().Name + " ends property drawing, but there are more drawers trying to be drawn after it: " + drawer.GetType().Name);
			}

			if (_editor == null && drawer.OnlyDrawWithEditor)
				return;

			drawers.Add(drawer);
		}

		public void Reset(in SerializedProperty property, ArrayDrawer? editor = null)
		{
		#if NAF_DEBUG
			_pathForValidation = property.propertyPath;
			using var _ = new Assertions.EndsSerializedProperty(property, "PropertyTree.Reset did not end with the same property.");
		#endif

			bool isArrayProperty = property.isArray && property.propertyType != SerializedPropertyType.String;
			_fieldInfo = UnityInternals.ScriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(property, out Type? propertyType);
			_editor = editor;

			// Load Field Attributes...
			if (_fieldInfo != null)
			{
				foreach (PropertyAttribute attribute in _fieldInfo.FieldType.GetCustomAttributes<PropertyAttribute>(true))
					propertyAttributesBuffer.Add(attribute);

				foreach (PropertyAttribute attribute in _fieldInfo.GetCustomAttributes<PropertyAttribute>(true))
					propertyAttributesBuffer.Add(attribute);
			}
			propertyAttributesBuffer.Sort(propertyAttributeComparer);

			List<PropertyAttribute> builtinAttributes = AttributeListBag.Get();
			NAFPropertyDrawer? drawer;

			for (int i = 0; i < propertyAttributesBuffer.Count; i++)
			{
				PropertyAttribute attribute = propertyAttributesBuffer[i];

				bool useNAF = false;
				// Check if the attribute should be drawn on this property...
				if (attribute is IArrayPropertyAttribute aa)
					useNAF = IArrayPropertyAttribute.DrawOnProperty(aa, property);
				else useNAF = !isArrayProperty;

				// Create Property Drawers for the attributes...

				if (_editor != null)
				{
					if (attribute is TooltipAttribute tooltipAttribute)
					{
						if (!property.IsElementOfArray())
							tooltip = tooltipAttribute.tooltip;
						continue;
					}

					if (attribute is SpaceAttribute spaceAttribute || attribute is HeaderAttribute)
						useNAF = true;
				}

				if (useNAF && NAFPropertyDrawer.TryGet(this, property, attribute.GetType(), attribute, out drawer))
					AddDrawer(drawer);
				else builtinAttributes.Add(attribute);
			}
			propertyAttributesBuffer.Clear();

			// Check if this type itself has a drawer...
			if (NAFPropertyDrawer.TryGet(this, property, propertyType, null, out drawer))
				AddDrawer(drawer);

			BuildPropertyHandler(property, builtinAttributes, propertyType);
			AttributeListBag.Return(builtinAttributes);

			ChangesCache.OnClear += Invalidate;

			if (DefaultableProperty(property) || propertyHandler != null)
			{
				property.NextVisible(false);
				return;
			}

			// Load Children...
			int depth = property.depth;
			maxDepth = depth;

			if (property.NextVisible(true))
			{
				while (property.depth > depth)
				{
					PropertyTree child = PropertyCache.TreePool.Get();

					child.Reset(property, editor);
					children.Add(child);
					maxDepth = Mathf.Max(maxDepth, child.maxDepth);

					if (!UnityInternals.SerializedProperty_isValid(property))
						break;
				}
			}
		}

		private void BuildPropertyHandler(SerializedProperty property, List<PropertyAttribute> builtinAttributes, Type? propertyType)
		{
			bool useBuiltinDrawer = false;
			bool CheckBuiltinDrawer(Type builtinDrawerType)
			{
				if (property.isArray && property.propertyType != SerializedPropertyType.String)
				{
					if (!typeof(DecoratorDrawer).IsAssignableFrom(builtinDrawerType))
						// TODO OR !propertyattibute.applytocollection
						return false;
				}

				if (FieldInfo?.FieldType.IsArrayOrList() ?? false && !typeof(PropertyDrawer).IsAssignableFrom(builtinDrawerType))
					return false;

				return true;
			}

			for (int i = 0; i < builtinAttributes.Count; i++)
			{
				PropertyAttribute attribute = builtinAttributes[i];
				Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(property, attribute.GetType());

				if (builtinDrawerType == null)
				{
					// TODO: Make this a drawer?
					Debug.LogWarning($"No drawer found for attribute {attribute.GetType().Name}.");
					continue;
				}

				if (CheckBuiltinDrawer(builtinDrawerType))
				{
					useBuiltinDrawer = true;
					break;
				}
			}

			// See if the property itself has a drawer
			bool useSelfDrawer = false;
			if (propertyType != null)
			{
				Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(property, propertyType);
				if (builtinDrawerType != null && builtinDrawerType != typeof(DefaultDrawer) && CheckBuiltinDrawer(builtinDrawerType))
				{
					useBuiltinDrawer = true;
					useSelfDrawer = true;
				}
			}

			if (!useBuiltinDrawer)
				return;

			if (FieldInfo == null) throw new InvalidOperationException("FieldInfo is null!");

			UnityEngine.Debug.Log($"Using builtin drawer for {FieldInfo.Name} of type {FieldInfo.FieldType.Name}. Attributes: {string.Join(", ", builtinAttributes.Select(a => a.GetType().Name))}");

			propertyHandler = Activator.CreateInstance(UnityInternals.PropertyHandlerType);

			for (int i = 0; i < builtinAttributes.Count; i++)
			{
				PropertyAttribute attribute = builtinAttributes[i];
				UnityInternals.PropertyDrawer_HandleAttribute(propertyHandler, property, attribute, FieldInfo, FieldInfo.FieldType);
			}

			if (useSelfDrawer && propertyType != null)
				UnityInternals.PropertyDrawer_HandleDrawnType(propertyHandler, property, propertyType, propertyType, FieldInfo, null);

			object propCache = _editor != null ?
				UnityInternals.Editor_m_PropertyHandlerCache(_editor) :
				UnityInternals.ScriptAttributeUtility_propertyHandlerCache;

			UnityInternals.PropertyHandlerCache_SetHandler(propCache, property, propertyHandler);
		}

		public void Return()
		{
			ChangesCache.OnClear -= Invalidate;

			_fieldInfo = null;
			tooltip = null;
			propertyHandler = null;

			for (int i = 0; i < drawers.Count; i++)
				drawers[i].Return();
			drawers.Clear();

			for (int i = 0; i < children.Count; i++)
				children[i].Return();
			children.Clear();

			PropertyCache.TreePool.Return(this);

		}

		public void OnGUILayout(SerializedProperty property, params GUILayoutOption[] options)
		{
		#if NAF_DEBUG
			if (property.propertyPath != _pathForValidation)
				throw new InvalidOperationException("PropertyTree does not match the property!: " + property.propertyPath + " != " + _pathForValidation);

			using var _ = new Assertions.EndsSerializedProperty(property, "PropertyTree.OnGUILayout() did not consume the entire property!");
		#endif

			AssertFresh();

			_currentLabel.text = property.displayName;
			_currentLabel.tooltip = tooltip;

			float height = InPlaceGetHeight(property, _currentLabel);
			Rect r = EditorGUILayout.GetControlRect(true, height, options);
			OnGUI(r, property, _currentLabel);

			AssertFresh();
		}

		private int _iterator;

		[System.Diagnostics.Conditional("NAF_DEBUG")]
		public void AssertFresh()
		{
			if (_iterator != 0)
				throw new InvalidOperationException("PropertyTree expected iterator context to be fresh!");
		}

		public static bool DefaultableProperty(SerializedProperty property)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Vector3:
				case SerializedPropertyType.Vector2:
				case SerializedPropertyType.Vector3Int:
				case SerializedPropertyType.Vector2Int:
				case SerializedPropertyType.Rect:
				case SerializedPropertyType.RectInt:
				case SerializedPropertyType.Bounds:
				case SerializedPropertyType.BoundsInt:
				case SerializedPropertyType.Hash128:
				case SerializedPropertyType.Quaternion:
					return true;
			}

			return !property.hasVisibleChildren;
		}


		public void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
		#if NAF_DEBUG
			if (property.propertyPath != _pathForValidation)
				throw new InvalidOperationException("PropertyTree does not match the property!: " + property.propertyPath + " != " + _pathForValidation);

			using var _ = new Assertions.EndsSerializedProperty(property, "PropertyTree.OnGUI() did not consume the entire property!");
		#endif

			if (_iterator < drawers.Count)
			{
				NAFPropertyDrawer drawer = drawers[_iterator];

				_iterator++;
				drawer.DoGUI(position, property, label);
				_iterator--;
				return;
			}

			if (propertyHandler != null) // Arrays always use this...
			{
				bool showChildren = UnityInternals.PropertyDrawer_OnGUI(propertyHandler, position, property, label, true);
				property.NextVisible(false); // TODO? Right now, even if this opens, do not draw children?
				return;
			}

			// if (!property.isExpanded || DefaultableProperty(property))
			// {
			// 	bool showChildren = UnityInternals.EditorGUI_DefaultPropertyField(position, property, label);
			// 	property.NextVisible(false); // TODO. Right now, even if this opens, do not draw children?
			// 	return;
			// }

			position.height = UnityInternals.EditorGUI_GetSinglePropertyHeight(property, label);
			bool expanded = UnityInternals.EditorGUI_DefaultPropertyField(position, property, label);

			if (expanded == false) // This happens if the property is closed on this draw call.
			{
				property.NextVisible(false);
				return;
			}

			position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

			int origIndent = EditorGUI.indentLevel;
			int depth = property.depth;

			Span<int> childIterators = stackalloc int[maxDepth - depth];
			int lastChildDepth = 0;

			// Loop through all children and draw them using the tree...
			property.NextVisible(true);
			while (property.depth > depth)
			{
				int rel = property.depth - depth;
				if (rel > lastChildDepth)
					lastChildDepth = rel;
				else while (rel < lastChildDepth)
				{
					lastChildDepth--;
					childIterators[lastChildDepth] = 0;
				}

				EditorGUI.indentLevel = origIndent + rel;

				PropertyTree tree = this;
				for (int i = 0; i < lastChildDepth; i++)
					tree = tree.children[childIterators[i]];
				childIterators[lastChildDepth - 1]++; // Next loop will use the next child.

				_currentLabel.text = property.displayName;
				_currentLabel.tooltip = tree.tooltip;

				tree.AssertFresh();
				position.height = tree.InPlaceGetHeight(property, _currentLabel);
				tree.OnGUI(position, property, _currentLabel);
				tree.AssertFresh();

				if (!UnityInternals.SerializedProperty_isValid(property))
					break;

				position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
			}

			EditorGUI.indentLevel = origIndent;
		}

		public float InPlaceGetHeight(SerializedProperty property, GUIContent label)
		{
			if (_iterator < drawers.Count)
			{
				NAFPropertyDrawer drawer = drawers[_iterator];

				if (drawer.LastHeightValid)
					return drawer.LastHeight;

				// Fall through to DoGetHeight...
			}
			else if (propertyHandler == null && (!property.isExpanded || DefaultableProperty(property)))
			{
				return UnityInternals.EditorGUI_GetSinglePropertyHeight(property, label);
			}

			return DoGetHeight(property.Copy(), label);
		}

		public float IterateGetHeight(SerializedProperty property, GUIContent label)
		{
			if (_iterator < drawers.Count)
			{
				NAFPropertyDrawer drawer = drawers[_iterator];

				if (drawer.LastHeightValid)
				{
					property.NextVisible(false);
					return drawer.LastHeight;
				}
			}

			return DoGetHeight(property, label);
		}

		private float DoGetHeight(SerializedProperty property, GUIContent label)
		{
		#if NAF_DEBUG
			if (property.propertyPath != _pathForValidation)
				throw new InvalidOperationException("PropertyTree does not match the property!: " + property.propertyPath + " != " + _pathForValidation);

			using var _ = new Assertions.EndsSerializedProperty(property, "PropertyTree.OnGUI() did not consume the entire property!");
		#endif

			if (_iterator < drawers.Count)
			{
				NAFPropertyDrawer drawer = drawers[_iterator];

				_iterator++;
				float h = drawer.DoGetHeight(property, label);
				_iterator--;

				return h;
			}

			if (propertyHandler != null) // Arrays always use this...
			{
				float h = UnityInternals.PropertyDrawer_GetHeight(propertyHandler, property, label);

				if (property.IsElementOfArray())
					h += EditorGUIUtility.standardVerticalSpacing;

				property.NextVisible(false);
				return h;
			}

			float height = UnityInternals.EditorGUI_GetSinglePropertyHeight(property, label);

			if (!property.isExpanded || DefaultableProperty(property))
			{
				property.NextVisible(false);
				return height;
			}

			if (property.IsElementOfArray())
				height += EditorGUIUtility.standardVerticalSpacing;

			int depth = property.depth;

			// Loop through all children and draw them using the tree...
			int childIterator = 0;
			property.NextVisible(true);
			while (property.depth > depth)
			{
				PropertyTree tree = children[childIterator++];

				tree.AssertFresh();
				height += tree.IterateGetHeight(property, TempUtility.Content(property.displayName, tooltip: tree.tooltip));
				tree.AssertFresh();

				if (!UnityInternals.SerializedProperty_isValid(property))
					break;

				height += EditorGUIUtility.standardVerticalSpacing;
			}

			return height;
		}

		private void Invalidate()
		{
			bool result = false;
			for (int i = 0; i < drawers.Count; i++)
				result &= drawers[i].Invalidate();

			if (result)
				Repaint();
		}

		public void Repaint()
		{
			if (_editor != null)
				_editor.Repaint();
			else // Force the whole inspector to repaint
				UnityEditorInternal.InternalEditorUtility.RepaintAllViews();
		}

		private static Comparer<PropertyAttribute> propertyAttributeComparer = Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));
		private static List<PropertyAttribute> propertyAttributesBuffer = new List<PropertyAttribute>();

		public static List<PropertyTree> BuildTree(SerializedObject serializedObject, ArrayDrawer? editor)
		{
			var extras = serializedObject.targetObject.GetType().GetCustomAttributes<PropertyAttribute>(true);

			List<PropertyTree> trees = new List<PropertyTree>();
			SerializedProperty iterator = serializedObject.GetIterator();
			iterator.NextVisible(true);
			do
			{
				PropertyTree tree = PropertyCache.TreePool.Get();
				propertyAttributesBuffer.AddRange(extras);
				tree.Reset(iterator, editor);
				trees.Add(tree);
			}
			while (UnityInternals.SerializedProperty_isValid(iterator));
			return trees;
		}
	}

	public static class PropertyCache
	{
		private readonly struct DrawerPoolData
		{
			public readonly ObjectPool<NAFPropertyDrawer> pool;
			public readonly bool subclasses;

			public DrawerPoolData(Type drawerType, bool subclasses)
			{
				pool = new ObjectPool<NAFPropertyDrawer>(() => (NAFPropertyDrawer)Activator.CreateInstance(drawerType));
				this.subclasses = subclasses;
			}
		}

		private readonly static Lazy<Dictionary<Type, DrawerPoolData>> _drawerTypeDictionary = new(BuildDrawerTypeForTypeDictionary, true);

		private readonly static ObjectPool<NAFPropertyDrawer> _reorderableDrawerPool = new ObjectPool<NAFPropertyDrawer>(() => new ReorderableDrawer());

		private static Dictionary<Type, DrawerPoolData> BuildDrawerTypeForTypeDictionary()
		{
			var tempDictionary = new Dictionary<Type, DrawerPoolData>();
			foreach (var drawerType in TypeCache.GetTypesDerivedFrom<NAFPropertyDrawer>())
			{
				var customPropertyDrawers = drawerType.GetCustomAttributes<CustomPropertyDrawer>(true);
				foreach (CustomPropertyDrawer drawerData in customPropertyDrawers)
				{
					(Type target, bool subclasses) = UnityInternals.CustomPropertyDrawer_m_Type_AND_m_UseForChildren(drawerData);
					if (!tempDictionary.TryGetValue(target, out var otherDrawer))
						tempDictionary.Add(target, new DrawerPoolData(drawerType, subclasses));
					else {
						UnityEngine.Debug.LogWarning($"Multiple drawers found for type {target.Name}!");
					}
				}
			}

			return tempDictionary;
		}
	
		public static ObjectPool<NAFPropertyDrawer>? GetDrawerPoolForType(Type type)
		{
			Type it = type;
			while (it != null)
			{
				if (_drawerTypeDictionary.Value.TryGetValue(it, out var value))
				{
					if (value.subclasses || it == type)
						return value.pool;
				}
				it = it.BaseType;
			}

			if (type.IsArrayOrList())
				return _reorderableDrawerPool;

			return null;
		}

		public static ObjectPool<PropertyTree> TreePool = new ObjectPool<PropertyTree>(() => new PropertyTree());
	}

	// // Draws all array properties with a custom drawer, everything else is drawn normally
	[CanEditMultipleObjects]
	// [CustomEditor(typeof(UnityEngine.Object), true)]
	public class ArrayDrawer : UnityEditor.Editor
	{
		public static ArrayDrawer? Current { get; private set; }
		public int LayoutDrawID { get; private set; }

		protected virtual void OnEnable()
		{
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
			ChangesCache.OnClear += InvalidateDrawers;

			LayoutDrawID = 0;
			_trees = PropertyTree.BuildTree(serializedObject, this);
		}

		private void OnDisable()
		{
			AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
			ChangesCache.OnClear -= InvalidateDrawers;

			if (_trees != null)
			{
				foreach (var tree in _trees)
					tree.Return();
				_trees.Clear();
			}
		}

		private void OnAfterAssemblyReload()
		{
		}

		private void InvalidateDrawers()
		{
		}

		protected List<PropertyTree> _trees;
	
		public override void OnInspectorGUI()
		{
			if (target == null || serializedObject == null)
				return;

			// Update the inspector to reflect the left padding
			Current = this;

			UnityEngine.Debug.Log(Event.current.type);
			if (Event.current.type == EventType.Layout)
				LayoutDrawID++;

			OverrideDoDrawDefaultInspector();
			Current = null;
		}

		internal bool OverrideDoDrawDefaultInspector()
		{
			using var _ = new LocalizationGroup(target);

			EditorGUI.BeginChangeCheck();
			serializedObject.UpdateIfRequiredOrScript();

			SerializedProperty property = serializedObject.GetIterator();
			property.NextVisible(true);

			for (int index = 0; ; index++)
			{
				using (new EditorGUI.DisabledScope("m_Script" == property.propertyPath))
					_trees[index].OnGUILayout(property);

				if (!UnityInternals.SerializedProperty_isValid(property))
				{
			#if NAF_DEBUG
					if (index != _trees.Count - 1)
						UnityEngine.Debug.LogWarning("Property finished before all trees were drawn!");
					break;
				}
				else if (index == _trees.Count - 1)
				{
					UnityEngine.Debug.LogWarning("Property did not finish before all trees were drawn!");
			#endif
					break;
				}
			}

			serializedObject.ApplyModifiedProperties();
			bool result = EditorGUI.EndChangeCheck();

			MonoBehaviour? monoBehaviour = target as MonoBehaviour;
			if (monoBehaviour != null && UnityInternals.AudioUtil_HasAudioCallback(monoBehaviour) && UnityInternals.AudioUtil_GetCustomFilterChannelCount(monoBehaviour) > 0)
			{
				UnityInternals.Editor_DrawAudioFilter(this, monoBehaviour);
			}

			return result;
		}

		// public Task InjectArrayProperties(SerializedObject serializedObject, PropertyAttribute[] additionalDrawers = null)
		// {
		// 	var iterator = serializedObject.GetIterator();

		// 	bool hasAdditionalDrawers = additionalDrawers != null && additionalDrawers.Length > 0;
		// 	List<Task> tasks = new List<Task>();

		// 	// Iterate over all properties, and inject property handlers for any 
		// 	while (iterator.NextVisible(true))
		// 	{
		// 		if (iterator.IsElementOfArray())
		// 		{
		// 			if (!iterator.propertyPath.EndsWith(".Array.data[0]"))
		// 				// Skip to the end of the array.
		// 				do { if (!iterator.NextVisible(false)) break; }
		// 				while(iterator.IsElementOfArray());
		// 			else continue;
		// 		}

		// 		bool isArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String;
		// 		if (hasAdditionalDrawers || isArray)
		// 			tasks.Add(InjectPropertyHandler(iterator, isArray, additionalDrawers));
		// 	}

		// 	return Task.WhenAll(tasks);
		// }

		// private static Func<UnityEditor.Editor, object> editor_propertyHandlerCache;
		// private static Action<object, SerializedProperty, object> propertyCache_SetHandler;
		// private static Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type> propertyHandler_HandleAttribute;
		// private static Func<object, List<PropertyDrawer>> propertyHandler_PropertyDrawers;
		// private static Func<object, List<DecoratorDrawer>> propertyHandler_DecoratorDrawers;
		// private static Type propertyHandlerType;

		// public Task InjectPropertyHandler([NotNull] SerializedProperty property)
		// {
		// 	if (propertyHandlerType == null)
		// 		CompileReflectionTargets();

		// 	int precount = customDrawers.Count;

		// 	property = property.Copy();

		// 	// object propertyCache = editor_propertyHandlerCache(this);
		// 	FieldInfo fieldInfo = scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(property);

		// 	object propertyHandler = Activator.CreateInstance(propertyHandlerType);
		// 	var propertyAttributes = GetFieldAttributes(fieldInfo, additionalDrawers);
		// }



		public static void CompileReflectionTargets()
		{

			// internal static PropertyHandlerCache propertyHandlerCache
			// FieldInfo propertyCacheField =typeof(UnityEditor.Editor).GetField("m_PropertyHandlerCache", bindings);
			// ParameterExpression editorParameter = Expression.Parameter(typeof(UnityEditor.Editor), "editor");
			// Expression propertyCacheFieldAccess = Expression.Field(editorParameter, propertyCacheField);
			// editor_propertyHandlerCache = Expression.Lambda<Func<UnityEditor.Editor, object>>(propertyCacheFieldAccess, editorParameter).Compile();

			// internal static void SetHandler(SerializedProperty property, PropertyHandler handler)
			// MethodInfo setHandlerMethod = propertyCacheField.FieldType.GetMethod("SetHandler", bindings);

			// ParameterInfo[] parameters = setHandlerMethod.GetParameters();
			// propertyHandlerType = parameters[1].ParameterType;

			// ParameterExpression propertyCacheParameter = Expression.Parameter(typeof(object), "propertyCache");
			// ParameterExpression propertyParameter = Expression.Parameter(typeof(SerializedProperty), "property");
			// ParameterExpression handlerParameter = Expression.Parameter(typeof(object), "handler");

			// UnaryExpression propertyCacheCast = Expression.Convert(propertyCacheParameter, propertyCacheField.FieldType);
			// UnaryExpression handlerCast = Expression.Convert(handlerParameter, propertyHandlerType);
			// Expression call = Expression.Call(propertyCacheCast, setHandlerMethod, propertyParameter, handlerCast);

			// propertyCache_SetHandler = Expression.Lambda<Action<object, SerializedProperty, object>>(call, propertyCacheParameter, propertyParameter, handlerParameter).Compile();

			// internal static void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo, Type propertyType)
			// MethodInfo handleAttributeMethod = propertyHandlerType.GetMethod("HandleAttribute", bindings);

			// ParameterExpression handlerParameter2 = Expression.Parameter(typeof(object), "handler");
			// ParameterExpression propertyParameter2 = Expression.Parameter(typeof(SerializedProperty), "property");
			// ParameterExpression attributeParameter = Expression.Parameter(typeof(PropertyAttribute), "attribute");
			// ParameterExpression fieldInfoParameter = Expression.Parameter(typeof(FieldInfo), "fieldInfo");
			// ParameterExpression propertyTypeParameter = Expression.Parameter(typeof(Type), "propertyType");

			// UnaryExpression handlerCast2 = Expression.Convert(handlerParameter2, propertyHandlerType);
			// propertyHandler_HandleAttribute = Expression.Lambda<Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type>>(
			// 	Expression.Call(handlerCast2, handleAttributeMethod, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter),
			// 	handlerParameter2, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter
			// ).Compile();

			// List<PropertyDrawer> m_PropertyDrawers;
			// FieldInfo propertyDrawersField = propertyHandlerType.GetField("m_PropertyDrawers", bindings);
			// ParameterExpression handlerParameter3 = Expression.Parameter(typeof(object), "handler");
			// UnaryExpression handlerCast3 = Expression.Convert(handlerParameter3, propertyHandlerType);
			// Expression propertyDrawersFieldAccess = Expression.Field(handlerCast3, propertyDrawersField);
			// propertyHandler_PropertyDrawers = Expression.Lambda<Func<object, List<PropertyDrawer>>>(propertyDrawersFieldAccess, handlerParameter3).Compile();

			// List<DecoratorDrawer> m_DecoratorDrawers;
			// FieldInfo decoratorDrawersField = propertyHandlerType.GetField("m_DecoratorDrawers", bindings);
			// ParameterExpression handlerParameter4 = Expression.Parameter(typeof(object), "handler");
			// UnaryExpression handlerCast4 = Expression.Convert(handlerParameter4, propertyHandlerType);
			// Expression decoratorDrawersFieldAccess = Expression.Field(handlerCast4, decoratorDrawersField);
			// propertyHandler_DecoratorDrawers = Expression.Lambda<Func<object, List<DecoratorDrawer>>>(decoratorDrawersFieldAccess, handlerParameter4).Compile();

			// internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
			

			// UnityEngine.Debug.Log("Checking delegates: " + scriptAttributeUtility_propertyHandlerCache + " | " + propertyCache_SetHandler + " | " + scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty + " | " + propertyHandler_HandleAttribute);
		}
	}
}