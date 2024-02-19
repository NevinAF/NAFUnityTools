namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using System;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(InlineButtonAttribute))]
	public class InlineButtonAttributeDrawer : InlineLabelAttributeDrawer
	{
		private Func<object, object, object> method;

		protected override Task OnEnable(in SerializedProperty property)
		{
			return Task.WhenAll(
				base.OnEnable(property),
				PropertyFieldCompiler<object>.Load(property, ((InlineButtonAttribute)Attribute).Expression).ContinueWith(t =>
				{
					method = t.Result;
				})
			);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			InlineButtonAttribute attribute = (InlineButtonAttribute)Attribute;
			method = PropertyFieldCompiler<object>.Get(property, attribute.Expression);
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			InlineButtonAttribute attribute = (InlineButtonAttribute)Attribute;
			attribute.Label ??= ObjectNames.NicifyVariableName(attribute.Expression);

			if (DrawAsButton(attribute.Alignment, position, content, style, property, label))
			{
				var targets = PropertyTargets.GetValues(property);
				for (int i = 0; i < targets.Length; i++)
				{
					try { method(targets.ParentValues[i], targets.FieldValues[i]); }
					catch (Exception e)
					{
						Debug.LogError("There was an error executing method called from a " + nameof(InlineButtonAttribute) + ". See following log for details.");
						Debug.LogException(e);
					}
				}
			}
		}
	}
}