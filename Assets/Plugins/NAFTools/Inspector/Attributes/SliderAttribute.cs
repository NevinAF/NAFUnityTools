/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
namespace NAF.Inspector
{
	using System;
	using UnityEngine;

	[AttributeUsage(AttributeTargets.Class | AttributeTargets.Field | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class SliderAttribute : PropertyAttribute
	{
		public object Min { get; }
		public object Max { get; }

		public SliderAttribute(object min, object max)
		{
			Min = min;
			Max = max;
		}
	}
}