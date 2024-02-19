namespace NAF.Inspector.Editor
{
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(SpaceAttribute))]
	public class NAFSpaceDrawer : NAFPropertyDrawer
	{
		private float Size => ((SpaceAttribute)Attribute).height;

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			Rect spaceRect = position;
			spaceRect.height = Size;
			// EditorGUI.DrawRect(spaceRect, UnityEngine.Random.ColorHSV());
			position.yMin += Size;
			base.OnGUI(position, property, label);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return base.OnGetHeight(property, label) + Size;
		}
	}

	[CustomPropertyDrawer(typeof(HeaderAttribute))]
	public class NAFHeaderDrawer : NAFPropertyDrawer
	{
		private string Header => ((HeaderAttribute)Attribute).header;

		private float Height()
		{
			float fullTextHeight = EditorStyles.boldLabel.CalcHeight(TempUtility.Content(Header), 1.0f);
			int lines = 1;
			if (Header != null)
			{
				for (int i = 0; i < Header.Length; i++)
					if (Header[i] == '\n')
						lines++;
			}
			float eachLineHeight = fullTextHeight / lines;
			return EditorGUIUtility.singleLineHeight * 1.5f + (eachLineHeight * (lines - 1));
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			// position.y += EditorGUIUtility.singleLineHeight * 0.5f;
			float height = Height();

			Rect headerRect = position;
			headerRect.height = height;
			headerRect = EditorGUI.IndentedRect(headerRect);
			// EditorGUI.DrawRect(headerRect, UnityEngine.Random.ColorHSV());
			headerRect.yMin += EditorGUIUtility.singleLineHeight * 0.5f;
			GUI.Label(headerRect, Header, EditorStyles.boldLabel);

			position.yMin += height;
			base.OnGUI(position, property, label);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return base.OnGetHeight(property, label) + Height();
		}
	}
}