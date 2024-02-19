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
		private bool _cResult;

		protected override Task OnEnable(in SerializedProperty property)
		{
			return AttributeEvaluator.Load((IConditionalAttribute)Attribute, property);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			_cResult = AttributeEvaluator.Conditional((IConditionalAttribute)Attribute, property);
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			using (new DisabledScope(_cResult))
				base.OnGUI(position, property, label);
		}
	}
}