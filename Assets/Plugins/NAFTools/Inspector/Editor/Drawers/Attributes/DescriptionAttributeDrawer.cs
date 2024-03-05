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
		protected AttributeExprCache<GUIContent> content;
		protected AttributeExprCache<GUIStyle> style;

		private float ViewWidth => EditorGUIUtility.currentViewWidth - 16f - UnityInternals.EditorGUI_indent;

		protected override async Task OnEnable()
		{
			var contentAttribute = (DescriptionAttribute)Attribute;
			(content, style) = await AttributeEvaluator.Content(contentAttribute, Tree.Property);
		}

		protected override void OnUpdate()
		{
			content.Refresh(Tree.Property);
			style.Refresh(Tree.Property, EditorStyles.wordWrappedLabel);
		}

		private void SelfGUI(ref Rect position, GUIContent content, GUIStyle style)
		{
			Rect descRect = position;
			descRect.width = ViewWidth;
			descRect.height = style.CalcHeight(content, descRect.width);
			// Draw box for debugging
			// EditorGUI.DrawRect(descRect, UnityEngine.Random.ColorHSV());
			EditorGUI.LabelField(descRect, content, style);

			position.yMin += descRect.height;
		}

		private float SelfHeight(GUIContent content, GUIStyle style)
		{
			return style.CalcHeight(content, ViewWidth);
		}

		protected override void OnGUI(Rect position)
		{
			SelfGUI(ref position, content, style);
			base.OnGUI(position);
		}

		protected override float OnGetHeight()
		{
			return SelfHeight(content, style) + base.OnGetHeight();
		}

		protected override void LoadingGUI(Rect position)
		{
			var attribute = (DescriptionAttribute)Attribute;

			if (attribute.Label is string label && label.Length != 0 && label[0] != PropertyFieldCompiler.ExpressionSymbol)
			{
				var labelContent = TempUtility.Content(label);
				var labelStyle = EditorStyles.wordWrappedLabel;

				SelfGUI(ref position, labelContent, labelStyle);
			}

			base.LoadingGUI(position);
		}

		protected override float GetLoadingHeight()
		{
			var attribute = (DescriptionAttribute)Attribute;
			float height = 0;

			if (attribute.Label is string label && label.Length != 0 && label[0] != PropertyFieldCompiler.ExpressionSymbol)
			{
				var labelContent = TempUtility.Content(label);
				var labelStyle = EditorStyles.wordWrappedLabel;

				height += SelfHeight(labelContent, labelStyle);
			}

			return height + base.GetLoadingHeight();
		}
	}
}