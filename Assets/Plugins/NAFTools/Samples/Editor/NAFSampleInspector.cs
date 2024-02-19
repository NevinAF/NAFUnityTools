namespace NAF.Samples.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using NAF.Inspector.Editor;

	using MemberDrawer = NAF.Inspector.Editor.MemberDefinitionDrawerAttributeDrawer;
	using System.Collections.Generic;

	// // Draws all array properties with a custom drawer, everything else is drawn normally
	[CanEditMultipleObjects]
	[CustomEditor(typeof(NAFSampleBehaviour), true)]
	public class NAFSampleInspector : ArrayDrawer
	{
		public static MemberDrawer.DrawType DrawType = MemberDrawer.DrawType.TwoColumn;

		public override void OnInspectorGUI()
		{
			DrawType = (MemberDrawer.DrawType)EditorGUILayout.EnumPopup("Show Definitions?", DrawType);
			EditorGUILayout.Space();

			var previousDraw = MemberDrawer.CurrentDrawType;
			MemberDrawer.CurrentDrawType = DrawType;

			if (DrawType == MemberDrawer.DrawType.TwoColumn)
			{
				float previous = UnityInternals.EditorGUIUtility_s_OverriddenViewWidth;
				UnityInternals.EditorGUIUtility_s_OverriddenViewWidth = EditorGUIUtility.currentViewWidth * MemberDrawer.ExpandedScalar;
				EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth));
				base.OnInspectorGUI();
				GUILayout.EndVertical();
				UnityInternals.EditorGUIUtility_s_OverriddenViewWidth = previous;
			}
			else {
				base.OnInspectorGUI();
			}

			MemberDrawer.CurrentDrawType = previousDraw;
		}
	}
}