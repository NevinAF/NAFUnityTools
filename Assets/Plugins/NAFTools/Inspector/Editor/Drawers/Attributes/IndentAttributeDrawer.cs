namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(IndentAttribute))]
	public class IndentAttributeDrawer : NAFPropertyDrawer
	{
		protected override void OnGUI(Rect position)
		{
			int indent = ((IndentAttribute)Attribute).Indent;
			EditorGUI.indentLevel += indent;

			base.OnGUI(position);

			EditorGUI.indentLevel -= indent;
		}

		protected override float OnGetHeight()
		{
			return base.OnGetHeight();
		}
	}
}