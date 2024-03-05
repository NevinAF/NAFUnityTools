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
	public class IndentAttribute : PropertyAttribute, IArrayPropertyAttribute
	{
		public bool DrawOnArray => true;
		public bool DrawOnElements => false;
		public bool DrawOnField => true;

		public int Indent { get; }

		public IndentAttribute(int indent = 1)
		{
			Indent = indent;
			this.order = int.MinValue;
		}
	}
}