/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
#nullable enable
namespace NAF.Inspector
{
	using System;
	using UnityEngine;

	public enum LabelAlignment
	{
		Left,
		BetweenLeft,
		BetweenRight,
		Right
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class InlineLabelAttribute : PropertyAttribute, IContentAttribute, IArrayPropertyAttribute
	{
		public virtual object? Label { get; set; }
		public virtual object? Tooltip { get; set; }
		public virtual object? Icon { get; set; }
		public virtual object? Style { get; set; }
		public virtual LabelAlignment Alignment { get; set; } = LabelAlignment.Right;

		private bool _drawOnArray = true;
		public bool DrawOnArray { get => _drawOnArray; set => _drawOnArray = value; }
		public bool DrawOnElements { get => !DrawOnArray; set => DrawOnArray = !value; }
		public bool DrawOnField => true;

		public InlineLabelAttribute(int order = 0)
		{
			if (Alignment == LabelAlignment.Left)
				order--;
			this.order = order;
		}
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class SuffixAttribute : InlineLabelAttribute
	{
		public override LabelAlignment Alignment { get; set; } = LabelAlignment.Right;

		public SuffixAttribute(object label)
		{
			this.Label = label;
		}
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class PrefixAttribute : InlineLabelAttribute
	{
		public override LabelAlignment Alignment { get; set; } = LabelAlignment.Left;

		public PrefixAttribute(object label)
		{
			this.Label = label;
		}
	}
}
#nullable restore