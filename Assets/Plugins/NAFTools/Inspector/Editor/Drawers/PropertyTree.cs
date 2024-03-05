namespace NAF.Inspector.Editor
{
	using UnityEngine;
	using UnityEditor;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Reflection;
	using System;
	using System.Linq;
	using UnityEngine.Pool;

#nullable enable

	public class PropertyTree
	{
		/// <summary> Readonly. The editor that is using this drawer, used for targeted repaint calls. </summary>
		private ArrayDrawer? _editor;
		/// <summary> Readonly. The property that this drawer is drawing. This property should never be iterated. </summary>
		private SerializedProperty? _property;
		/// <summary> Readonly. The backing field info of the <see cref="_property"/>. This may be null if the SerializedProperty does not have a field. </summary>
		private FieldInfo? _fieldInfo;
		/// <summary> Readonly. The type of the <see cref="_property"/>. When the <see cref="_property"/> is an array element, this is the element type of <see cref="_fieldInfo"/>.FieldType. Otherwise, this the same as <see cref="_fieldInfo"/>.FieldType. </summary>
		private Type? _propertyType;
		/// <summary> Readonly. The Unity PropertyHandler for this property. Only valid when there are built-in GUIDrawers that need to be drawn (oppose to NAF Drawers). This allows for custom editors and attributes without needing to conform to NAF drawers. This is used to draw properties rather than using direct calls for optimization. </summary>
		private object? _propertyHandler;
		/// <summary> Readonly Collection. The list of NAFPropertyDrawers that are used to draw this property. </summary>
		private readonly List<NAFPropertyDrawer> _drawers = new List<NAFPropertyDrawer>();
		/// <summary> Readonly Collection. The list of child properties that are used to draw this property. </summary>
		private readonly List<PropertyTree> _children = new List<PropertyTree>();
		/// <summary> Readonly. The tooltip used for the property. </summary>
		private string? _tooltip;

		/// <summary>
		/// Cache. The valid draw id from the last time the height was calculated. When the editors draw id is different, the cache is invalidated. Only used when no _propertyHandler is present and _editor is not null.
		/// </summary>
		private uint _cacheDrawID;
		/// <summary> Cache. The last height that was calculated. Only used when no _propertyHandler is present and _editor is not null. </summary>
		private float _cachedHeight;

		/// <summary> Context only. Used to store the recursion used when drawing the property (OnGUI) or getting the height (GetHeight). Always '0' when there are no draw/height calls for this class on the stack. </summary>
		private int _iterator;
		/// <summary> Context only. Used to store the current label that will be used to draw this property. Always 'null' when there are no draw/height class for this class on the stack. </summary>
		private GUIContent? _propertyLabel;
		/// <summary> The current label that should be drawn with this property. This value is reset to the property display name on every fresh (non-recursed) draw/height call. This value should always be treated as though it is a temp utility context (use immediately without other NAF draw calls). </summary>
		public GUIContent PropertyLabel
		{
			get => _propertyLabel ?? TempUtility.Content(Property.displayName, (Texture?)null, _tooltip);
			set => _propertyLabel = value;
		}

		/// <summary> The editor that is being used for this Property. This is null when the property is not being directly drawn from a <see cref="ArrayDrawer"/> editor. </summary>
		public ArrayDrawer? Editor => _editor;
		/// <summary> The property that this tree represents. Never null when this tree is valid. </summary>
		public SerializedProperty Property => _property ?? throw new InvalidOperationException("PropertyTree.Property is null!");
		/// <summary> The backing field info of the <see cref="Property"/>. This may be null if the SerializedProperty does not have a field. </summary>
		public FieldInfo? FieldInfo => _fieldInfo;
		/// <summary> The type of the <see cref="Property"/>. When the <see cref="Property"/> is an array element, this is the element type of <see cref="FieldInfo"/>.FieldType. Otherwise, this the same as <see cref="FieldInfo"/>.FieldType. </summary>
		public Type? PropertyType => _propertyType;
		/// <summary> True when the property is an array property and is not a string property. </summary>
		public bool IsArrayProperty => Property.isArray && Property.propertyType != SerializedPropertyType.String;
		/// <summary> True when the property is an array element (field info is an array, and this property represents an element within that array). </summary>
		public bool IsArrayElement => Property.propertyPath.EndsWith("]");
		/// <summary> True when any drawers do not recursively draw children. Useful for determining if further drawers/property handlers will be able to be used normally with the given attributes used. </summary>
		/// <remarks> This is not the same thing as asking if the property or its children will be drawn. For example, the HideIf attribute will never block children because the children can and should still be drawn normally when not hidden.
		/// <example> If a property uses a <see cref="SliderAttribute"/>, the slider is a custom drawer that overrides the normal property drawing for numeric types. A custom drawers or another similar attributes will not be able to draw the property because the Slider drawer does not draw children. </example>
		public bool BlocksChildren => (_drawers.Count != 0 && _drawers[_drawers.Count - 1].EndsDrawing) || _propertyHandler != null;
		/// <summary>
		/// True if the property can be drawn on a single line using Unity's default property drawer. This is the same as 'HasVisibleChildFields' internal predicate, but with a bit more clarity. This essentially filters out properties that will use a foldout drawer so we can manually draw them to prevent having to inject out of Unity's normal draw order (for optimization and prevent oddities from specific use cases).
		/// </summary>
		public bool Defaultable => Property.propertyType switch
		{
			SerializedPropertyType.Vector4 => true,
			SerializedPropertyType.Vector3 => true,
			SerializedPropertyType.Vector2 => true,
			SerializedPropertyType.Vector3Int => true,
			SerializedPropertyType.Vector2Int => true,
			SerializedPropertyType.Rect => true,
			SerializedPropertyType.RectInt => true,
			SerializedPropertyType.Bounds => true,
			SerializedPropertyType.BoundsInt => true,
			SerializedPropertyType.Hash128 => true,
			SerializedPropertyType.Quaternion => true,
			_ => !Property.hasVisibleChildren,
		};

		/// <summary> Resets this instance to a fresh state matching the given SerializedProperty. </summary>
		/// <param name="iterator">The property to draw. When this function exits, the iterator must be at the next property without entering children (iterator.NextVisible(false)).</param>
		public void Activate(in SerializedProperty iterator, ArrayDrawer? editor = null)
		{
			_editor = editor;
			_property = iterator.Copy();
			_fieldInfo = UnityInternals.ScriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(_property, out _propertyType);
			if (_editor != null)
				_cacheDrawID = _editor.ValidDrawId - 1;

			// Load Field Attributes...
			if (_fieldInfo != null)
			{
				foreach (PropertyAttribute attribute in _fieldInfo.FieldType.GetCustomAttributes<PropertyAttribute>(true))
					propertyAttributesBuffer.Add(attribute);

				foreach (PropertyAttribute attribute in _fieldInfo.GetCustomAttributes<PropertyAttribute>(true))
					propertyAttributesBuffer.Add(attribute);
			}
			propertyAttributesBuffer.Sort(propertyAttributeComparer);

			NAFPropertyDrawer? drawer;
			List<PropertyAttribute> builtinAttributes = ListPool<PropertyAttribute>.Get();

			for (int i = 0; i < propertyAttributesBuffer.Count; i++)
			{
				PropertyAttribute attribute = propertyAttributesBuffer[i];

				bool useNAF = false;
				// Check if the attribute should be drawn on this property...
				if (attribute is IArrayPropertyAttribute aa)
					useNAF = IArrayPropertyAttribute.DrawOnProperty(aa, _property);
				else useNAF = !IsArrayProperty;

				// Create Property Drawers for the attributes...

				if (_editor != null)
				{
					if (attribute is TooltipAttribute tooltipAttribute)
					{
						if (!IsArrayElement)
						{
							_tooltip = tooltipAttribute.tooltip;
						}
						continue;
					}

					if (attribute is SpaceAttribute spaceAttribute || attribute is HeaderAttribute)
						useNAF = true;
				}

				if (useNAF && NAFPropertyDrawer.TryGet(this, attribute.GetType(), attribute, out drawer))
					_drawers.Add(drawer);
				else builtinAttributes.Add(attribute);
			}
			propertyAttributesBuffer.Clear();

			if (IsArrayProperty)
			{
				// When looking at an array, always use the property handler over the automatic NAFPropertyDrawer...
				BuildPropertyHandler(builtinAttributes);
				if (!BlocksChildren)
				{
					if (!NAFPropertyDrawer.TryGet(this, _propertyType, null, out drawer))
						throw new InvalidOperationException("Array properties should always return a drawer!");
					_drawers.Add(drawer);
				}
			}
			else {
				// Check if this type itself has a drawer...
				if (NAFPropertyDrawer.TryGet(this, _propertyType, null, out drawer))
					_drawers.Add(drawer);

				// Check if there are any built-in drawers...
				BuildPropertyHandler(builtinAttributes);
			}

			ListPool<PropertyAttribute>.Release(builtinAttributes);

			ChangesCache.OnClear += Invalidate;

			if (Defaultable || BlocksChildren)
			{
				iterator.NextVisible(false);
				return;
			}

			// Load Children...
			int depth = iterator.depth;
			if (iterator.NextVisible(true))
			{
				while (iterator.depth > depth)
				{
					PropertyTree child = PropertyCache.TreePool.Get();

					child.Activate(iterator, editor);
					_children.Add(child);

					if (!UnityInternals.SerializedProperty_isValid(iterator))
						break;
				}
			}
		}

		private void BuildPropertyHandler(List<PropertyAttribute> builtinAttributes)
		{
			if (builtinAttributes.Count == 0 && Defaultable)
				return;

			bool useBuiltinDrawer = false;
			bool CheckBuiltinDrawer(Type builtinDrawerType)
			{
				if (IsArrayProperty)
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
				Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(Property, attribute.GetType());

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
			if (PropertyType != null)
			{
				Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(Property, PropertyType);
				if (builtinDrawerType != null && builtinDrawerType != typeof(DefaultDrawer) && CheckBuiltinDrawer(builtinDrawerType))
				{
					useBuiltinDrawer = true;
					useSelfDrawer = true;
				}
			}

			if (!useBuiltinDrawer)
				return;
			if (_drawers.Count != 0 && _drawers[_drawers.Count - 1].EndsDrawing)
			{
				string warn = "Drawer " + _drawers[_drawers.Count - 1].GetType().Name + " ends property drawing, but there built-in drawers that should be drawn after it:";

				for (int i = 0; i < builtinAttributes.Count; i++)
				{
					PropertyAttribute attribute = builtinAttributes[i];
					Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(Property, attribute.GetType());
					if (builtinDrawerType == null || !CheckBuiltinDrawer(builtinDrawerType))
						continue;
					warn += " " + builtinDrawerType.GetType().Name;
				}

				if (useSelfDrawer)
				{
					Type builtinDrawerType = UnityInternals.ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(Property, PropertyType);
					warn += " " + builtinDrawerType.GetType().Name;
				}

				UnityEngine.Debug.LogWarning(warn);
			}

			if (FieldInfo == null) throw new InvalidOperationException("FieldInfo is null!");

			UnityEngine.Debug.Log($"Using builtin drawer for {FieldInfo.Name} of type {FieldInfo.FieldType.Name}. Attributes: {string.Join(", ", builtinAttributes.Select(a => a.GetType().Name))}");

			_propertyHandler = Activator.CreateInstance(UnityInternals.PropertyHandlerType);

			for (int i = 0; i < builtinAttributes.Count; i++)
			{
				PropertyAttribute attribute = builtinAttributes[i];
				UnityInternals.PropertyDrawer_HandleAttribute(_propertyHandler, Property, attribute, FieldInfo, FieldInfo.FieldType);
			}

			if (useSelfDrawer && PropertyType != null)
				UnityInternals.PropertyDrawer_HandleDrawnType(_propertyHandler, Property, PropertyType, PropertyType, FieldInfo, null);

			// The following is not needed because the only time the PropertyHandler will be drawn is with this property, which already has it's referenced attached to this script.
			// object propCache = _editor != null ?
			// 	UnityInternals.Editor_m_PropertyHandlerCache(_editor) :
			// 	UnityInternals.ScriptAttributeUtility_propertyHandlerCache;

			// UnityInternals.PropertyHandlerCache_SetHandler(propCache, Property, _propertyHandler);
		}

		public void Return()
		{
			ChangesCache.OnClear -= Invalidate;

			_editor = null;
			_property = null;
			_fieldInfo = null;
			_propertyHandler = null;
			_propertyLabel = null;
			_tooltip = null;

			for (int i = 0; i < _drawers.Count; i++)
				_drawers[i].Return();
			_drawers.Clear();

			for (int i = 0; i < _children.Count; i++)
				_children[i].Return();
			_children.Clear();

			PropertyCache.TreePool.Return(this);
		}

		public void OnGUILayout(params GUILayoutOption[] options)
		{
			AssertFresh();

			float height = GetHeight();
			Rect r = EditorGUILayout.GetControlRect(true, height, options);

			if (Event.current.type != EventType.Layout)
				OnGUI(r);

			AssertFresh();
		}

		[System.Diagnostics.Conditional("NAF_DEBUG")]
		public void AssertFresh()
		{
			if (_iterator != 0)
				throw new InvalidOperationException("PropertyTree expected iterator context to be fresh!");
		}

		public void OnGUI(Rect position)
		{
			if (_iterator == 0)
				_propertyLabel = null;

			if (_iterator < _drawers.Count)
			{
				NAFPropertyDrawer drawer = _drawers[_iterator];

				using (new DrawerIncrement(this))
					drawer.DoGUI(position);
				return;
			}

			if (_propertyHandler != null) // Arrays always use this...
			{
				bool showChildren = UnityInternals.PropertyDrawer_OnGUI(_propertyHandler, position, Property, PropertyLabel, true);
				// TODO? Right now, even if this opens, do not draw children?
				return;
			}

			if (IsArrayProperty)
			{
				UnityEngine.Debug.LogWarning("PropertyTree.OnGUI: Array property should always use the default NAFDrawer or a property handler!");
				return;
			}

			// if (!property.isExpanded || DefaultableProperty(property))
			// {
			// 	bool showChildren = UnityInternals.EditorGUI_DefaultPropertyField(position, property, label);
			// 	property.NextVisible(false); // TODO. Right now, even if this opens, do not draw children?
			// 	return;
			// }


			position.height = UnityInternals.EditorGUI_GetSinglePropertyHeight(Property, PropertyLabel);
			bool expanded = UnityInternals.EditorGUI_DefaultPropertyField(position, Property, PropertyLabel);

			if (expanded == false) // This happens if the property is closed on this draw call.
			{
				return;
			}

			position.y += position.height + EditorGUIUtility.standardVerticalSpacing;

			EditorGUI.indentLevel++;

			for (int childIndex = 0; childIndex < _children.Count; childIndex++)
			{
				PropertyTree tree = _children[childIndex];

				tree.AssertFresh();
				position.height = tree.GetHeight();
				tree.OnGUI(position);
				tree.AssertFresh();

				position.y += position.height + EditorGUIUtility.standardVerticalSpacing;
			}

			EditorGUI.indentLevel--;
		}

		private bool CachedHeight()
		{
			if (_editor == null)
				return false;

			if (_cacheDrawID != _editor.ValidDrawId)
				return false;

			// Never cache properties with built-in drawers...
			if (_propertyHandler != null) 
				return false;

			for (int i = _iterator; i < _drawers.Count; i++)
				if (_drawers[i].Invalidated)
					return false;

			if (BlocksChildren)
				return true;

			if (!Property.isExpanded || Defaultable)
				return true;

			for (int childIndex = 0; childIndex < _children.Count; childIndex++)
			{
				if (_children[childIndex].CachedHeight() == false)
				{
					// Avoids most other checks in further calls until the cache is valid again.
					_cacheDrawID = _editor.ValidDrawId - 1;
					return false;
				}
			}

			return true;
		}

		public float GetHeight()
		{
			if (CachedHeight())
			{
				if (_iterator < _drawers.Count)
					return _drawers[_iterator].LastHeight;
				return _cachedHeight;
			}

			float height = DoGetHeight();

			if (_iterator >= _drawers.Count)
				_cachedHeight = height;
			else if (_editor != null && _iterator == 0)
				_cacheDrawID = _editor.ValidDrawId;

			return height;
		}

		private float DoGetHeight() // As method to allow for tail call in GetHeight()
		{
			if (_iterator == 0)
				_propertyLabel = null;

			if (_iterator < _drawers.Count)
			{
				NAFPropertyDrawer drawer = _drawers[_iterator];

				using (new DrawerIncrement(this))
					return drawer.DoGetHeight();
			}

			if (_propertyHandler != null) // Arrays always use this...
			{
				float h = UnityInternals.PropertyDrawer_GetHeight(_propertyHandler, Property, PropertyLabel);

				if (IsArrayElement)
					h += EditorGUIUtility.standardVerticalSpacing;
				return h;
			}

			if (IsArrayProperty)
			{
				UnityEngine.Debug.LogWarning("PropertyTree.OnGUI: Array property should always use the default NAFDrawer or a property handler!");
				return 20;
			}

			float height = UnityInternals.EditorGUI_GetSinglePropertyHeight(Property, PropertyLabel);

			if (!Property.isExpanded || Defaultable)
				return height;

			if (IsArrayElement)
				height += EditorGUIUtility.standardVerticalSpacing;

			for (int childIndex = 0; childIndex < _children.Count; childIndex++)
			{
				PropertyTree tree = _children[childIndex];

				tree.AssertFresh();
				height += tree.GetHeight();
				tree.AssertFresh();

				height += EditorGUIUtility.standardVerticalSpacing;
			}

			return height;
		}

		private void Invalidate()
		{
			bool result = false;
			for (int i = 0; i < _drawers.Count; i++)
				result &= _drawers[i].Invalidate();

			if (result)
			{
				if (_editor != null)
					_cacheDrawID = _editor.ValidDrawId - 1; // Invalidate height cache

				Repaint();
			}
		}

		public void Repaint()
		{
			if (_editor != null)
				_editor.Repaint();
			else ArrayDrawer.RepaintAll();
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
				tree.Activate(iterator, editor);
				trees.Add(tree);
			}
			while (UnityInternals.SerializedProperty_isValid(iterator));
			return trees;
		}

		/// <summary>
		/// A struct to increment the iterator of the PropertyTree. GUI calls throw exceptions to exit contexts once an event has been handled (optimization), and this ensures the iterator is decremented without needing to catch the exception.
		/// </summary>
		private readonly struct DrawerIncrement : IDisposable
		{
			private readonly PropertyTree tree;
			public DrawerIncrement(PropertyTree tree) => (this.tree = tree)._iterator++;
			public void Dispose() => tree._iterator--;
		}
	}
}