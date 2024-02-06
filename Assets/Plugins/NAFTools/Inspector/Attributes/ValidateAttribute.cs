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
	public class ValidateAttribute : InlineLabelAttribute, IConditionalAttribute
	{
		public object Condition { get; }
		public bool Invert { get; set; }

		public override object? Label { get; set; } = "Invalid";
		public override object? Icon { get; set; } = EditorIcons.d_console_erroricon;
		public override object? Style { get; set; }

		public ValidateAttribute(object condition) : base()
		{
			Condition = condition;
		}
	}

	[AttributeUsage(System.AttributeTargets.Field, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class RequiredAttribute : ValidateAttribute
	{
		public override object? Label { get; set; } = "Required";
		public override object? Tooltip { get; set; } = "This field should not be null or the default value.";

		public RequiredAttribute() : base("{1}")
		{
		}
	}
}
#nullable restore