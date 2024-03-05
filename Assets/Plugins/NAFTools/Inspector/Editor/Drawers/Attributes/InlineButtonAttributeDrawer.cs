namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(InlineButtonAttribute))]
	public class InlineButtonAttributeDrawer : InlineLabelAttributeDrawer
	{
		// private Func<object, object, object> method;

		// protected override Task OnEnable(in SerializedProperty property)
		// {
		// 	return Task.WhenAll(
		// 		base.OnEnable(property),
		// 		PropertyFieldCompiler<object>.Load(property, ((InlineButtonAttribute)Attribute).Expression).ContinueWith(t =>
		// 		{
		// 			method = t.Result;
		// 		})
		// 	);
		// }

		// protected override void OnUpdate(SerializedProperty property)
		// {
		// 	InlineButtonAttribute attribute = (InlineButtonAttribute)Attribute;
		// 	method = PropertyFieldCompiler<object>.Get(property, attribute.Expression);
		// }

		protected override void OnGUI(Rect position)
		{
			InlineButtonAttribute attribute = (InlineButtonAttribute)Attribute;

			GUIContent content = this.content;
			content.text ??= ObjectNames.NicifyVariableName(attribute.Expression);

			if (DrawAsButton(attribute.Alignment, position, content, style))
				AttributeExpr<object>.Invoke(Tree.Property, attribute.Expression);
		}
	}
}