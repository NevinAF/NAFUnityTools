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

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class HideIfAttribute : PropertyAttribute, IConditionalAttribute
	{
		public object Condition { get; }
		public bool Invert { get; set; }

		public HideIfAttribute(object conditionMethod)
		{
			Condition = conditionMethod;
		}
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class ShowIfAttribute : HideIfAttribute
	{
		public ShowIfAttribute(object conditionMethod) : base(conditionMethod)
		{
			Invert = !Invert;
		}
	}
}