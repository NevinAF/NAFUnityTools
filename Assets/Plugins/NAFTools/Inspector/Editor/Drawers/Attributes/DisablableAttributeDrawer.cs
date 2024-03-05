namespace NAF.Inspector.Editor
{
	using System;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(DisablableAttribute))]
	public class DisablableAttributeDrawer : NAFPropertyDrawer
	{
		private AttributeExprCache<bool> disabled;

		protected override async Task OnEnable()
		{
			DisablableAttribute attribute = (DisablableAttribute)Attribute;

			if (attribute.Disabled is string temp)
			{
				string expression = "={1}==";
				if (temp[0] == PropertyFieldCompiler.ExpressionSymbol)
					expression += $"({temp.Substring(1)})";
				else expression += $"({temp})";

				disabled = await AttributeExpr<bool>.AsyncCreate(Tree.Property, expression);
				return;
			}

			Func<object, object, bool> func;

			if (attribute.Disabled is not null)
			{
				object converted;

				if (Tree.PropertyType != attribute.Disabled.GetType())
					converted = Convert.ChangeType(attribute.Disabled, Tree.PropertyType);
				else converted = attribute.Disabled;

				func = (object parent, object field) => field.Equals(converted);
			}
			else {
				func = (object parent, object field) => !PropertyFieldCompiler<bool>.Caster(field);
			}

			disabled = AttributeExpr<bool>.Constant(func);

			await base.OnEnable();
			return;
		}

		protected override void OnUpdate()
		{
			base.OnUpdate();
			disabled.Refresh(Tree.Property);
		}

		private void SetValue(object value)
		{
			PropertyTargets.Result targets = PropertyTargets.Resolve(Tree.Property);
			if (value is string temp)
			{
				string expression = "={1}=";
				if (temp[0] == PropertyFieldCompiler.ExpressionSymbol)
					expression += $"({temp.Substring(1)})";
				else expression += $"({temp})";

				var func = PropertyFieldCompiler<object>.GetOrCreate(Tree.Property, expression);

				for (int i = 0; i < targets.Length; i++)
					func(targets.ParentValues[i], targets.FieldValues[i]);
			}
			else if (value is not null)
			{
				object converted;

				if (Tree.PropertyType != value.GetType())
					converted = Convert.ChangeType(value, Tree.PropertyType);
				else converted = value;

				for (int i = 0; i < targets.Length; i++)
					Tree.FieldInfo.SetValue(targets.ParentValues[i], converted);
			}
			else {
				object converted = Tree.PropertyType.IsValueType ? Activator.CreateInstance(Tree.PropertyType) : null;
				for (int i = 0; i < targets.Length; i++)
					Tree.FieldInfo.SetValue(targets.ParentValues[i], converted);
			}

			// Make the property dirty so the changes are saved
			Tree.Property.serializedObject.ApplyModifiedProperties();
		}

		protected override void OnGUI(Rect position)
		{
			// Adds a toggle button to the right of the value property. Value property draws like normal

			bool lastMulti = EditorGUI.showMixedValue;

			EditorGUI.showMixedValue = disabled.MultipleValues;
			bool newDisabled = InlineGUI.InlineToggle(ref position, disabled);

			if (newDisabled != disabled)
			{
				DisablableAttribute attribute = (DisablableAttribute)Attribute;
				object value = newDisabled ? attribute.Disabled : attribute.EnabledDefault;
				SetValue(value);
			}

			EditorGUI.showMixedValue = lastMulti;

			using (new DisabledScope(newDisabled))
				base.OnGUI(position);
		}

		protected override float OnGetHeight()
		{
			return base.OnGetHeight();
		}
	}
}