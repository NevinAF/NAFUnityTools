namespace NAF.Inspector.Editor
{
	using System.Reflection;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class HideIfAttributeDrawer : NAFPropertyDrawer
	{
		private bool _cResult;
		public override void TryUpdate(SerializedProperty property)
		{
			_cResult = AttributeEvaluator.Conditional(attribute as IConditionalAttribute, property);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return _cResult ? 0f : EditorGUI.GetPropertyHeight(property, label);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_cResult) return;
			EditorGUI.PropertyField(position, property, label, true);
		}
	}
}