namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(LabelContentAttribute))]
	public class LabelContentAttributeDrawer : NAFPropertyDrawer
	{
		protected GUIContent content;
		protected GUIStyle style;

		protected override Task OnEnable(in SerializedProperty property)
		{
			var contentAttribute = (IContentAttribute)Attribute;
			return AttributeEvaluator.Load(contentAttribute, property);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			var contentAttribute = (IContentAttribute)Attribute;
			AttributeEvaluator.PopulateContent(contentAttribute, property, ref content);
			style = AttributeEvaluator.ResolveStyle(contentAttribute, property);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return base.OnGetHeight(property, label);
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (style == null)
			{
				base.OnGUI(position, property, content);
				return;
			}

			EditorGUI.LabelField(position, content, style);
			base.OnGUI(position, property, GUIContent.none);
		}
	}
}