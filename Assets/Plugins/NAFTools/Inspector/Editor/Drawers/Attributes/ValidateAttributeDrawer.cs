namespace NAF.Inspector.Editor
{
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(ValidateAttribute))]
	[CustomPropertyDrawer(typeof(RequiredAttribute))]
	public class ValidateAttributeDrawer : InlineLabelAttributeDrawer
	{
		private AttributeExprCache<bool> conditional;

		protected override async Task OnEnable()
		{
			Task baseTask = base.OnEnable();
			conditional = await AttributeEvaluator.Conditional((IConditionalAttribute)Attribute, Tree.Property);
			await baseTask;
		}


		protected override void OnUpdate()
		{
			content.Refresh(Tree.Property);
			style.Refresh(Tree.Property, EditorStyles.helpBox);
			conditional.Refresh(Tree.Property);
		}

		protected override void OnGUI(Rect position)
		{
			if (!conditional)
			{
				base.OnGUI(position);
				return;
			}

			Tree.OnGUI(position);
		}
	}
}