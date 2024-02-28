namespace NAF.Inspector.Editor
{
	using System;
	using System.Reflection;
	using System.Runtime.Remoting.Messaging;
	using System.Text.RegularExpressions;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEditorInternal;
	using UnityEngine;

	/// <summary>
	/// Wrapper class for EditorGUILayout.PropertyField which overrides how arrays are drawn to fix the issue where the first element of an array does not get drawn with it's expanded height.
	/// </summary>
	public class ReorderableDrawer : NAFPropertyDrawer
	{
		#region NAFPropertyDrawer Functions

		protected override Task OnEnable(in SerializedProperty property)
		{
			Init(property.Copy());
			return base.OnEnable(property);
		}

		protected override void OnDisable()
		{
			base.OnDisable();
			property = null;
			arraySize = null;
			reorderable = null;
		}

		#endregion

		/// <summary> Cached GUIContent for the array label, preventing garbage collection. </summary>
		private static readonly GUIContent s_ArrayLabelContent = new GUIContent();
		/// <summary> Cached GUIContent for the array size label, preventing garbage collection. </summary>
		private static readonly GUIContent s_ArraySizeContent =  new GUIContent("", "Array Size");

		/// <summary> The base property that is to be drawn. </summary>
		private SerializedProperty property;
		/// <summary> The property representing the size of the array. </summary>
		private SerializedProperty arraySize;
		/// <summary> The ReorderableList that is used to draw the array, with modified draw calls to fix the first element bug. </summary>
		private ReorderableList reorderable;

		public ReorderableDrawer() { }

		/// <summary> Create a new ReorderableFixDrawer for the property. </summary>
		public ReorderableDrawer(SerializedProperty source) => Init(source);

		/// <summary> Initialize the ReorderableList and properties. </summary>
		private void Init(SerializedProperty source)
		{
			if (source.isArray == false)
				throw new System.ArgumentException("SerializedProperty is not an array.");

			UnityEngine.Debug.Log("ReorderableDrawer Init: " + source.propertyPath);

			property = source;
			arraySize = property.FindPropertyRelative("Array.size");

			reorderable = new ReorderableList(property.serializedObject, property, true, true, true, true)
			{
				drawHeaderCallback = DrawHeader,
				drawElementCallback = DrawElement,
				elementHeightCallback = ElementHeight
			};
		}

		/// <summary> Optimization field used for getting child element without copying property. </summary>
		private int m_index;
		/// <summary> Optimization field used for getting child element without copying property. </summary>
		private SerializedProperty m_childIt;

		/// <summary>
		/// Draw the property using the ReorderableList fix. The property should match the property used to create the drawer, otherwise it will be disposed and a new one will be created. If the property is not an array property, an exception will be thrown. This is a Layout call, meaning it allocates its own space in the GUI.
		/// </summary>
		public void GUILayout(SerializedProperty property)
		{
			VerifyReset(property);

			// This is ok because the header is always drawn first.
			s_ArrayLabelContent.text = property.displayName;
			s_ArrayLabelContent.tooltip = property.tooltip;
			s_ArrayLabelContent.image = null;

			reorderable.DoLayoutList();
		}

		/// <summary> Draw the property using the ReorderableList fix. The property should match the property used to create the drawer, otherwise it will be disposed and a new one will be created. If the property is not an array property, an exception will be thrown. </summary>
		protected override void OnGUI(Rect r, SerializedProperty property, GUIContent label)
		{
			VerifyReset(property);

			// This is ok because the header is always drawn first.
			s_ArrayLabelContent.text = label.text;
			s_ArrayLabelContent.tooltip = label.tooltip;
			s_ArrayLabelContent.image = label.image;

			property.NextVisible(false);

			if (this.property.isExpanded == false)
				DrawHeader(r);
			else reorderable.DoList(r);
		}

		/// <summary> Get the height of the property using the ReorderableList fix. The property should match the property used to create the drawer, otherwise it will be disposed and a new one will be created. If the property is not an array property, an exception will be thrown. </summary>
		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			VerifyReset(property);

			property.NextVisible(false);

			if (this.property.isExpanded == false)
				return EditorGUIUtility.singleLineHeight;

			// This is ok because the header is always drawn first.
			s_ArrayLabelContent.text = label.text;
			s_ArrayLabelContent.tooltip = label.tooltip;
			s_ArrayLabelContent.image = label.image;

			return reorderable.GetHeight();
		}

		private void VerifyReset(SerializedProperty property)
		{
			if (!SerializedProperty.EqualContents(this.property, property))
			{
				Debug.LogWarning("ReorderableFixDrawer property does not match the property given for layout, which should not happen. Disposing old drawer and creating a new one. Old: " + this.property.propertyPath + ", New: " + property.propertyPath);
				Init(property.Copy());
			}

			m_index = int.MaxValue;
			m_childIt = null;
		}

		/// <summary> Get the child property at the given index. Used for a slight optimization to avoid copying the property. </summary>
		private SerializedProperty GetChild(int index)
		{
			if (index < m_index || m_childIt == null)
			{
				m_index = index;
				m_childIt = property.GetArrayElementAtIndex(index);
			}
			else while (m_index < index)
			{
				m_childIt.Next(false);
				m_index++;
			}

			return m_childIt;
		}

		/// <summary> Draw the header of the array, including the foldout and size fields. </summary>
		/// <remarks> Most of this code is copied from the ReorderableListWrapper.Draw method, with modifications for protected members (and the elements are not drawn here). </remarks>
		private void DrawHeader(Rect headerRect)
		{
			const float kSizeWidth = 48f; // Pulled from ReorderableListWrapper.Constants.kArraySizeWidth
			float EditorGUI_indent = EditorGUI.indentLevel * 15.0f; // Wrapper for EditorGUI.indent

			// Undo the margin added by the ReorderableList, and match the box to the original property
			if (property.isExpanded)
			{
				headerRect.xMax += 6.0f;
				headerRect.xMin += EditorGUI_indent - 6.0f;
				headerRect.y -= 1f;
			}
			headerRect.height = EditorGUIUtility.singleLineHeight;

			Rect sizeRect = new Rect(headerRect.xMax - kSizeWidth - EditorGUI_indent * EditorGUI.indentLevel, headerRect.y,
				kSizeWidth + EditorGUI_indent * EditorGUI.indentLevel, EditorGUIUtility.singleLineHeight);

			// Mixed Value is manually updated for the label and size fields. This saves the previous state.
			bool showMixedValue = EditorGUI.showMixedValue;

			// Prevent clicking on the size field from closing the array.
			EventType prev = Event.current.type;
			if (Event.current.type == EventType.MouseUp && sizeRect.Contains(Event.current.mousePosition))
				Event.current.type = EventType.Used;

			/* Draw the foldout/label */
			EditorGUI.showMixedValue = false;
			bool foldout = EditorGUI.BeginFoldoutHeaderGroup(headerRect, property.isExpanded, s_ArrayLabelContent);
			EditorGUI.EndFoldoutHeaderGroup();

			// Prevents opening and closing the array with one click event.
			if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				property.isExpanded = foldout;

			// Restore the event type before drawing the size field.
			Event.current.type = prev;

			/* Draw the size field */
			EditorGUI.BeginChangeCheck();
			EditorGUI.showMixedValue = arraySize.hasMultipleDifferentValues;
			int result = EditorGUI.DelayedIntField(sizeRect, s_ArraySizeContent, arraySize.intValue, EditorStyles.numberField);
			if (EditorGUI.EndChangeCheck())
				arraySize.intValue = result;

			// Restore the mixed value state
			EditorGUI.showMixedValue = showMixedValue;
		}

		/// <summary> Draw the element at the given index. </summary>
		private void DrawElement(Rect r, int index, bool isActive, bool isFocused)
		{
			r.xMin += 8.0f; // Indent for foldout arrow
			r.yMin += 1.0f; // Center element within the padding

			SerializedProperty element = GetChild(index);

			// Change label width so the property field lines up with non-array fields
			// Taken from ReorderableList.Defaults.FieldLabelSize
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = r.xMax * 0.45f - 35f
				- (float)(element.depth * 15f)
				- (float)(Regex.Matches(element.propertyPath, ".Array.data").Count * 10f);

			// Draw the element
			EditorGUI.PropertyField(r, element, true);

			if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
				Event.current.Use();
			EditorGUIUtility.labelWidth = labelWidth;
		}

		/// <summary> Get the height of the element at the given index. </summary>
		private float ElementHeight(int index)
		{
			return EditorGUI.GetPropertyHeight(GetChild(index));
		}
	}
}