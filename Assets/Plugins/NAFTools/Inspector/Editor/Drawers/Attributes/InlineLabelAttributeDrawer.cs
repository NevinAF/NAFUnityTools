namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;

	[CustomPropertyDrawer(typeof(InlineLabelAttribute))]
	[CustomPropertyDrawer(typeof(SuffixAttribute))]
	[CustomPropertyDrawer(typeof(PrefixAttribute))]
	public class InlineLabelAttributeDrawer : NAFPropertyDrawer
	{
		protected GUIContent content;
		protected GUIStyle style;
		protected SerializedProperty property;

		public override void TryUpdate(SerializedProperty property)
		{
			var contentAttribute = this.attribute as IContentAttribute;
			AttributeEvaluator.PopulateContent(contentAttribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle(contentAttribute, property) ?? EditorStyles.miniLabel;
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (content == null || style == null)
			{
				EditorGUI.PropertyField(position, property, label, property.isExpanded);
				return;
			}

			DrawAsLabel((this.attribute as InlineLabelAttribute).Alignment, position, content, style, property, label);
		}
	
		public static void DrawAsLabel(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, SerializedProperty property, GUIContent label, bool disableProperty = false)
		{
			switch (alignment)
			{
				case LabelAlignment.Left:
					InlineGUI.InlineLabel(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						EditorGUI.PropertyField(position, property, label, property.isExpanded);
					break;

				case LabelAlignment.Right:
					InlineGUI.InlineLabel(ref position, content, style, true);
					EditorGUI.PropertyField(position, property, label, property.isExpanded);
					break;

				case LabelAlignment.BetweenLeft:
				case LabelAlignment.BetweenRight:
					Rect labelRect = position; labelRect.width = EditorGUIUtility.labelWidth;
					Rect propertyRect = position; propertyRect.xMin += EditorGUIUtility.labelWidth;

					if (alignment == LabelAlignment.BetweenLeft)
						InlineGUI.InlineLabel(ref labelRect, content, style, true);
					else InlineGUI.InlineLabel(ref propertyRect, content, style, false);

					EditorGUI.LabelField(labelRect, label);
					EditorGUI.PropertyField(propertyRect, property, GUIContent.none, property.isExpanded);
					break;

				default:
					throw new InvalidOperationException();
			}
		}

		public static bool DrawAsButton(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, SerializedProperty property, GUIContent label, bool disableProperty = false)
		{
			bool result;

			switch (alignment)
			{
				case LabelAlignment.Left:
					result = InlineGUI.InlineButton(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						EditorGUI.PropertyField(position, property, label, property.isExpanded);
					return result;

				case LabelAlignment.Right:
					result = InlineGUI.InlineButton(ref position, content, style, true);
					using (new DisabledScope(!GUI.enabled || disableProperty))
						EditorGUI.PropertyField(position, property, label, property.isExpanded);
					return result;

				case LabelAlignment.BetweenLeft:
				case LabelAlignment.BetweenRight:
					Rect labelRect = position; labelRect.width = EditorGUIUtility.labelWidth;
					Rect propertyRect = position; propertyRect.xMin += EditorGUIUtility.labelWidth;

					if (alignment == LabelAlignment.BetweenLeft)
						result = InlineGUI.InlineButton(ref labelRect, content, style, true);
					else result = InlineGUI.InlineButton(ref propertyRect, content, style, false);

					EditorGUI.LabelField(labelRect, label);
					using (new DisabledScope(!GUI.enabled || disableProperty))
						EditorGUI.PropertyField(propertyRect, property, GUIContent.none, property.isExpanded);
					return result;

				default:
					throw new InvalidOperationException();
			}

			
		}
	}
}