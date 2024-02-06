namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;

	[CustomPropertyDrawer(typeof(InlineButtonAttribute))]
	public class InlineButtonAttributeDrawer : InlineLabelAttributeDrawer
	{
		private Func<object, object, object> method;

		public override void TryUpdate(SerializedProperty property)
		{
			base.TryUpdate(property);

			InlineButtonAttribute attribute = (InlineButtonAttribute)this.attribute;
			method = PropertyFieldCompiler<object>.Get(property, attribute.Expression);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InlineButtonAttribute attribute = (this.attribute as InlineButtonAttribute)!;
			attribute.Label ??= ObjectNames.NicifyVariableName(attribute.Expression);

			if (DrawAsButton(attribute.Alignment, position, content, style, property, label))
			{
				var targets = PropertyTargets.GetValues(property);
				for (int i = 0; i < targets.Length; i++)
				{
					try { method(targets.ParentValues[i], targets.FieldValues[i]); }
					catch (Exception e)
					{
						Debug.LogError("There was an error executing method called from a " + nameof(InlineButtonAttribute) + ". See following log for details.");
						Debug.LogException(e);
					}
				}
			}
		}
	}
}