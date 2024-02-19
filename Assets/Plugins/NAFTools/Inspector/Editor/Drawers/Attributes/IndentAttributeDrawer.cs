namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(IndentAttribute))]
	public class IndentAttributeDrawer : NAFPropertyDrawer
	{
		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			int indent = ((IndentAttribute)Attribute).Indent;
			EditorGUI.indentLevel += indent;

			base.OnGUI(position, property, label);

			EditorGUI.indentLevel -= indent;
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return base.OnGetHeight(property, label);
		}
	}
}