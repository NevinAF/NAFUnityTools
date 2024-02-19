#nullable enable
namespace NAF.Inspector.Editor
{
	using NAF.Inspector;
	using UnityEngine;
	using UnityEditor;
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Threading.Tasks;

	[CustomPropertyDrawer(typeof(UnitsAttribute))]
	public class UnitsAttributeDrawer : NAFPropertyDrawer
	{
		private static Dictionary<Type, GUIContent[]>? _typeNames;
		private static Dictionary<Type, GUIContent[]> TypeNames => _typeNames ??= new Dictionary<Type, GUIContent[]>()
		{
			{ typeof(UnitsAttribute.Time), Enum.GetNames(typeof(UnitsAttribute.Time)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Distance), Enum.GetNames(typeof(UnitsAttribute.Distance)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Mass), Enum.GetNames(typeof(UnitsAttribute.Mass)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Angle), Enum.GetNames(typeof(UnitsAttribute.Angle)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Acceleration), Enum.GetNames(typeof(UnitsAttribute.Acceleration)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Velocity), Enum.GetNames(typeof(UnitsAttribute.Velocity)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Force), Enum.GetNames(typeof(UnitsAttribute.Force)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Area), Enum.GetNames(typeof(UnitsAttribute.Area)).Select(x => new GUIContent(x, null, null)).ToArray() },
			{ typeof(UnitsAttribute.Volume), Enum.GetNames(typeof(UnitsAttribute.Volume)).Select(x => new GUIContent(x, null, null)).ToArray() },
		};

		protected override Task OnEnable(in SerializedProperty property)
		{
			var propertyType = property.propertyType;
			if (propertyType != SerializedPropertyType.Float &&
				propertyType != SerializedPropertyType.Integer &&
				propertyType != SerializedPropertyType.Vector2 &&
				propertyType != SerializedPropertyType.Vector3 &&
				propertyType != SerializedPropertyType.Vector4 &&
				propertyType != SerializedPropertyType.Vector2Int &&
				propertyType != SerializedPropertyType.Vector3Int)
			{
				throw new NotSupportedException("Property type '" + propertyType + "' is not supported by the UnitsAttribute.");
			}

			return base.OnEnable(property);
		}

		protected override float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			float result = UnityInternals.EditorGUI_GetSinglePropertyHeight(property, label);
			property.NextVisible(false); // FIXME: Skip all other drawers?
			return result;
		}

		protected override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			UnitsAttribute units = (UnitsAttribute)Attribute!;

			int index = InlineGUI.InlinePopup(ref position, Convert.ToInt32(units.value), TypeNames[units.value.GetType()]);
			units.value = Enum.ToObject(units.value.GetType(), index);

			EditorGUI.BeginProperty(position, label, property);

			if (property.propertyType == SerializedPropertyType.Float)
			{
				float value = property.floatValue;
				float converted = UnitsAttribute.Convert(value, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.FloatField(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.floatValue = UnitsAttribute.Convert(converted, units.value, units.storeAs);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector2)
			{
				Vector2 value = property.vector2Value;
				Vector2 converted = new Vector2(
					UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					UnitsAttribute.Convert(value.y, units.storeAs, units.value)
				);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector2Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector2Value = new Vector2(
						UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.y, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector3)
			{
				Vector3 value = property.vector3Value;
				Vector3 converted = new Vector3(
					UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					UnitsAttribute.Convert(value.z, units.storeAs, units.value)
				);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector3Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector3Value = new Vector3(
						UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.y, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.z, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector4)
			{
				Vector4 value = property.vector4Value;
				Vector4 converted = new Vector4(
					UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					UnitsAttribute.Convert(value.z, units.storeAs, units.value),
					UnitsAttribute.Convert(value.w, units.storeAs, units.value)
				);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector4Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector4Value = new Vector4(
						UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.y, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.z, units.value, units.storeAs),
						UnitsAttribute.Convert(converted.w, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Integer)
			{
				int value = property.intValue;
				float converted = UnitsAttribute.Convert(value, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.DelayedFloatField(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.intValue = Mathf.RoundToInt(UnitsAttribute.Convert(converted, units.value, units.storeAs));
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector2Int)
			{
				Vector2Int value = property.vector2IntValue;
				_v2Values[0] = UnitsAttribute.Convert(value.x, units.storeAs, units.value);
				_v2Values[1] = UnitsAttribute.Convert(value.y, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				position = EditorGUI.PrefixLabel(position, label);
				MultiDelayedFloatFieldInternal(position, _v2Labels, _v2Values);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector2IntValue = new Vector2Int(
						Mathf.RoundToInt(UnitsAttribute.Convert(_v2Values[0], units.value, units.storeAs)),
						Mathf.RoundToInt(UnitsAttribute.Convert(_v2Values[1], units.value, units.storeAs))
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector3Int)
			{
				Vector3Int value = property.vector3IntValue;
				_v3Values[0] = UnitsAttribute.Convert(value.x, units.storeAs, units.value);
				_v3Values[1] = UnitsAttribute.Convert(value.y, units.storeAs, units.value);
				_v3Values[2] = UnitsAttribute.Convert(value.z, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				position = EditorGUI.PrefixLabel(position, label);
				MultiDelayedFloatFieldInternal(position, _v3Labels, _v3Values);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector3IntValue = new Vector3Int(
						Mathf.RoundToInt(UnitsAttribute.Convert(_v3Values[0], units.value, units.storeAs)),
						Mathf.RoundToInt(UnitsAttribute.Convert(_v3Values[1], units.value, units.storeAs)),
						Mathf.RoundToInt(UnitsAttribute.Convert(_v3Values[2], units.value, units.storeAs))
					);
				}
			}
			else throw new NotImplementedException("Property type '" + property.propertyType + "' is supported but somehow was not drawn properly.");

			EditorGUI.EndProperty();

			property.NextVisible(false); // FIXME: Skip all other drawers?
		}

		private static GUIContent[] _v2Labels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y") };
		private static GUIContent[] _v3Labels = new GUIContent[] { new GUIContent("X"), new GUIContent("Y"), new GUIContent("Z") };
		private static float[] _v2Values = new float[2];
		private static float[] _v3Values = new float[3];

		private static void MultiDelayedFloatFieldInternal(Rect position, GUIContent[] subLabels, float[] values)
		{
			const float kSpacingSubLabel = 4f;

			int eCount = values.Length;
			float w = (position.width - (eCount - 1) * kSpacingSubLabel) / eCount;
			Rect nr = new Rect(position) {width = w};

			using var _i  = IndentScope.Zero;
			using var labelWidth = LabelWidthScope.Zero;

			for (int i = 0; i < values.Length; i++)
			{
				using (new LabelWidthScope(subLabels[i]))
					values[i] = EditorGUI.DelayedFloatField(nr, subLabels[i], values[i]);

				nr.x += w + kSpacingSubLabel;
			}
		}
	}
}
#nullable restore