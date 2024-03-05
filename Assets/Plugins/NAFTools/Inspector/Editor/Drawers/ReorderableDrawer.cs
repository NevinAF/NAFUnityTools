namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics;
	using System.Linq;
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

		public override bool EndsDrawing => true;

		protected override Task OnEnable()
		{
			if (!Tree.IsArrayProperty)
				throw new System.ArgumentException("SerializedProperty is not an array.");

			UnityEngine.Debug.Log("ReorderableDrawer Init: " + Tree.Property.propertyPath);

			SerializedProperty iterator = Tree.Property.Copy();
			iterator.Next(true); // .Array
			iterator.Next(true); // .Array.size

			arraySize = iterator.Copy();
			reorderable = new ReorderableList(Tree.Property.serializedObject, Tree.Property, true, true, true, true)
			{
				drawHeaderCallback = DrawHeader,
				drawElementCallback = DrawElement,
				elementHeightCallback = ElementHeight,
				onChangedCallback = ListChanged,
			};

			iterator.Next(false); // First array element
			int size = Tree.Property.arraySize;
			for (int i = 0; i < size; i++)
			{
				PropertyTree tree = PropertyCache.TreePool.Get();
				tree.Activate(iterator, Tree.Editor);
				elements.Add(tree);
			}
			return base.OnEnable();
		}

		protected override void OnDisable()
		{
			base.OnDisable();

			arraySize = null;
			reorderable = null;

			for (int i = 0; i < elements.Count; i++)
				elements[i].Return();
			elements.Clear();
		}

		private void ResetElements()
		{
			for (int i = 0; i < elements.Count; i++)
				elements[i].Return();
			elements.Clear();

			SerializedProperty iterator = Tree.Property.Copy();
			iterator.Next(true); // .Array
			iterator.Next(true); // .Array.size
			iterator.Next(false); // First array element
			int size = Tree.Property.arraySize;

			for (int i = 0; i < size; i++)
			{
				PropertyTree tree = PropertyCache.TreePool.Get();
				tree.Activate(iterator, Tree.Editor);
				elements.Add(tree);
			}
		}

		/// <summary> Draw the property using the ReorderableList fix. The property should match the property used to create the drawer, otherwise it will be disposed and a new one will be created. If the property is not an array property, an exception will be thrown. </summary>
		protected override void OnGUI(Rect r)
		{
			if (Tree.Property.isExpanded == false)
				DrawHeader(r);
			else reorderable.DoList(r);
		}

		/// <summary> Get the height of the property using the ReorderableList fix. The property should match the property used to create the drawer, otherwise it will be disposed and a new one will be created. If the property is not an array property, an exception will be thrown. </summary>
		protected override float OnGetHeight()
		{
			if (Tree.Property.isExpanded == false)
				return EditorGUIUtility.singleLineHeight;

			return reorderable.GetHeight();
		}

		#endregion

		/// <summary> Cached GUIContent for the array size label, preventing garbage collection. </summary>
		private static readonly GUIContent s_ArraySizeContent =  new GUIContent("", "Array Size");

		/// <summary> The property representing the size of the array. </summary>
		private SerializedProperty arraySize;
		public List<PropertyTree> elements = new List<PropertyTree>();
		/// <summary> The ReorderableList that is used to draw the array, with modified draw calls to fix the first element bug. </summary>
		private ReorderableList reorderable;

		public ReorderableDrawer() { }

		// TODO REMOVE!!
		[Conditional("DEBUG")]
		private void ValidateChild(int index)
		{
			// TODO REMOVE!!
			if (index > elements.Count ||  SerializedProperty.DataEquals(elements[index].Property, Tree.Property.GetArrayElementAtIndex(index)) == false)
			{
				UnityEngine.Debug.LogError("PropertyTree does not match the array element at index " + index + ". Resetting elements.\nOld: " + elements[index].Property.propertyPath + "\nNew: " + Tree.Property.GetArrayElementAtIndex(index).propertyPath);
				ResetElements();
			}
		}

		/// <summary> Draw the header of the array, including the foldout and size fields. </summary>
		/// <remarks> Most of this code is copied from the ReorderableListWrapper.Draw method, with modifications for protected members (and the elements are not drawn here). </remarks>
		private void DrawHeader(Rect headerRect)
		{
			const float kSizeWidth = 48f; // Pulled from ReorderableListWrapper.Constants.kArraySizeWidth
			float EditorGUI_indent = EditorGUI.indentLevel * 15.0f; // Wrapper for EditorGUI.indent

			// Undo the margin added by the ReorderableList, and match the box to the original property
			if (Tree.Property.isExpanded)
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
			bool foldout = EditorGUI.BeginFoldoutHeaderGroup(headerRect, Tree.Property.isExpanded, Tree.PropertyLabel);
			EditorGUI.EndFoldoutHeaderGroup();

			// Prevents opening and closing the array with one click event.
			if (Event.current.type != EventType.Layout && Event.current.type != EventType.Repaint)
				Tree.Property.isExpanded = foldout;

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
			ValidateChild(index);

			r.xMin += 8.0f; // Indent for foldout arrow
			r.yMin += 1.0f; // Center element within the padding

			PropertyTree element = elements[index];

			// Change label width so the property field lines up with non-array fields
			// Taken from ReorderableList.Defaults.FieldLabelSize
			float labelWidth = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = r.xMax * 0.45f - 35f
				- (float)(element.Property.depth * 15f)
				- (float)(Regex.Matches(element.Property.propertyPath, ".Array.data").Count * 10f);

			// Draw the element
			element.OnGUI(r);

			if (Event.current.type == EventType.ContextClick && r.Contains(Event.current.mousePosition))
				Event.current.Use();
			EditorGUIUtility.labelWidth = labelWidth;
		}

		/// <summary> Get the height of the element at the given index. </summary>
		private float ElementHeight(int index)
		{
			ValidateChild(index);
			PropertyTree element = elements[index];
			return element.GetHeight();
		}

		private void ListChanged(ReorderableList _list)
		{
			if (_list != reorderable)
				throw new System.ArgumentException("ReorderableList does not match the list that was changed.");

			UnityEngine.Debug.Log("Changed array");
			ResetElements();

			// // To avoid resetting the whole these are some primitive checks for add/remove/swap
			// int size = Tree.Property.arraySize;
			// int diff = size - elements.Count;

			// SerializedProperty iterator = Tree.Property.Copy();
			// iterator.Next(true); // .Array
			// iterator.Next(true); // .Array.size
			// iterator.Next(false); // First array element

			// if (diff > 0) // Added items?
			// {
			// 	for (int i = 0; i < elements.Count; i++, iterator.Next(false))
			// 	{
			// 		PropertyTree tree = elements[i];
			// 		if (!SerializedProperty.DataEquals(tree.Property, iterator))
			// 		{
			// 			UnityEngine.Debug.Log("Changed array by adding " + diff + " elements, but the elements were not added at the end to resetting elements for drawer.\nOld: " + tree.Property.propertyPath + "\nNew: " + iterator.propertyPath);
			// 			ResetElements();
			// 			return;
			// 		}
			// 	}

			// 	for (int i = elements.Count; i < size; i++)
			// 	{
			// 		PropertyTree tree = PropertyCache.TreePool.Get();
			// 		tree.Reset(iterator, Tree.Editor);
			// 		elements.Add(tree);
			// 	}
			// }
			// else if (diff < 0) // Removed items?
			// {
			// 	Span<int> indices = stackalloc int[diff]; // List of the removed indices
			// 	int removedIndex = 0;
			// 	for (int i = 0; i < elements.Count; i++)
			// 	{
			// 		PropertyTree tree = elements[i];
			// 		if (!SerializedProperty.DataEquals(tree.Property, iterator))
			// 		{
			// 			if (removedIndex == diff)
			// 			{
			// 				UnityEngine.Debug.Log("Changed array by removing " + diff + " elements, but the element order was not maintained, or elements were removed and changed.\nChanges:\n\t" + string.Join("\n\t", indices.ToArray().Append(i).Select((ei, si) => elements[ei].Property.propertyPath + " != " + Tree.Property.GetArrayElementAtIndex(ei - si))) + "\nResetting elements for drawer.");
			// 				ResetElements();
			// 				return;
			// 			}

			// 			indices[removedIndex++] = i;
			// 		}
			// 		else iterator.Next(false);
			// 	}

			// 	for (int i = 0; i < diff; i++)
			// 	{
			// 		PropertyTree tree = elements[indices[i]];
			// 		tree.Return();
			// 		elements.RemoveAt(indices[i]);
			// 	}
			// }
			// else if (diff == 0) // Reorder?
			// {
			// 	int diffIndex = -1;
			// 	int fromIndex = -1;
			// 	for (int i = 0; i < elements.Count; i++, iterator.Next(false))
			// 	{
			// 		PropertyTree tree = elements[i];
			// 		if (!SerializedProperty.DataEquals(tree.Property, iterator))
			// 		{
			// 			if (diffIndex != -1)
			// 			{
			// 				UnityEngine.Debug.Log("Changed array by reordering elements, but the elements were not reordered, or elements were removed and changed.\nChanges:\n\t" + elements[diffIndex].Property.propertyPath + " != " + iterator.propertyPath + "\nResetting elements for drawer.");
			// 				ResetElements();
			// 				return;
			// 			}

			// 			diffIndex = i;
			// 		}
			// 	}
			// }
		}
	}
}