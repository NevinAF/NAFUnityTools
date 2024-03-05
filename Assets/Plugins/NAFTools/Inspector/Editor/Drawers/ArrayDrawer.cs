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
					Debug.LogError(_message + "\n" + _property.propertyPath + " is not " + _endProperty.propertyPath);
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
				tree.Activate(property);
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
			var tree = Tree(property);
			tree.PropertyLabel = label;
			tree.OnGUI(position);
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			var tree = Tree(property);
			tree.PropertyLabel = label;
			return tree.GetHeight();
		}

		~DefaultDrawer()
		{
			foreach (var tree in _trees.Values)
				tree.Return();

			// if (_tree != null)
			// 	PropertyCache.TreePool.Return(_tree);
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
	[CustomEditor(typeof(UnityEngine.Object), true)]
	public class ArrayDrawer : UnityEditor.Editor
	{
		public static ArrayDrawer? Current { get; private set; }
		public uint ValidDrawId { get; private set; }
		public EventType LastEventType { get; private set; }

		private bool i_repainting;
		private static bool s_repainting;

		public void QueueRepaint()
		{
			if (i_repainting || s_repainting)
				return;

			i_repainting = true;
			EditorApplication.delayCall += () => {
				if (i_repainting)
					Repaint();
			};
		}

		public static void RepaintAll()
		{
			if (s_repainting)
				return;

			// Get all instances
			EditorApplication.delayCall += () => {
				ActiveEditorTracker tracker = ActiveEditorTracker.sharedTracker;
				Editor[] editor = tracker.activeEditors;

				for (int i = 0; i < editor.Length; i++)
					if (editor[i] is ArrayDrawer ad && !ad.i_repainting)
						ad.Repaint();
			};
		}

		protected virtual void OnEnable()
		{
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
			ChangesCache.OnClear += InvalidateDrawers;

			LastEventType = EventType.Ignore;
			ValidDrawId = 1;
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

			UnityEngine.Debug.Log(Event.current.type + ": " + EditorGUILayout.GetControlRect(true, 0).ToString());

			if (LastEventType == Event.current.type || (LastEventType != EventType.Layout && LastEventType != EventType.Repaint))
				ValidDrawId++;
			LastEventType = Event.current.type;

			OverrideDoDrawDefaultInspector();
			Current = null;
		}

		internal bool OverrideDoDrawDefaultInspector()
		{
			using var _ = new LocalizationGroup(target);

			EditorGUI.BeginChangeCheck();
			serializedObject.UpdateIfRequiredOrScript();

			for (int index = 0; index < _trees.Count; index++)
			{
				using (new EditorGUI.DisabledScope("m_Script" == _trees[index].Property.propertyPath))
					_trees[index].OnGUILayout();
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