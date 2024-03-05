namespace NAF.Inspector.Editor
{
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(ReadonlyAttribute))]
	[CustomPropertyDrawer(typeof(DisableIfAttribute))]
	[CustomPropertyDrawer(typeof(EnableIfAttribute))]
	public class ReadonlyAttributeDrawer : NAFPropertyDrawer
	{
		private AttributeExprCache<bool> conditional;

		protected override async Task OnEnable()
		{
			conditional = await AttributeEvaluator.Conditional((IConditionalAttribute)Attribute, Tree.Property);
		}

		protected override void OnUpdate()
		{
			conditional.Refresh(Tree.Property);
		}


		protected override void OnGUI(Rect position)
		{
			using (new DisabledScope(conditional))
				base.OnGUI(position);
		}

		protected override void LoadingGUI(Rect position)
		{
			using (new DisabledScope(true))
				base.LoadingGUI(position);
		}
	}
}