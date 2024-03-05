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

		protected override Task OnEnable()
		{
			var propertyType = Tree.Property.propertyType;
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

			return base.OnEnable();
		}

		public override bool EndsDrawing => true;

		protected override float OnGetHeight()
		{
			return UnityInternals.EditorGUI_GetSinglePropertyHeight(Tree.Property, Tree.PropertyLabel);
		}

		protected override void OnGUI(Rect position)
		{
			UnitsAttribute units = (UnitsAttribute)Attribute!;
	
			int index = InlineGUI.InlinePopup(ref position, Convert.ToInt32(units.value), TypeNames[units.value.GetType()]);
			units.value = Enum.ToObject(units.value.GetType(), index);

			SerializedProperty property = Tree.Property;
			GUIContent label = Tree.PropertyLabel;

			EditorGUI.BeginProperty(position, label, property);

			if (property.propertyType == SerializedPropertyType.Integer)
			{
				int value = property.intValue;
				double converted = UnitsAttribute.Convert(value, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.DelayedDoubleField(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.intValue = (int)Math.Round(UnitsAttribute.Convert(converted, units.value, units.storeAs));
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector2Int)
			{
				Vector2Int value = property.vector2IntValue;
				Span<float> floats = stackalloc float[2] {
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value)
				};

				EditorGUI.BeginChangeCheck();
				position = EditorGUI.PrefixLabel(position, label);
				DelayedAttributeDrawer.MultiDelayedFieldInternal(position, floats, DelayedAttributeDrawer.VECTOR_Labels);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector2IntValue = new Vector2Int(
						(int)Math.Round(UnitsAttribute.Convert(floats[0], units.value, units.storeAs)),
						(int)Math.Round(UnitsAttribute.Convert(floats[1], units.value, units.storeAs))
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector3Int)
			{
				Vector3Int value = property.vector3IntValue;
				Span<float> floats = stackalloc float[3] {
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.z, units.storeAs, units.value)
				};

				EditorGUI.BeginChangeCheck();
				position = EditorGUI.PrefixLabel(position, label);
				DelayedAttributeDrawer.MultiDelayedFieldInternal(position, floats, DelayedAttributeDrawer.VECTOR_Labels);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector3IntValue = new Vector3Int(
						(int)Math.Round(UnitsAttribute.Convert(floats[0], units.value, units.storeAs)),
						(int)Math.Round(UnitsAttribute.Convert(floats[1], units.value, units.storeAs)),
						(int)Math.Round(UnitsAttribute.Convert(floats[2], units.value, units.storeAs))
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.RectInt)
			{
				RectInt value = property.rectIntValue;
				Span<int> ints = stackalloc int[4] {
					(int)Math.Round(UnitsAttribute.Convert(value.x, units.storeAs, units.value)),
					(int)Math.Round(UnitsAttribute.Convert(value.y, units.storeAs, units.value)),
					(int)Math.Round(UnitsAttribute.Convert(value.width, units.storeAs, units.value)),
					(int)Math.Round(UnitsAttribute.Convert(value.height, units.storeAs, units.value))
				};

				EditorGUI.BeginChangeCheck();
				position = EditorGUI.PrefixLabel(position, label);
				DelayedAttributeDrawer.MultiDelayedFieldInternal(position, ints, DelayedAttributeDrawer.RECT_Labels);
				if (EditorGUI.EndChangeCheck())
				{
					property.rectIntValue = new RectInt(
						ints[0], ints[1], ints[2], ints[3]
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Float)
			{
				double value = property.doubleValue;
				double converted = UnitsAttribute.Convert(value, units.storeAs, units.value);

				EditorGUI.BeginChangeCheck();
				if (property.numericType == SerializedPropertyNumericType.Float)
					converted = EditorGUI.FloatField(position, label, (float)converted);
				else
					converted = EditorGUI.DoubleField(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.doubleValue = UnitsAttribute.Convert(converted, units.value, units.storeAs);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector2)
			{
				Vector2 value = property.vector2Value;
				Vector2 converted = new Vector2(
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value)
				);
				
				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector2Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector2Value = new Vector2(
						(float)UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.y, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector3)
			{
				Vector3 value = property.vector3Value;
				Vector3 converted = new Vector3(
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.z, units.storeAs, units.value)
				);
				
				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector3Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector3Value = new Vector3(
						(float)UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.y, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.z, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Vector4)
			{
				Vector4 value = property.vector4Value;
				Vector4 converted = new Vector4(
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.z, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.w, units.storeAs, units.value)
				);
				
				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.Vector4Field(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.vector4Value = new Vector4(
						(float)UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.y, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.z, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.w, units.value, units.storeAs)
					);
				}
			}
			else if (property.propertyType == SerializedPropertyType.Rect)
			{
				Rect value = property.rectValue;
				Rect converted = new Rect(
					(float)UnitsAttribute.Convert(value.x, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.y, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.width, units.storeAs, units.value),
					(float)UnitsAttribute.Convert(value.height, units.storeAs, units.value)
				);

				EditorGUI.BeginChangeCheck();
				converted = EditorGUI.RectField(position, label, converted);
				if (EditorGUI.EndChangeCheck())
				{
					property.rectValue = new Rect(
						(float)UnitsAttribute.Convert(converted.x, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.y, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.width, units.value, units.storeAs),
						(float)UnitsAttribute.Convert(converted.height, units.value, units.storeAs)
					);
				}
			}

			else throw new NotImplementedException("Property type '" + property.propertyType + "' is supported but somehow was not drawn properly.");

			EditorGUI.EndProperty();
		}
	}
}
#nullable restore