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
	public class DescriptionAttribute : PropertyAttribute, IContentAttribute
	{
		public object? Label { get; }
		public virtual object? Tooltip { get; set; }
		public virtual object? Icon { get; set; }
		public virtual object? Style { get; set; }

		public DescriptionAttribute(object label)
		{
			this.Label = label;
		}
	}
}
#nullable restore