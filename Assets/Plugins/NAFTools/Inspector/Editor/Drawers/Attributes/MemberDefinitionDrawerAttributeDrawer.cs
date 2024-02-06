namespace NAF.Inspector.Editor
{
	using System.Diagnostics;
	using System.Reflection;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using NAF.ExpressionCompiler;

	[CustomPropertyDrawer(typeof(MemberDefinitionDrawerAttribute))]
	public class MemberDefinitionDrawerAttributeDrawer : NAFPropertyDrawer
	{
		public enum DrawType
		{
			No,
			Inline,
			TwoColumn
		}

		public static DrawType CurrentDrawType = DrawType.TwoColumn;
		public const float ExpandedScalar = 0.4f;

		private static MemberColorizer _colorizer;
		private static MemberColorizer Colorizer => _colorizer ??= new UnityMemberColorizer(typeof(MemberDefinitionDrawerAttribute), typeof(HeaderAttribute), typeof(SpaceAttribute), typeof(DebuggerBrowsableAttribute));

		private static GUIStyle _hoverStyle;
		private static GUIStyle HoverStyle => _hoverStyle ??= new GUIStyle(EditorStyles.label)
		{
			font = MonoSpace,
			richText = true,
			wordWrap = false,
			alignment = TextAnchor.UpperLeft,
			fontStyle = FontStyle.Bold,
		};

		private static GUIStyle _normalStyle;
		private static GUIStyle NormalStyle => _normalStyle ??= new GUIStyle(EditorStyles.label)
		{
			font = MonoSpace,
			richText = true,
			wordWrap = false,
			alignment = TextAnchor.UpperLeft,
		};

		private const string CollapsedIcon = EditorIcons.d_align_horizontally_right;
		private const string ExpandedIcon = EditorIcons.d_align_horizontally_center;
		public const string CopyIcon = EditorIcons.Clipboard;

		private MemberColorizer.Result? colorized;

		private MemberInfo Member(SerializedProperty property)
		{
			if (fieldInfo != null)
				return fieldInfo;

			if (property.name == "m_Script")
				return property.serializedObject.targetObject.GetType();

			return null;
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.depth > 0 || CurrentDrawType == DrawType.No)
			{
				EditorGUI.PropertyField(position, property, label, true);
				return;
			}
			colorized ??= Colorizer.DeclarationContent(Member(property));

			Rect definitionRect = position;
			bool copied;

			if (CurrentDrawType == DrawType.TwoColumn)
			{
				Rect totalRect = position;
				totalRect.width = EditorGUIUtility.currentViewWidth;
				bool hovered = totalRect.Contains(Event.current.mousePosition);

				float propHeight = EditorGUI.GetPropertyHeight(property, label);

				definitionRect.x += EditorGUIUtility.currentViewWidth * ExpandedScalar + EditorGUIUtility.standardVerticalSpacing;
				definitionRect.width = EditorGUIUtility.currentViewWidth - definitionRect.x - EditorGUIUtility.standardVerticalSpacing;
				definitionRect.height = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

				position.height = propHeight;
				EditorGUI.PropertyField(position, property, label, true);


				using (DisabledScope.False)
					copied = GUI.Button(definitionRect, TempUtility.Content(colorized.Value.RichText, tooltip: "Click to copy!"), hovered ? HoverStyle : NormalStyle);
			}
			else
			{
				bool hovered = position.Contains(Event.current.mousePosition);

				definitionRect.height = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

				position.yMin += definitionRect.height + EditorGUIUtility.standardVerticalSpacing;
				EditorGUI.PropertyField(position, property, label, true);

				using (DisabledScope.False)
					copied = GUI.Button(definitionRect, TempUtility.Content(colorized.Value.RichText), hovered ? HoverStyle : NormalStyle);
			}

			// Rect copyButton = definitionRect;
			// copyButton.xMin = copyButton.xMax - 20f;
			// copyButton.height = 20f;

			// using (IconSizeScope.Mini)
			// using (DisabledScope.False)
			// {
			// 	if (GUI.Button(copyButton, TempUtility.Content(null, CopyIcon), EditorStyles.miniButton))
			// 	{
			// 		EditorGUIUtility.systemCopyBuffer = colorized.Value.PlainText;
			// 	}
			// }

			if (copied)
			{
				EditorGUIUtility.systemCopyBuffer = colorized.Value.PlainText;
			}
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			if (property.depth > 0 || CurrentDrawType == DrawType.No)
				return EditorGUI.GetPropertyHeight(property, label, property.isExpanded);

			colorized ??= Colorizer.DeclarationContent(Member(property));


			float prop = EditorGUI.GetPropertyHeight(property, label, property.isExpanded);
			float drawer = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

			float spacing = property.NextVisible(false) ? EditorGUIUtility.standardVerticalSpacing * 1.5f : 0f;

			if (CurrentDrawType == DrawType.TwoColumn)
				return Mathf.Max(prop, drawer) + spacing;
			return prop + drawer + spacing + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}