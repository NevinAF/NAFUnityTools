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

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DescriptionAttribute : PropertyAttribute, IContentAttribute, IArrayPropertyAttribute
	{
		public object? Label { get; }
		public virtual object? Tooltip { get; set; }
		public virtual object? Icon { get; set; }
		public virtual object? Style { get; set; }

		private bool _drawOnArray = true;
		public bool DrawOnArray { get => _drawOnArray; set => _drawOnArray = value; }
		public bool DrawOnElements { get => !DrawOnArray; set => DrawOnArray = !value; }
		public bool DrawOnField => true;

		public DescriptionAttribute(object label)
		{
			this.Label = label;
		}
	}
}
#nullable restore