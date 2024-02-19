// namespace NAF.Inspector.Editor
// {
// 	using System;
// 	using System.Reflection;
// 	using NAF.Inspector;
// 	using UnityEditor;
// 	using UnityEngine;

// 	public abstract class ArrayPropertyDrawer : NAFPropertyDrawer
// 	{
// 		private bool? _isArrayType;
// 		public bool IsArrayType => _isArrayType ??= fieldInfo?.FieldType.IsArrayOrList() ?? false;

// 		public ArrayPropertyDrawer(PropertyAttribute attribute, FieldInfo fieldInfo) :
// 			base(attribute, fieldInfo)
// 		{
// 		}

// 		public bool 

// 		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
// 		{
			
// 		}


// 		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
// 		{
// 			var a = (attribute as IArrayPropertyAttribute)!;

// 			if (!IsArrayType)
// 			{
// 				if (!a.DrawOnField)
// 					throw new InvalidOperationException($"Attribute {attribute.GetType().Name} can only be used on array fields.");

// 				DrawElement(position, property, label);
// 				return;
// 			}

// 			if (!a.DrawOnElements && !a.DrawOnArray)
// 				throw new InvalidOperationException($"Attribute {attribute.GetType().Name} extends '{nameof(IArrayPropertyAttribute)}' but does not draw on the array nor the elements.");

// 			if (IsElement(property))
// 			{
// 				if (a.DrawOnElements)
// 				{
// 					DrawElement(position, property, label);
// 					return;
// 				}
// 			}
// 			else if (a.DrawOnArray)
// 			{
// 				DrawArray(position, property, label);
// 				return;
// 			}

// 			EditorGUI.PropertyField(position, property, label, true);
// 		}

// 		public abstract float GetArrayHeight(SerializedProperty property, GUIContent label);
// 		public abstract float GetElementHeight(SerializedProperty property, GUIContent label);
// 		public abstract void DrawArray(Rect position, SerializedProperty property, GUIContent label);
// 		public abstract void DrawElement(Rect position, SerializedProperty property, GUIContent label);
// 	}
// }