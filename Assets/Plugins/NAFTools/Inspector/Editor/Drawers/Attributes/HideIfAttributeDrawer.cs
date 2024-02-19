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
		private bool _cResult;

		protected override Task OnEnable(in SerializedProperty property)
		{
			return AttributeEvaluator.Load((IConditionalAttribute)Attribute, property);
		}

		protected override void OnUpdate(SerializedProperty property)
		{
			_cResult = AttributeEvaluator.Conditional((IConditionalAttribute)Attribute, property);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			if (_cResult)
			{
				property.NextVisible(false);
				return 0f;
			}
			else return base.OnGetHeight(property, label);
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_cResult)
			{
				property.NextVisible(false);
			}
			else base.OnGUI(position, property, label);
		}
	}
}