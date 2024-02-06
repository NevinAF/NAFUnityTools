namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(ReadonlyAttribute))]
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class ReadonlyAttributeDrawer : NAFPropertyDrawer
	{
		private bool _cResult;
		public override void TryUpdate(SerializedProperty property)
		{
			_cResult = AttributeEvaluator.Conditional((IConditionalAttribute)attribute, property);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new DisabledScope(_cResult))
				EditorGUI.PropertyField(position, property, label, true);
		}
	}
}