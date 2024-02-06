#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Runtime.ExceptionServices;
	using System.Threading.Tasks;
	using NAF.ExpressionCompiler;
	using UnityEditor;

	public static class PropertyFieldCompiler
	{
		public const char ExpressionSymbol = '=';

		public static readonly ObjectPool<Compiler> CompilerPool = new ObjectPool<Compiler>(() => new Compiler(ReferableTypes));
		public static Type[]? ReferableTypes = null;

		static PropertyFieldCompiler()
		{
			PropertyFieldCompiler<bool>.Caster = CastBool;
			PropertyFieldCompiler<string>.Caster = CastString;
		}

		private static bool CastBool(object? obj)
		{
			if (obj == null)
				return false;

			Type type = obj.GetType();

			if (type.IsValueType)
				return !obj.Equals(Activator.CreateInstance(type));

			if (obj is UnityEngine.Object uObj)
				return uObj != null;

			return obj != null;
		}

		private static string? CastString(object? obj)
		{
			return obj?.ToString();
		}
	}

	/// <summary>
	/// A cache for compiled delegates created from the <see cref="ExpressionCompiler"/> class. In addition, this has type converters that make using the compiled delegates easier.
	/// </summary>
	public static class PropertyFieldCompiler<T>
	{
		private readonly struct CompileKey
		{
			public readonly Type hostType;
			public readonly Type fieldType;
			public readonly string expression;

			public CompileKey(Type hostType, string expression, Type fieldType)
			{
				this.hostType = hostType;
				this.expression = expression;
				this.fieldType = fieldType;
			}

			public bool Equals(CompileKey other)
			{
				return fieldType == other.fieldType && expression == other.expression && object.Equals(hostType, other.hostType);
			}

			public override bool Equals(object? obj)
			{
				if (obj == null)
				{
					return false;
				}

				return obj is CompileKey cache && Equals(cache);
			}

			public override int GetHashCode()
			{
				return ((((hostType != null) ? hostType.GetHashCode() : 0) * 397) ^ ((expression != null) ? expression.GetHashCode() : 0)) * 31 ^ ((fieldType != null) ? fieldType.GetHashCode() : 0);
			}

			public override string ToString()
			{
				return "(" + hostType + ", " + fieldType + ") => " + expression;
			}
		}

		private readonly struct CompileValue
		{
			public readonly Func<object?, object?, T?>? compiled;
			public readonly Exception? exception;

			public CompileValue(Func<object?, object?, T?> compiled)
			{
				this.compiled = compiled;
				this.exception = null;
			}

			public CompileValue(Exception exception)
			{
				this.compiled = null;
				this.exception = exception;
			}

			public override string ToString()
			{
				return exception == null ? "Compiled" : "Exception";
			}
		}

		private static readonly ConcurrentDictionary<CompileKey, CompileValue> compile_cache = new ConcurrentDictionary<CompileKey, CompileValue>();
		public static void ClearCache() => compile_cache.Clear();

		private static Func<object?, T?>? _caster;
		public static Func<object?, T?>? Caster
		{
			get => _caster;
			set
			{
				_caster = value;
				ClearCache();
			}
		}


		public static Func<object?, object?, T?> Get(SerializedProperty property, string expression)
		{
			var targets = PropertyTargets.GetValues(property);
			return Get(targets.ParentType, targets.FieldType, expression);
		}

		public static Func<object?, object?, T?> Load(Type hostType, Type fieldType, string expression)
		{
			return Get(hostType, fieldType, expression);
		}

		public static Task<Func<object?, object?, T?>> Load(in SerializedProperty property, string expression)
		{
			var grabber = PropertyTargets.Load(property);
			return grabber.ContinueWith(t =>
			{
				var result = t.Result;
				return Load(result.ParentType, result.ValueType, expression);
			});
		}

		public static Func<object?, object?, T?> Get(Type hostType, Type fieldType, string expression)
		{
			if (string.IsNullOrEmpty(expression))
				throw new ArgumentException("Expression cannot be empty.", nameof(expression));
			if (hostType == null)
				throw new ArgumentNullException(nameof(hostType));
			if (fieldType == null)
				throw new ArgumentNullException(nameof(fieldType));

			CompileKey key = new CompileKey(hostType, expression, fieldType);
			if (!compile_cache.TryGetValue(key, out CompileValue value))
			{
				try {
					UnityEngine.Debug.Log("Compiler Cache miss: " + key + " | \n\t" + string.Join("\n\t", compile_cache.Select(s => s.Key.ToString() + " => " + s.Value.ToString())) + "\n\t");
					value = new CompileValue(Fetch(key));
				}
				catch (Exception e)
				{
					value = new CompileValue(e);
				}

				compile_cache[key] = value;
			}

			if (value.exception != null)
				ExceptionDispatchInfo.Throw(value.exception);

			return value.compiled!;
		}

		private static Func<object?, object?, T?> Fetch(in CompileKey key)
		{
			ParameterExpression target = Expression.Parameter(typeof(object));
			ParameterExpression field = Expression.Parameter(typeof(object));

			ReadOnlySpan<char> span = key.expression.AsSpan();
			if (span.Length > 1 && span[0] == PropertyFieldCompiler.ExpressionSymbol)
				span = span.Slice(1);

			Compiler compiler = PropertyFieldCompiler.CompilerPool.Get();

			try {
				Expression body = compiler.Parse(span,
					Expression.Convert(target, key.hostType),
					Expression.Convert(field, key.fieldType));

				if (body.Type == typeof(T))
					return Expression.Lambda<Func<object?, object?, T>>(body, target, field).Compile();

				// If the return type is object, also allow void, but return null
				else if (body.Type == typeof(void) && typeof(T) == typeof(object))
					return Expression.Lambda<Func<object?, object?, T>>(Expression.Block(body, Expression.Constant(null, typeof(T))), target, field).Compile();

				else try {
					Expression converted = Expression.Convert(body, typeof(T));
					return Expression.Lambda<Func<object?, object?, T>>(converted, target, field).Compile();
				}
				catch (InvalidOperationException)
				{
					if (Caster != null)
					{
						return Expression.Lambda<Func<object?, object?, T>>(Expression.Invoke(Expression.Constant(Caster), Expression.Convert(body, typeof(object))), target, field).Compile();
					}
					else throw;
				}
			}
			finally
			{
				PropertyFieldCompiler.CompilerPool.Return(compiler);
			}
		}
	}
}
#nullable restore