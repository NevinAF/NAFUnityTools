namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(RuntimeDebugAttribute))]
	public class RuntimeDebugAttributeDrawer : NAFPropertyDrawer
	{
		protected override void OnGUI(Rect position)
		{
			if (EditorApplication.isPlaying)
			{
				using (DisabledScope.True)
					base.OnGUI(position);
			}
		}

		protected override float OnGetHeight()
		{
			return EditorApplication.isPlaying ? base.OnGetHeight() : 0;
		}
	}
}