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
	using UnityEngine;

	public struct AttributeExprCache<T>
	{
		private readonly AttributeExpr<T> _expr;
		private T? _value;
		private bool _multipleValues;

		public readonly T? Value => _value;
		public readonly bool MultipleValues => _multipleValues;

		public void Refresh(in SerializedProperty property, T? nullValue = default)
		{
			if (_expr.IsNull)
				_value = nullValue;
			else _value = _expr.Compute(property, out _multipleValues) ?? nullValue;
		}

		public AttributeExprCache(AttributeExpr<T> expr)
		{
			_expr = expr;
			_value = default;
			_multipleValues = false;
		}

		public static implicit operator T?(AttributeExprCache<T> cache) => cache.Value;
		public static implicit operator AttributeExprCache<T>(AttributeExpr<T> expr) => new AttributeExprCache<T>(expr);
	}

	public readonly struct AttributeExpr<T>
	{
		private readonly object? _captured;
		private AttributeExpr(object? captured)
		{
		#if DEBUG
			if (typeof(MulticastDelegate).IsAssignableFrom(typeof(T)))
				throw new NotSupportedException("The type '" + typeof(T).Name + "' cannot be used as a captured type for an attribute expression.");
			if (typeof(Exception).IsAssignableFrom(typeof(T)))
				throw new NotSupportedException("The type '" + typeof(T).Name + "' cannot be used as a captured type for an attribute expression.");
		#endif

			_captured = captured;
		}

		public readonly bool IsNull => _captured is null && typeof(T).IsClass;
		public readonly bool IsConstant => _captured is not MulticastDelegate;
		public readonly bool IsFaulted => _captured is Exception;

		public readonly Span<T?> Compute(in SerializedProperty property)
		{
			if (_captured is Func<object?, object?, T?> func)
				return Resolve(property, func);

			Span<T?> results = s_resultBuffer.AsSpan(0, 1);
			results[0] = AsConstant();
			return results;
		}

		public readonly T? Compute(in SerializedProperty property, out bool multipleValues)
		{
			Span<T?> results = Compute(property);
			if (results.Length == 0)
			{
				multipleValues = false;
				return default;
			}

			multipleValues = !TempUtility.AllEqual(results);
			return results[0];
		}

		public readonly T? Compute(object? parent, object? field)
		{
			if (_captured is Func<object?, object?, T?> func)
				return func(parent, field);

			return AsConstant();
		}

		public readonly T? AsConstant()
		{
			if (_captured is null && !typeof(T).IsValueType) return default;
			if (_captured is Exception exception) ExceptionDispatchInfo.Capture(exception).Throw();
			else if (_captured is T value) return value;

			throw new Exception("The expression or value provided to the property field compiler is not valid for the type '" + typeof(T).Name + "'.");
		}

		public static AttributeExpr<T> Create(in SerializedProperty property, object? expressionOrValue)
		{
			if (expressionOrValue is string expression && expression.Length > 0)
			{
				try
				{
					Func<object?, object?, T?>? func = PropertyFieldCompiler<T>.GetOrCreate(property, expression);
					if (func != null)
						return new AttributeExpr<T>(func);
				}
				catch (Exception exception)
				{
					// This must be an expression..
					if (expression[0] == PropertyFieldCompiler.ExpressionSymbol)
						return new AttributeExpr<T>(exception);
				}
			}

			return Constant(expressionOrValue);
		}

		public static Task<AttributeExpr<T>> AsyncCreate(in SerializedProperty property, object? expressionOrValue)
		{
			if (expressionOrValue is string expression && expression.Length > 0)
				return AsyncCreate(PropertyFieldCompiler<T>.GetOrAsyncCreate(property, expression), expressionOrValue);

			return Task.FromResult(Constant(expressionOrValue));
		}

		private static async Task<AttributeExpr<T>> AsyncCreate(Task<Func<object?, object?, T?>> funcTask, object expressionOrValue)
		{
			try {
				var func = await funcTask;
				if (func != null)
					return new AttributeExpr<T>(func);
			}
			catch (Exception e)
			{
				if ((expressionOrValue as string)![0] == PropertyFieldCompiler.ExpressionSymbol)
					return Constant(e);
			}

			return Constant(expressionOrValue);
		}

		public static AttributeExpr<T> Constant(object? value)
		{
			if (value is null || value is Func<object?, object?, T?> || value is T)
				return new AttributeExpr<T>(value);

			if (PropertyFieldCompiler<T>.Caster != null)
			{
				try {
					return new AttributeExpr<T>(PropertyFieldCompiler<T>.Caster(value));
				}
				catch (Exception e)
				{
					return new AttributeExpr<T>(e);
				}
			}

			return new AttributeExpr<T>(new Exception("The value provided '" + value + "' of type '" + value.GetType().Name + "' cannot be cast to the AttributeExpressions type '" + typeof(T).Name + "'."));
		}

		public static Span<T?> Invoke(SerializedProperty property, string expression)
		{
			try {
				return AttributeExpr<T>.Create(property, expression).Compute(property);
			}
			catch (Exception e)
			{
				Debug.LogError("There was an error executing method expression: '" + expression + "'. See following log for details.");
				Debug.LogException(e);
				return Span<T?>.Empty;
			}
		}

		internal static T?[] s_resultBuffer = new T?[8];
		public static Span<T?> Resolve(SerializedProperty property, Func<object?, object?, T?> func)
		{
			PropertyTargets.Result targets = PropertyTargets.Resolve(property);
			int count = targets.Length;

			if (count == 0)
				return Span<T?>.Empty;

			if (count > s_resultBuffer.Length)
				Array.Resize(ref s_resultBuffer, count);

			Span<T?> results = s_resultBuffer.AsSpan(0, count);
			for (int i = 0; i < count; i++)
				results[i] = func(targets.ParentValues[i], targets.FieldValues[i]);

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