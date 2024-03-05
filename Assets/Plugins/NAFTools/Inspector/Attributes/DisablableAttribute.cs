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

	[AttributeUsage(System.AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DisablableAttribute : PropertyAttribute, IArrayPropertyAttribute
	{
		public bool DrawOnArray => true;
		public bool DrawOnElements => false;
		public bool DrawOnField => true;

		public object EnabledDefault;
		public object Disabled;

		public DisablableAttribute() { }

		public DisablableAttribute(object enabledDefault)
		{
			EnabledDefault = enabledDefault;
		}
	}
}