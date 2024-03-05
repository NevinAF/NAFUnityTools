namespace NAF.Inspector
{
	using System;
	using UnityEngine;
	
	[AttributeUsage(System.AttributeTargets.Field | AttributeTargets.Class | AttributeTargets.Struct, Inherited = true, AllowMultiple = true)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class RuntimeDebugAttribute : PropertyAttribute, IArrayPropertyAttribute
	{
		public bool DrawOnArray => true;
		public bool DrawOnElements => false;
		public bool DrawOnField => true;
	}
}