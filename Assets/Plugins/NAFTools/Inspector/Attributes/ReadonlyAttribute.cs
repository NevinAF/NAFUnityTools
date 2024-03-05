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

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class ReadonlyAttribute : PropertyAttribute, IConditionalAttribute, IArrayPropertyAttribute
	{
		public object? Condition { get; set; }
		public bool Invert { get; set; }

		public bool _drawOnArray = true;
		public bool DrawOnArray { get => _drawOnArray; set => _drawOnArray = value; }
		public bool DrawOnElements { get => !DrawOnArray; set => DrawOnArray = !value; }
		public bool DrawOnField => true;
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DisableIfAttribute : ReadonlyAttribute
	{
		public DisableIfAttribute(object conditionMethod)
		{
			Condition = conditionMethod;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class EnableIfAttribute : ReadonlyAttribute
	{
		public EnableIfAttribute(object conditionMethod)
		{
			Condition = conditionMethod;
			Invert = !Invert;
		}
	}

	[AttributeUsage(AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class DisableOnPlayAttribute : ReadonlyAttribute
	{
		public DisableOnPlayAttribute()
		{
		#if UNITY_EDITOR
			Condition = UnityEditor.EditorApplication.isPlaying;
		#endif
		}
	}
}