namespace NAF.Inspector.Editor
{
	using System.Diagnostics;
	using System.Reflection;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using NAF.ExpressionCompiler;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(MemberDefinitionDrawerAttribute))]
	public class MemberDefinitionDrawerAttributeDrawer : NAFPropertyDrawer
	{
		public enum DrawType
		{
			No,
			Inline,
			TwoColumn
		}

		public static DrawType CurrentDrawType = DrawType.Inline;
		public const float ExpandedScalar = 0.4f;

		private static readonly ObjectPool<MemberColorizer> _colorizerPool = new ObjectPool<MemberColorizer>(() => new UnityMemberColorizer(typeof(MemberDefinitionDrawerAttribute), typeof(DebuggerBrowsableAttribute)));

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
		protected override Task OnEnable(in SerializedProperty property)
		{
			MemberInfo member = Member(property);

			if (member.Name.StartsWith("__") && member.Name.EndsWith("__"))
			{
				colorized = null;
				return Task.CompletedTask;
			}

			return Task.Run(() => {
				MemberColorizer mc = _colorizerPool.Get();
				colorized = mc.DeclarationContent(member);
				_colorizerPool.Return(mc);
			});
		}

		private MemberInfo Member(SerializedProperty property)
		{
			if (FieldInfo != null)
				return FieldInfo;

			if (property.name == "m_Script")
				return property.serializedObject.targetObject.GetType();

			return null;
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.depth > 0 || CurrentDrawType == DrawType.No || colorized == null)
			{
				base.OnGUI(position, property, label);
				return;
			}

			Rect definitionRect = position;
			bool copied;

			if (CurrentDrawType == DrawType.TwoColumn)
			{
				Rect totalRect = position;
				totalRect.width = EditorGUIUtility.currentViewWidth / ExpandedScalar;
				bool hovered = totalRect.Contains(Event.current.mousePosition);


				definitionRect.x += EditorGUIUtility.currentViewWidth + EditorGUIUtility.standardVerticalSpacing;
				definitionRect.width = totalRect.width - definitionRect.x - EditorGUIUtility.standardVerticalSpacing;
				definitionRect.height = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

				// position.height = propHeight;
				base.OnGUI(position, property, label);


				using (DisabledScope.False)
					copied = GUI.Button(definitionRect, TempUtility.Content(colorized.Value.RichText, tooltip: "Click to copy!"), hovered ? HoverStyle : NormalStyle);
			}
			else
			{
				bool hovered = position.Contains(Event.current.mousePosition);

				definitionRect.height = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

				position.yMin += definitionRect.height + EditorGUIUtility.standardVerticalSpacing;
				base.OnGUI(position, property, label);

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

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			if (property.depth > 0 || CurrentDrawType == DrawType.No || colorized == null)
				return base.OnGetHeight(property, label);

			float prop = base.OnGetHeight(property, label);
			float drawer = NormalStyle.margin.vertical + NormalStyle.lineHeight * colorized.Value.Lines;

			float spacing = UnityInternals.SerializedProperty_isValid(property) ? EditorGUIUtility.standardVerticalSpacing * 1.5f : 0f;

			if (CurrentDrawType == DrawType.TwoColumn)
				return Mathf.Max(prop, drawer) + spacing;
			return prop + drawer + spacing + EditorGUIUtility.standardVerticalSpacing;
		}
	}
}