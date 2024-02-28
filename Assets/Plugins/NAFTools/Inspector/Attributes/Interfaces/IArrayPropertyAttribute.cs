using System;

namespace NAF.Inspector
{
	public interface IArrayPropertyAttribute
	{
		bool DrawOnArray { get; }
		bool DrawOnElements { get; }
		bool DrawOnField { get; }

	#if UNITY_EDITOR
		public static bool DrawOnProperty(IArrayPropertyAttribute attribute, UnityEditor.SerializedProperty property)
		{
			if (!attribute.DrawOnElements && !attribute.DrawOnArray && !attribute.DrawOnField)
				// TODO: Make this a drawer?
				throw new InvalidOperationException($"Attribute {attribute.GetType().Name} extends '{nameof(IArrayPropertyAttribute)}' but does not draw on the array nor the elements.");

			if (property.propertyPath.EndsWith("]"))
				return attribute.DrawOnElements;
			else if (property.isArray && property.propertyType != UnityEditor.SerializedPropertyType.String)
				return attribute.DrawOnArray;
			else return attribute.DrawOnField;
		}
	#endif
	}
}