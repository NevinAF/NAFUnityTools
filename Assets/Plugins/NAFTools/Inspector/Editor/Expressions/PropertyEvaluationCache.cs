#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using UnityEditor;
	using NAF.Inspector;
	using NAF.ExpressionCompiler;
	using System.Runtime.ExceptionServices;
	using System.Threading.Tasks;

	public static class PropertyEvaluationCache<T>
	{
		private static T?[] s_resultBuffer = new T?[8];

		public static Func<object?, object?, T?>? Load(Type hostType, Type fieldType, object? expressionOrValue)
		{
			if (expressionOrValue is string expression)
			{
				try 
				{
					return PropertyFieldCompiler<T>.Load(hostType, fieldType, expression);
				}
				catch (Exception)
				{
					if (expression[0] == PropertyFieldCompiler.ExpressionSymbol) throw; // Forced as expression
				}
			}

			return null;
		}

		public static Task<Func<object?, object?, T?>?> Load(in SerializedProperty property, object? expressionOrValue)
		{
			var grabber = PropertyTargets.Load(property);
			return grabber.ContinueWith(t =>
			{
				var result = t.Result;

				if (expressionOrValue is string expression)
				{
					try 
					{
						return PropertyFieldCompiler<T>.Load(result.ParentType, result.ValueType, expression);
					}
					catch (Exception)
					{
						if (expression[0] == PropertyFieldCompiler.ExpressionSymbol) throw; // Forced as expression
					}
				}
				return null;
			});
		}

		public static Span<T?> ResolveAll(SerializedProperty property, object? expressionOrValue, bool force = false)
		{
			var targets = PropertyTargets.GetValues(property);
			int count = targets.Length;

			if (count == 0)
				return Span<T?>.Empty;

			if (count > s_resultBuffer.Length)
				Array.Resize(ref s_resultBuffer, count);

			Span<T?> results = s_resultBuffer.AsSpan(0, count);

			if (expressionOrValue == null)
			{
				for (int i = 0; i < count; i++)
					results[i] = default;
				return results;
			}

			Func<object?, object?, T?>? func = Load(targets.ParentType, targets.FieldType, expressionOrValue);
			if (func != null)
			{
				for (int i = 0; i < count; i++)
					results[i] = func(targets.ParentValues[i], targets.FieldValues[i]);
			}
			else if (PropertyFieldCompiler<T>.Caster != null)
			{
				for (int i = 0; i < count; i++)
					results[i] = PropertyFieldCompiler<T>.Caster(expressionOrValue);
			}
			else
			{
				for (int i = 0; i < count; i++)
					results[i] = (T)expressionOrValue;
			}

			return results;
		}

		// public static object GetValue(this SerializedProperty property)
		// {
		// 	return property.propertyType switch
		// 	{
		// 		SerializedPropertyType.Integer => property.intValue,
		// 		SerializedPropertyType.Boolean => property.boolValue,
		// 		SerializedPropertyType.Float => property.floatValue,
		// 		SerializedPropertyType.String => property.stringValue,
		// 		SerializedPropertyType.Color => property.colorValue,
		// 		SerializedPropertyType.ObjectReference => property.objectReferenceValue,
		// 		SerializedPropertyType.LayerMask => property.intValue,
		// 		SerializedPropertyType.Enum => property.enumValueIndex,
		// 		SerializedPropertyType.Vector2 => property.vector2Value,
		// 		SerializedPropertyType.Vector3 => property.vector3Value,
		// 		SerializedPropertyType.Vector4 => property.vector4Value,
		// 		SerializedPropertyType.Rect => property.rectValue,
		// 		SerializedPropertyType.ArraySize => property.arraySize,
		// 		SerializedPropertyType.Character => (char)property.intValue,
		// 		SerializedPropertyType.AnimationCurve => property.animationCurveValue,
		// 		SerializedPropertyType.Bounds => property.boundsValue,
		// 		SerializedPropertyType.Gradient => throw new NotImplementedException("Gradient is not supported by Unity as a serializable type value?"),
		// 		SerializedPropertyType.Quaternion => property.quaternionValue,
		// 		SerializedPropertyType.ExposedReference => property.exposedReferenceValue,
		// 		SerializedPropertyType.FixedBufferSize => property.fixedBufferSize,
		// 		SerializedPropertyType.Vector2Int => property.vector2IntValue,
		// 		SerializedPropertyType.Vector3Int => property.vector3IntValue,
		// 		SerializedPropertyType.RectInt => property.rectIntValue,
		// 		SerializedPropertyType.BoundsInt => property.boundsIntValue,
		// 		SerializedPropertyType.ManagedReference => property.managedReferenceValue,
		// 		SerializedPropertyType.Hash128 => property.hash128Value,
		// 		_ => throw new ArgumentOutOfRangeException(),
		// 	};
		// }

		// private static string PropertyFieldName(string path)
		// {
		// 	int iterator = path.Length - 1;
		// 	if (path[iterator] != ']')
		// 	{
		// 		while (iterator >= 0 && path[iterator] != '.')
		// 			iterator--;
		// 		return path.Substring(iterator + 1, path.Length - iterator - 1);
		// 	}

		// 	do { iterator--; }
		// 	while (path[iterator] != '[');

		// 	string index = path.Substring(iterator + 1, path.Length - iterator - 2);

		// 	int arrayNameEnd = iterator - 12;
		// 	iterator = arrayNameEnd - 1;
		// 	while (iterator >= 0 && path[iterator] != '.')
		// 		iterator--;
		// 	string arrayName = path.Substring(iterator + 1, arrayNameEnd - iterator);

		// 	return arrayName + "[" + index + "]";
		// }
	}
}