#nullable enable
namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	public class AttributeSamplesWindow : EditorWindow
	{
		private GameObject? gameObject;
		private Editor? editor;

		[MenuItem("NAF/Attribute Samples")]
		private static void ShowWindow()
		{
			var window = GetWindow<AttributeSamplesWindow>();
			window.titleContent = new GUIContent("Attribute Samples");

			window.gameObject = new GameObject("Attribute Samples");
			// window.gameObject.hideFlags = HideFlags.

			window.editor = Editor.CreateEditor(window.gameObject.AddComponent<Rigidbody>());

			window.Show();
		}

		public static Color DarkLineColor => EditorGUIUtility.isProSkin ? new Color(0.11f, 0.11f, 0.11f, 0.8f) : new Color(0.38f, 0.38f, 0.38f, 0.6f);
		public static Color LightLineColor => EditorGUIUtility.isProSkin ? new Color(1.000f, 1.000f, 1.000f, 0.103f) : new Color(1, 1, 1, 1);

		private Vector2 scrollPosition;
		private bool foldout = true;
		private bool foldout2 = true;
		private string text =
@"
using UnityEngine;
using NAF.Inspector;

public class ExampleBehaviour : MonoBehaviour
{
	[Readonly]
	public int readonlyField = 0;

	[field: Readonly]
	public int ReadonlyProperty => 0;

	[Validate(nameof(Validate))]
	public int validatedField = 0;
}
";

		public static void DrawBottomBorder(Rect rect, bool lightColor = false)
		{
			Color color = lightColor ? LightLineColor : DarkLineColor;
			EditorGUI.DrawRect(new Rect(rect.x + 1, rect.yMax - 1, rect.width - 2, 1), color);
		}

		private void OnGUI()
		{
			if (editor == null)
			{
				EditorGUILayout.HelpBox("The editor was destroyed. Please close and reopen this window.", UnityEditor.MessageType.Error);
				return;
			}

			scrollPosition = GUILayout.BeginScrollView(scrollPosition, false, true, GUIStyle.none, GUI.skin.verticalScrollbar);

			EditorGUILayout.LabelField("Attribute Samples", EditorStyles.boldLabel);
			EditorGUILayout.LabelField("This window demonstrates some of the custom attributes in NAF Extensions.", EditorStyles.wordWrappedLabel);
			EditorGUILayout.Space();

			if (gameObject == null)
			{
				EditorGUILayout.HelpBox("The game object was destroyed. Please close and reopen this window.", UnityEditor.MessageType.Error);
				return;
			}

			// Create a new GUIStyle
			GUIStyle y = new GUIStyle(EditorStyles.helpBox) { padding = new RectOffset(0, 0, 0, 0), margin = new RectOffset(8, 8, 8, 8) };
			EditorGUILayout.BeginVertical(y);

			// Draw a box around the whole thing
			GUIStyle p = new GUIStyle(GUIStyle.none) { padding = EditorStyles.helpBox.padding };

			Rect lastRect = EditorGUILayout.BeginVertical(p);
				GUIContent content = TempUtility.Content(
					ObjectNames.NicifyVariableName(editor.target.GetType().Name),
					AssetPreview.GetMiniThumbnail(editor.target));
				foldout = EditorGUILayout.Foldout(foldout, content, true, EditorStyles.foldout);
			EditorGUILayout.EndVertical();

			if (foldout)
			{
				DrawBottomBorder(lastRect, true);

				// GUIStyle p = new GUIStyle(GUIStyle.none) {  };
				lastRect = EditorGUILayout.BeginVertical(EditorStyles.inspectorDefaultMargins, GUILayout.ExpandWidth(true));
					GUI.Box(lastRect, GUIContent.none);

					bool save = EditorGUIUtility.hierarchyMode;
					EditorGUIUtility.hierarchyMode = true;
					editor.OnInspectorGUI();
					EditorGUIUtility.hierarchyMode = save;
				EditorGUILayout.EndVertical();
			}

			DrawBottomBorder(lastRect, false);

			lastRect = EditorGUILayout.BeginVertical(p);
				content = TempUtility.Content(
					ObjectNames.NicifyVariableName(editor.target.GetType().Name),
					image: EditorIcons.TextAsset_Icon);
				foldout2 = EditorGUILayout.Foldout(foldout2, content, true, EditorStyles.foldout);
			EditorGUILayout.EndVertical();

			if (foldout2)
			{
				DrawBottomBorder(lastRect, true);

				lastRect = EditorGUILayout.BeginVertical(p);
					GUI.Box(lastRect, GUIContent.none);

					text = EditorGUILayout.TextArea(text);
				EditorGUILayout.EndVertical();
			}



			EditorGUILayout.EndVertical();

			EditorGUILayout.EndScrollView();
		}
	}
}
#nullable restore