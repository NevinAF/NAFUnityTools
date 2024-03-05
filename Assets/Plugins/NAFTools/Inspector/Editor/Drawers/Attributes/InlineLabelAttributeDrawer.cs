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
		protected AttributeExprCache<GUIContent> content;
		protected AttributeExprCache<GUIStyle> style;

		protected override async Task OnEnable()
		{
			var contentAttribute = (IContentAttribute)Attribute;
			(content, style) = await AttributeEvaluator.Content(contentAttribute, Tree.Property);
		}

		protected override void OnUpdate()
		{
			content.Refresh(Tree.Property);
			style.Refresh(Tree.Property, EditorStyles.miniLabel);
		}

		protected override void OnGUI(Rect position)
		{
			if (content.Value == null || style.Value == null)
			{
				base.OnGUI(position);
				return;
			}

			DrawAsLabel(((InlineLabelAttribute)Attribute).Alignment, position, content, style);
		}
	
		public void DrawAsLabel(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, bool disableProperty = false)
		{
			switch (alignment)
			{
				case LabelAlignment.Left:
					InlineGUI.InlineLabel(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position);
					break;

				case LabelAlignment.Right:
					InlineGUI.InlineLabel(ref position, content, style, true);
					base.OnGUI(position);
					break;

				case LabelAlignment.BetweenLeft:
				case LabelAlignment.BetweenRight:
					Rect labelRect = position; labelRect.width = EditorGUIUtility.labelWidth;
					Rect propertyRect = position; propertyRect.xMin += EditorGUIUtility.labelWidth;

					if (alignment == LabelAlignment.BetweenLeft)
						InlineGUI.InlineLabel(ref labelRect, content, style, true);
					else InlineGUI.InlineLabel(ref propertyRect, content, style, false);

					EditorGUI.LabelField(labelRect, Tree.PropertyLabel);
					Tree.PropertyLabel = GUIContent.none;
					base.OnGUI(propertyRect);
					break;

				default:
					throw new InvalidOperationException();
			}
		}

		public bool DrawAsButton(LabelAlignment alignment, Rect position, GUIContent content, GUIStyle style, bool disableProperty = false)
		{
			bool result;

			switch (alignment)
			{
				case LabelAlignment.Left:
					result = InlineGUI.InlineButton(ref position, content, style, false);
					position.xMin += 12;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position);
					return result;

				case LabelAlignment.Right:
					result = InlineGUI.InlineButton(ref position, content, style, true);
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(position);
					return result;

				case LabelAlignment.BetweenLeft:
				case LabelAlignment.BetweenRight:
					Rect labelRect = position; labelRect.width = EditorGUIUtility.labelWidth;
					Rect propertyRect = position; propertyRect.xMin += EditorGUIUtility.labelWidth;

					if (alignment == LabelAlignment.BetweenLeft)
						result = InlineGUI.InlineButton(ref labelRect, content, style, true);
					else result = InlineGUI.InlineButton(ref propertyRect, content, style, false);

					EditorGUI.LabelField(labelRect, Tree.PropertyLabel);
					Tree.PropertyLabel = GUIContent.none;
					using (new DisabledScope(!GUI.enabled || disableProperty))
						base.OnGUI(propertyRect);
					return result;

				default:
					throw new InvalidOperationException();
			}

			
		}
	}
}