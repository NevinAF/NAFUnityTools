namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;

	[CustomPropertyDrawer(typeof(LabelContentAttribute))]
	public class LabelContentAttributeDrawer : NAFPropertyDrawer
	{
		protected GUIContent content;
		protected GUIStyle style;

		public override void TryUpdate(SerializedProperty property)
		{
			var contentAttribute = this.attribute as IContentAttribute;
			AttributeEvaluator.PopulateContent(contentAttribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle(contentAttribute, property);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (style == null)
			{
				EditorGUI.PropertyField(position, property, content, property.isExpanded);
				return;
			}

			EditorGUI.LabelField(position, content, style);
			EditorGUI.PropertyField(position, property, GUIContent.none, property.isExpanded);
		}
	}
}