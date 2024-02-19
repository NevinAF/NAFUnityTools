namespace NAF.Inspector.Editor
{
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(ValidateAttribute))]
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class ValidateAttributeDrawer : InlineLabelAttributeDrawer
	{
		private bool _cResult;

		protected override Task OnEnable(in SerializedProperty property)
		{
			return Task.WhenAll(
				base.OnEnable(property),
				AttributeEvaluator.Load((IConditionalAttribute)Attribute, property)
			);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			((ValidateAttribute)Attribute).Style ??= EditorStyles.helpBox;
			_cResult = AttributeEvaluator.Conditional((IConditionalAttribute)Attribute, property);
			base.OnUpdate(property);
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (!_cResult)
			{
				base.OnGUI(position, property, label);
				return;
			}

			Tree.OnGUI(position, property, label);
		}
	}
}