namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(InlineLabelAttribute))]
	[CustomPropertyDrawer(typeof(SuffixAttribute))]
	[CustomPropertyDrawer(typeof(PrefixAttribute))]
	public class InlineLabelAttributeDrawer : NAFPropertyDrawer
	{
		protected GUIContent content;
		protected GUIStyle style;
		protected SerializedProperty property;

		protected override Task OnEnable(in SerializedProperty property)
		{
			var contentAttribute = (IContentAttribute)Attribute;
			return AttributeEvaluator.Load(contentAttribute, property);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			var contentAttribute = (IContentAttribute)Attribute;
			AttributeEvaluator.PopulateContent(contentAttribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle(contentAttribute, property) ?? EditorStyles.miniLabel;
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (content == null || style == null)
			{
				base.OnGUI(position, property, label);
				return;
			}

			DrawAsLabel(((InlineLabelAttribute)Attribute).Alignment, position, content, style, property, label);
		}
	
		public void DrawAsLabel(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, SerializedProperty property, GUIContent label, bool disableProperty = false)
		{
			switch (alignment)
			{
				case LabelAlignment.Left:
					InlineGUI.InlineLabel(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position, property, label);
					break;

				case LabelAlignment.Right:
					InlineGUI.InlineLabel(ref position, content, style, true);
					base.OnGUI(position, property, label);
					break;

				case LabelAlignment.BetweenLeft:
				case LabelAlignment.BetweenRight:
					Rect labelRect = position; labelRect.width = EditorGUIUtility.labelWidth;
					Rect propertyRect = position; propertyRect.xMin += EditorGUIUtility.labelWidth;

					if (alignment == LabelAlignment.BetweenLeft)
						InlineGUI.InlineLabel(ref labelRect, content, style, true);
					else InlineGUI.InlineLabel(ref propertyRect, content, style, false);

					EditorGUI.LabelField(labelRect, label);
					base.OnGUI(propertyRect, property, GUIContent.none);
					break;

				default:
					throw new InvalidOperationException();
			}
		}

		public bool DrawAsButton(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, SerializedProperty property, GUIContent label, bool disableProperty = false)
		{
			bool result;

			switch (alignment)
			{
				case LabelAlignment.Left:
					result = InlineGUI.InlineButton(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position, property, label);
					return result;

				case LabelAlignment.Right:
					result = InlineGUI.InlineButton(ref position, content, style, true);
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position, property, label);
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
						base.OnGUI(propertyRect, property, GUIContent.none);
					return result;

				default:
					throw new InvalidOperationException();
			}

			
		}
	}
}