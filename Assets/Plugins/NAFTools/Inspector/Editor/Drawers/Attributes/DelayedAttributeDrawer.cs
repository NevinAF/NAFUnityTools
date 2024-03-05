namespace NAF.Inspector.Editor
{
	using System;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(DelayedAttribute))]
	public class DelayedAttributeDrawer : NAFPropertyDrawer
	{
		public override bool EndsDrawing => true;

		protected override void OnGUI(Rect position)
		{
			DrawDelayedField(position, Tree.Property);
		}

		protected override float OnGetHeight()
		{
			return UnityInternals.EditorGUI_GetSinglePropertyHeight(Tree.Property, Tree.PropertyLabel);
		}

		public static void DrawDelayedField(Rect position, SerializedProperty property, GUIContent label = null)
		{
			if (label != null)
				EditorGUI.LabelField(position, label);
			position = EditorGUI.PrefixLabel(position, label);

			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					property.intValue = EditorGUI.DelayedIntField(position, property.displayName, property.intValue);
					break;
				case SerializedPropertyType.Float:
					if (property.numericType == SerializedPropertyNumericType.Float)
						property.floatValue = EditorGUI.DelayedFloatField(position, property.displayName, property.floatValue);
					else property.doubleValue = EditorGUI.DelayedDoubleField(position, property.displayName, property.doubleValue);
					break;
				case SerializedPropertyType.Vector2: {
					Vector2 value = property.vector2Value;
					Span<float> floats = stackalloc float[2] { value.x, value.y };
					MultiDelayedFieldInternal(position, floats, VECTOR_Labels);
					property.vector2Value = new Vector2(floats[0], floats[1]);
					break;
				}
				case SerializedPropertyType.Vector3: {
					Vector3 value = property.vector3Value;
					Span<float> floats = stackalloc float[3] { value.x, value.y, value.z };
					MultiDelayedFieldInternal(position, floats, VECTOR_Labels);
					property.vector3Value = new Vector3(floats[0], floats[1], floats[2]);
					break;
				}
				case SerializedPropertyType.Vector4: {
					Vector4 value = property.vector4Value;
					Span<float> floats = stackalloc float[4] { value.x, value.y, value.z, value.w };
					MultiDelayedFieldInternal(position, floats, VECTOR_Labels);
					property.vector4Value = new Vector4(floats[0], floats[1], floats[2], floats[3]);
					break;
				}
				case SerializedPropertyType.Rect: {
					Rect value = property.rectValue;
					Span<float> floats = stackalloc float[4] { value.x, value.y, value.width, value.height };
					MultiDelayedFieldInternal(position, floats, RECT_Labels);
					property.rectValue = new Rect(floats[0], floats[1], floats[2], floats[3]);
					break;
				}
				case SerializedPropertyType.RectInt: {
					RectInt value = property.rectIntValue;
					Span<int> ints = stackalloc int[4] { value.x, value.y, value.width, value.height };
					MultiDelayedFieldInternal(position, ints, RECT_Labels);
					property.rectIntValue = new RectInt(ints[0], ints[1], ints[2], ints[3]);
					break;
				}
				case SerializedPropertyType.String:
					property.stringValue = EditorGUI.DelayedTextField(position, property.displayName, property.stringValue);
					break;
				default:
					throw new NotImplementedException($"DelayedAttribute does not support {property.propertyType} type.");

			}
		}

		public const string VECTOR_Labels = "XYZW";
		public const string RECT_Labels = "XYWH";

		public static void MultiDelayedFieldInternal(Rect position, Span<int> values, string labels)
		{
			const float kSpacingSubLabel = 4f;

			int eCount = values.Length;
			float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
			Rect nr = new Rect(position) {width = w};

			using (IndentScope.Zero)
			{
				for (int i = 0; i < values.Length; i++)
				{
					using (LabelWidthScope.MiniLabelW)
						values[i] = EditorGUI.DelayedIntField(nr, TempUtility.Content(labels[i].ToString()), values[i]);

					nr.x += w + kSpacingSubLabel;
				}
			}
		}

		public static void MultiDelayedFieldInternal(Rect position, Span<float> values, string labels)
		{
			const float kSpacingSubLabel = 4f;

			int eCount = values.Length;
			float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
			Rect nr = new Rect(position) {width = w};

			using (IndentScope.Zero)
			{
				for (int i = 0; i < values.Length; i++)
				{
					using (LabelWidthScope.MiniLabelW)
						values[i] = EditorGUI.DelayedFloatField(nr, TempUtility.Content(labels[i].ToString()), values[i]);

					nr.x += w + kSpacingSubLabel;
				}
			}
		}
	}
}