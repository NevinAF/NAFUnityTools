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

	[AttributeUsage(System.AttributeTargets.All, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class MemberDefinitionDrawerAttribute : PropertyAttribute, IArrayPropertyAttribute
	{
		public bool DrawOnArray => true;
		public bool DrawOnElements => false;
		public bool DrawOnField => true;

		public MemberDefinitionDrawerAttribute()
		{
			order = int.MinValue;
		}
	}
}