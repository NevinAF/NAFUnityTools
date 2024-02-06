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

		public override Task TryEnable(in SerializedProperty property)
		{
			var attribute = this.attribute as OnValidateAttribute;

			return PropertyFieldCompiler<object>.Load(property, attribute.Expression).ContinueWith(t =>
			{
				method = t.Result;
			});
		}

		public override void TryUpdate(SerializedProperty property)
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

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			EditorGUI.PropertyField(position, property, label, true);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}
	}
}