namespace NAF.Inspector.Editor
{
	using System;
	using System.ComponentModel;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(IndentAttribute))]
	public class IndentAttributeDrawer : NAFPropertyDrawer
	{
		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int indent = (this.attribute as IndentAttribute)!.Indent;
			EditorGUI.indentLevel += indent;

			EditorGUI.PropertyField(position, property, label, true);

			EditorGUI.indentLevel -= indent;
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}
	}
}