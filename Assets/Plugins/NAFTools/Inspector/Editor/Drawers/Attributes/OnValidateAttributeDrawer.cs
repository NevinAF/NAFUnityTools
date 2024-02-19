namespace NAF.Inspector.Editor
{
	using System;
	using System.ComponentModel;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(OnValidateAttribute))]
	public class OnValidateAttributeDrawer : NAFPropertyDrawer
	{
		private Func<object, object, object> method;

		protected override Task OnEnable(in SerializedProperty property)
		{
			var attribute = (OnValidateAttribute)Attribute;

			return PropertyFieldCompiler<object>.Load(property, attribute.Expression).ContinueWith(t =>
			{
				method = t.Result;
			});
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			var targets = PropertyTargets.GetValues(property);
			for (int i = 0; i < targets.Length; i++)
			{
				try { method(targets.ParentValues[i], targets.FieldValues[i]); }
				catch (Exception e)
				{
					Debug.LogError("There was an error executing method called from a " + nameof(OnValidateAttribute) + ". See following log for details.");
					Debug.LogException(e);
				}
			}
		}
	}
}