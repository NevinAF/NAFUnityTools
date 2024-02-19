namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(SNullable<>))]
	public class NullablePropertyDrawer : PropertyDrawer
	{
		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			// hasValue
			property.NextVisible(true);
			// value
			property.NextVisible(false);

			float result = EditorGUI.GetPropertyHeight(property, label, true);

			property.NextVisible(false); // end.

			return result;
		}

		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// Adds a toggle button to the right of the value property. Value property draws like normal

			// hasValue
			property.NextVisible(true);
			bool hasValue = InlineGUI.InlineToggle(ref position, property);

			// value
			property.NextVisible(false);

			bool guiEnabled = GUI.enabled;
			GUI.enabled = hasValue && guiEnabled;

			EditorGUI.PropertyField(position, property, label, true);

			GUI.enabled = guiEnabled;

			property.NextVisible(false); // end.
		}
	}
}