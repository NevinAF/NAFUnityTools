namespace NAF.Inspector.Editor
{
	using System.Reflection;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(HideIfAttribute))]
	[CustomPropertyDrawer(typeof(ShowIfAttribute))]
	public class HideIfAttributeDrawer : NAFPropertyDrawer
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

		protected override float OnGetHeight()
		{
			return conditional ? 0f : base.OnGetHeight();
		}

		protected override void OnGUI(Rect position)
		{
			if (!conditional)
				base.OnGUI(position);
		}

		// Don't show while loading?
		protected override void LoadingGUI(Rect position)
		{
		}

		protected override float GetLoadingHeight()
		{
			return 0f;
		}
	}
}