namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;
	using System.Threading.Tasks;
	using Unity.Properties;

	[CustomPropertyDrawer(typeof(LabelContentAttribute))]
	public class LabelContentAttributeDrawer : NAFPropertyDrawer
	{
		protected AttributeExprCache<GUIContent> content;
		protected AttributeExprCache<GUIStyle> style;

		protected override async Task OnEnable()
		{
			var contentAttribute = (LabelContentAttribute)Attribute;
			(content, style) = await AttributeEvaluator.Content(contentAttribute, Tree.Property);
		}

		protected override void OnUpdate()
		{
			content.Refresh(Tree.Property);
			style.Refresh(Tree.Property, null);
		}

		protected override float OnGetHeight()
		{
			return base.OnGetHeight();
		}

		protected override void OnGUI(Rect position)
		{
			if (style.Value == null)
			{
				Tree.PropertyLabel = content;
				base.OnGUI(position);
				return;
			}

			EditorGUI.LabelField(position, content, style);
			base.OnGUI(position);
		}
	}
}