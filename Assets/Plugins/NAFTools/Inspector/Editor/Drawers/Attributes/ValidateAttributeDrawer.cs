namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(ValidateAttribute))]
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class ValidateAttributeDrawer : InlineLabelAttributeDrawer
	{
		private bool _cResult;

		public override void TryUpdate(SerializedProperty property)
		{
			((ValidateAttribute)this.attribute).Style ??= EditorStyles.helpBox;
			base.TryUpdate(property);
			_cResult = AttributeEvaluator.Conditional((IConditionalAttribute)attribute, property);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!_cResult)
			{
				base.TryOnGUI(position, property, label);
				return;
			}

			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}