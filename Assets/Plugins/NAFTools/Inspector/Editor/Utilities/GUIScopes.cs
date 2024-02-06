namespace NAF.Inspector.Editor
{
	using System;
	using UnityEngine;
	using UnityEditor;

	public readonly struct IndentScope : IDisposable
	{
		private readonly int _previous;

		public static IndentScope Zero => new IndentScope(0);

		public IndentScope(int newValue = 0)
		{
			this._previous = EditorGUI.indentLevel;
			EditorGUI.indentLevel = newValue;
		}

		public readonly void Dispose()
		{
			EditorGUI.indentLevel = this._previous;
		}
	}

	public readonly struct DisabledScope : IDisposable
	{
		private readonly bool _previous;

		public static DisabledScope False => new DisabledScope(false);
		public static DisabledScope True => new DisabledScope(true);

		public DisabledScope(bool newValue = true)
		{
			this._previous = GUI.enabled;
			GUI.enabled = !newValue;
		}

		public readonly void Dispose()
		{
			GUI.enabled = this._previous;
		}
	}

	public readonly struct LabelWidthScope : IDisposable
	{
		private static float? _kMiniLabelW;
		private static float kMiniLabelW => _kMiniLabelW ?? (_kMiniLabelW = EditorStyles.label.CalcSize(new GUIContent("W")).x).Value;

		private readonly float _previous;
		public static LabelWidthScope Zero => new LabelWidthScope(0);

		public LabelWidthScope(float newValue = 0)
		{
			this._previous = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth += newValue;
		}

		public LabelWidthScope(GUIContent label, float prefixLabelWidth = -1)
		{
			this._previous = EditorGUIUtility.labelWidth;
			EditorGUIUtility.labelWidth = GetWidth(label, prefixLabelWidth);
		}

		public readonly void Dispose()
		{
			EditorGUIUtility.labelWidth = this._previous;
		}

		private static float GetWidth(GUIContent Label, float prefixLabelWidth = -1)
		{
			float LabelWidth = EditorStyles.label.CalcSize(Label).x;
			if (LabelWidth > kMiniLabelW)
				return prefixLabelWidth > 0f ? prefixLabelWidth + LabelWidth : LabelWidth;
			else
				return prefixLabelWidth > 0f ? prefixLabelWidth + kMiniLabelW : kMiniLabelW;
		}
	}

	public readonly struct IconSizeScope : IDisposable
	{
		private readonly Vector2 _previous;


		public static IconSizeScope Mini => new IconSizeScope(new Vector2(12, 12));
		public static IconSizeScope Small => new IconSizeScope(new Vector2(16, 16));
		public static IconSizeScope Normal => new IconSizeScope(new Vector2(32, 32));
		public static IconSizeScope Large => new IconSizeScope(new Vector2(64, 64));

		public IconSizeScope(Vector2 newValue)
		{
			this._previous = EditorGUIUtility.GetIconSize();
			EditorGUIUtility.SetIconSize(newValue);
		}

		public readonly void Dispose()
		{
			EditorGUIUtility.SetIconSize(this._previous);
		}
	}

	public readonly struct ColorScope : IDisposable
	{
		private readonly Color _previous;

		public ColorScope(Color newValue)
		{
			this._previous = GUI.color;
			GUI.color = newValue;
		}

		public readonly void Dispose()
		{
			GUI.color = this._previous;
		}
	}

	public readonly struct ContentColorScope : IDisposable
	{
		private readonly Color _previous;

		public ContentColorScope(Color newValue)
		{
			this._previous = GUI.contentColor;
			GUI.contentColor = newValue;
		}

		public readonly void Dispose()
		{
			GUI.contentColor = this._previous;
		}
	}
}