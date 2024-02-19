namespace NAF.Inspector.Editor
{
	using System;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(DescriptionAttribute))]
	public class DescriptionAttributeDrawer : NAFPropertyDrawer
	{
		private GUIContent content;
		private GUIStyle style;

		private float ViewWidth => EditorGUIUtility.currentViewWidth - 16f - UnityInternals.EditorGUI_indent;

		protected override Task OnEnable(in SerializedProperty property)
		{
			return AttributeEvaluator.Load((DescriptionAttribute)Attribute, property);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			AttributeEvaluator.PopulateContent((DescriptionAttribute)Attribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle((DescriptionAttribute)Attribute, property) ?? EditorStyles.wordWrappedLabel;
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect descRect = position;
			descRect.width = ViewWidth;
			descRect.height = style.CalcHeight(content, descRect.width);
			// Draw box for debugging
			// EditorGUI.DrawRect(descRect, UnityEngine.Random.ColorHSV());
			EditorGUI.LabelField(descRect, content, style);

			position.yMin += descRect.height;
			base.OnGUI(position, property, label);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return style.CalcHeight(content, ViewWidth) + base.OnGetHeight(property, label);
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