#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using UnityEditor;
	using UnityEngine;

	public static class TempUtility
	{
		private static readonly GUIContent _tempContent = new GUIContent();

		public static Texture? EditorTexture(string? icon)
		{
			return string.IsNullOrEmpty(icon) ? null : EditorGUIUtility.IconContent(icon).image;
		}

		public static GUIContent Content(string? text, Texture? image = null, string? tooltip = null)
		{
			_tempContent.text = text;
			_tempContent.tooltip = tooltip;
			_tempContent.image = image;
			return _tempContent;
		}

		public static GUIContent Content(string? text, string? image, string? tooltip = null)
		{
			_tempContent.text = text;
			_tempContent.tooltip = tooltip;
			_tempContent.image = EditorTexture(image);
			return _tempContent;
		}

		public static bool AllEqual<T>(this Span<T> array)
		{
			if (array.Length <= 1)
				return true;

			T first = array[0];

			if (first == null)
			{
				for (int i = 1; i < array.Length; i++)
				{
					if (array[i] != null)
						return false;
				}
			}
			else for (int i = 1; i < array.Length; i++)
			{
				if (!first.Equals(array[i]))
					return false;
			}
			return true;
		}

		public static string AppendLine(this string? str, string value)
		{
			if (string.IsNullOrEmpty(str))
				return value;
			else
				return str + "\n" + value;
		}
	}
}
#nullable restore