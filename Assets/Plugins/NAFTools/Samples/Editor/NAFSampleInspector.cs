namespace NAF.Samples.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using NAF.Inspector.Editor;

	using MemberDrawer = NAF.Inspector.Editor.MemberDefinitionDrawerAttributeDrawer;

	// // Draws all array properties with a custom drawer, everything else is drawn normally
	[CanEditMultipleObjects]
	[CustomEditor(typeof(NAFSampleBehaviour), true)]
	public class NAFSampleInspector : ArrayDrawer
	{
		protected override PropertyAttribute[] GetAdditionalDrawers() => new PropertyAttribute[] { new MemberDefinitionDrawerAttribute() };

		public override void OnInspectorGUI()
		{
			MemberDrawer.CurrentDrawType = (MemberDrawer.DrawType)EditorGUILayout.EnumPopup("Show Definitions?", MemberDrawer.CurrentDrawType);
			EditorGUILayout.Space();

			if (MemberDrawer.CurrentDrawType == MemberDrawer.DrawType.TwoColumn)
			{
				EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * MemberDrawer.ExpandedScalar));
				EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.currentViewWidth * 0.45f * MemberDrawer.ExpandedScalar - 40f, 120f);
				base.OnInspectorGUI();
				EditorGUIUtility.labelWidth = 0f;
				GUILayout.EndVertical();
			}
			else {
				base.OnInspectorGUI();
			}
		}
	}
}