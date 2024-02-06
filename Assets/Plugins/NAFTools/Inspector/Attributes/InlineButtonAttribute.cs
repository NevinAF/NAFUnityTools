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

	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class InlineButtonAttribute : InlineLabelAttribute
	{
		public string Expression { get; }

		public InlineButtonAttribute(string method)
		{
			Expression = method;
		}
	}
}