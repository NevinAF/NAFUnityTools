#nullable enable
namespace NAF.Inspector.Editor
{
	using UnityEditor;
	using UnityEngine;

	public static class InlineGUI
	{
		public static Rect GetInlinePosition(ref Rect position, float width, bool right, float padding = 2f)
		{
			width = Mathf.Min(width, position.width - padding);

			if (!right)
				width = Mathf.Min(width, EditorGUIUtility.labelWidth - padding);

			Rect result = position;
			result.width = width;

			if (!right) // left
			{
				position.x += width + padding;
				position.width -= width + padding;
			}
			else // right
			{
				position.width -= width + padding;
				result.x += position.width + padding;
			}

			return result;
		}

		public static Vector2 GetInlineSize(float maxHeight, GUIContent content, GUIStyle style)
		{
			using (IconSizeScope.Mini)
				return style.CalcSize(content);
		}

		public static Rect GetInlinePosition(ref Rect position, GUIContent content, GUIStyle style, bool right = true, float verticalAlignment = 0.5f, float padding = 2f)
		{
			LimitText(content);
			Vector2 size = GetInlineSize(position.height, content, style);
			Rect result = GetInlinePosition(ref position, size.x, right, padding);

			if (verticalAlignment >= 0f && verticalAlignment <= 1f && size.y < result.height)
			{
				result.y += (result.height - size.y) * verticalAlignment;
				result.height = size.y;
			}

			return result;
		}

		public static void LimitText(GUIContent content, int limit = 19)
		{
			if (string.IsNullOrEmpty(content.text)) return;

			if (content.text.Length > limit)
			{
				if (content.tooltip != null)
				{
					if (!content.tooltip.StartsWith(content.text))
						content.tooltip = content.text + ": " + content.tooltip;
				}
				else content.tooltip = content.text;

				content.text = content.text.Substring(0, limit - 1) + "..";
			}
			else if (!string.IsNullOrEmpty(content.tooltip) && !content.text.EndsWith("*"))
			{
				content.text += "*";
			}
		}

		public static bool InlineButton(ref Rect position, GUIContent content, GUIStyle style, bool right = true, float verticalAlignment = 0.5f) => InlineButton(ref position, content, style, out _, right, verticalAlignment);

		public static bool InlineButton(ref Rect position, GUIContent content, GUIStyle style, out Rect inlinePosition, bool right = true, float verticalAlignment = 0.5f)
		{
			if (string.IsNullOrEmpty(content.text) && content.image == null)
			{
				inlinePosition = position;
				return false;
			}

			inlinePosition = GetInlinePosition(ref position, content, style, right, verticalAlignment);

			using (IndentScope.Zero)
			using (IconSizeScope.Mini)
				return GUI.Button(inlinePosition, content, style);
		}

		public static void InlineLabel(ref Rect position, GUIContent content, GUIStyle style, bool right = true, float verticalAlignment = 0.5f) => InlineLabel(ref position, content, style, out _, right, verticalAlignment);

		public static void InlineLabel(ref Rect position, GUIContent content, GUIStyle style, out Rect inlinePosition, bool right = true, float verticalAlignment = 0.5f)
		{
			if (string.IsNullOrEmpty(content.text) && content.image == null)
			{
				inlinePosition = position;
				return;
			}

			inlinePosition = GetInlinePosition(ref position, content, style, right, verticalAlignment);

			using (IndentScope.Zero)
			using (IconSizeScope.Mini)
				GUI.Label(inlinePosition, content, style);
		}

		public static bool InlineToggle(ref Rect position, bool value, bool right = true, float verticalAlignment = 0.5f) => InlineToggle(ref position, value, out _, right, verticalAlignment);

		public static bool InlineToggle(ref Rect position, bool value, out Rect inlinePosition, bool right = true, float verticalAlignment = 0.5f)
		{
			inlinePosition = GetInlinePosition(ref position, 15, right);
			inlinePosition.y += (inlinePosition.height - 15) * verticalAlignment;
			inlinePosition.height = 15;

			using (IndentScope.Zero)
				return EditorGUI.Toggle(inlinePosition, value);
		}

		public static bool InlineToggle(ref Rect position, SerializedProperty property, bool right = true, float verticalAlignment = 0.5f) => InlineToggle(ref position, property, out _, right, verticalAlignment);

		public static bool InlineToggle(ref Rect position, SerializedProperty property, out Rect inlinePosition, bool right = true, float verticalAlignment = 0.5f)
		{
			if (property.propertyType != SerializedPropertyType.Boolean)
			{
				Debug.Log("SerializedProperty must be of type bool.");
				inlinePosition = position;
				return false;
			}

			return property.boolValue = InlineToggle(ref position, property.boolValue, out inlinePosition, right, verticalAlignment);
		}

		public static int InlinePopup(ref Rect position, int selectedIndex, GUIContent[] options, GUIStyle? style = null, bool right = true, float verticalAlignment = 0.5f) => InlinePopup(ref position, selectedIndex, options, out _, style, right, verticalAlignment);

		public static int InlinePopup(ref Rect position, int selectedIndex, GUIContent[] options, out Rect inlinePosition, GUIStyle? style = null, bool right = true, float verticalAlignment = 0.5f)
		{
			style ??= EditorStyles.popup;

			inlinePosition = GetInlinePosition(ref position, options[selectedIndex], style, right, verticalAlignment);

			using (IndentScope.Zero)
				return EditorGUI.Popup(inlinePosition, selectedIndex, options, style);
		}
	}
}
#nullable restore