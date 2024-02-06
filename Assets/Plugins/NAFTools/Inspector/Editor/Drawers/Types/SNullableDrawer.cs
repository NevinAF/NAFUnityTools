namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(SNullable<>))]
	public class NullablePropertyDrawer : NAFPropertyDrawer
	{
		public override float TryGetHeight(SerializedProperty property,
												GUIContent label)
		{
			SerializedProperty valueProperty = property.FindPropertyRelative("value");
			return EditorGUI.GetPropertyHeight(valueProperty, label);
		}

		public override void TryOnGUI(Rect position,
								SerializedProperty property,
								GUIContent label)
		{
			// Adds a toggle button to the right of the value property. Value property draws like normal

			SerializedProperty hasValueProperty = property.FindPropertyRelative("hasValue");
			SerializedProperty valueProperty = property.FindPropertyRelative("value");

			bool hasValue = InlineGUI.InlineToggle(ref position, hasValueProperty);

			bool guiEnabled = GUI.enabled;
			GUI.enabled = hasValue && guiEnabled;

			EditorGUI.PropertyField(position, valueProperty, label, true);

			GUI.enabled = guiEnabled;
		}
	}
}