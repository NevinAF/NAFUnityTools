namespace NAF.Inspector.Editor
{
	using System;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(DescriptionAttribute))]
	public class DescriptionAttributeDrawer : NAFDecoratorDrawer
	{
		private GUIContent content;
		private GUIStyle style;

		public override void TryUpdate(SerializedProperty property)
		{
			AttributeEvaluator.PopulateContent((DescriptionAttribute)attribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle((DescriptionAttribute)attribute, property) ?? EditorStyles.wordWrappedLabel;
		}

		public override void TryOnGUI(Rect position)
		{
			position.width = EditorGUIUtility.currentViewWidth - 20f;
			EditorGUI.LabelField(position, content, style);
		}

		public override float TryGetHeight()
		{
			return style.CalcHeight(content, EditorGUIUtility.currentViewWidth);
		}

		// private float? _height = null;

		// public override void DrawElement(Rect position, SerializedProperty property, GUIContent label)
		// {
		// 	DescriptionAttribute attribute = this.attribute as DescriptionAttribute;

		// 	var content = AttributeEvaluator.TempContent(attribute, property);
		// 	var style = AttributeEvaluator.ResolveStyle(attribute, property);

		// 	_height = style.CalcHeight(content, position.width);
		// 	float height = _height.Value;

		// 	if (attribute.ShowAbove)
		// 	{
		// 		Rect descriptionRect = position;
		// 		descriptionRect.height = height;

		// 		EditorGUI.LabelField(descriptionRect, content, style);

		// 		position.yMin += height + EditorGUIUtility.standardVerticalSpacing;
		// 		EditorGUI.PropertyField(position, property, label, property.isExpanded);
		// 	}
		// 	else
		// 	{
		// 		EditorGUI.PropertyField(position, property, label, property.isExpanded);
		// 		position.yMax += height + EditorGUIUtility.standardVerticalSpacing;
		// 		Rect descriptionRect = position;
		// 		descriptionRect.yMin = position.yMax - height;

		// 		EditorGUI.LabelField(descriptionRect, content, style);
		// 	}
		// }

		// public override float GetElementHeight(SerializedProperty property, GUIContent label)
		// {
		// 	DescriptionAttribute attribute = this.attribute as DescriptionAttribute;

		// 	var content = AttributeEvaluator.TempContent(attribute, property);
		// 	var style = AttributeEvaluator.ResolveStyle(attribute, property);

		// 	_height ??= style.CalcHeight(content, EditorGUIUtility.currentViewWidth);

		// 	return EditorGUI.GetPropertyHeight(property, label) + _height.Value + EditorGUIUtility.standardVerticalSpacing;
		// }

		// public override float GetArrayHeight(SerializedProperty property, GUIContent label)
		// {
		// 	return GetElementHeight(property, label);
		// }

		// public override void DrawArray(Rect position, SerializedProperty property, GUIContent label)
		// {
		// 	DrawElement(position, property, label);
		// }
	}
}