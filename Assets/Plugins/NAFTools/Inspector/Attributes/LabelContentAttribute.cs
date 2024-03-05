
using System;
using UnityEngine;

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
	[AttributeUsage(System.AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class LabelContentAttribute : PropertyAttribute, IContentAttribute, IArrayPropertyAttribute
	{
		public virtual object? Label { get; set; }
		public virtual object? Tooltip { get; set; }
		public virtual object? Icon { get; set; }
		public virtual object? Style { get; set; }

		private bool _drawOnArray = true;
		public bool DrawOnArray { get => _drawOnArray; set => _drawOnArray = value; }
		public bool DrawOnElements { get => !DrawOnArray; set => DrawOnArray = !value; }
		public bool DrawOnField => true;

		public LabelContentAttribute()
		{
			order = -100;
		}
	}
}
#nullable restore