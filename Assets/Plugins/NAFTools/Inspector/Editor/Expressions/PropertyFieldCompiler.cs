#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Runtime.ExceptionServices;
	using System.Threading.Tasks;
	using NAF.ExpressionCompiler;
	using UnityEditor;
	using UnityEngine;

	[InitializeOnLoad]
	public static class PropertyFieldCompiler
	{
		public const char ExpressionSymbol = '=';

		private static readonly Lazy<Type[]?> ReferableTypes = new Lazy<Type[]?>(LoadReferableTypes);
		public static readonly ObjectPool<Compiler> CompilerPool = new ObjectPool<Compiler>(() => new Compiler(ReferableTypes.Value));

		private static Type[]? LoadReferableTypes()
		{
			var unityObjects = TypeCache.GetTypesDerivedFrom(typeof(UnityEngine.Object));

			Type[] types = new Type[unityObjects.Count + 60];
			types[0] = typeof(System.Object);
			types[1] = typeof(System.String);
			types[2] = typeof(System.Enum);
			types[3] = typeof(System.Math);
			types[4] = typeof(System.Convert);
			types[5] = typeof(System.Collections.Generic.List<>);
			types[6] = typeof(System.Collections.Generic.Dictionary<,>);
			types[7] = typeof(System.Collections.Generic.HashSet<>);
			types[8] = typeof(System.Collections.Generic.Queue<>);
			types[9] = typeof(System.Collections.Generic.Stack<>);
			types[10] = typeof(System.Collections.Generic.IEnumerable<>);
			types[11] = typeof(System.Collections.Generic.IList<>);
			types[12] = typeof(System.Linq.Enumerable);
			types[13] = typeof(System.Threading.Tasks.Task);
			types[14] = typeof(System.Threading.Tasks.Task<>);
			types[15] = typeof(System.Exception);
			types[16] = typeof(System.Diagnostics.Debug);
			types[17] = typeof(System.DateTime);
			types[18] = typeof(System.Diagnostics.Stopwatch);
			types[19] = typeof(System.Console);
			types[20] = typeof(System.IO.File);
			types[21] = typeof(System.IO.Directory);
			types[22] = typeof(System.IO.Path);
			types[23] = typeof(UnityEngine.Physics);
			types[24] = typeof(UnityEngine.Physics2D);
			types[25] = typeof(UnityEngine.Input);
			types[26] = typeof(UnityEngine.Time);
			types[27] = typeof(UnityEngine.Random);
			types[28] = typeof(UnityEngine.Application);
			types[29] = typeof(UnityEngine.Resources);
			types[30] = typeof(UnityEditor.EditorUtility);
			types[31] = typeof(UnityEditor.AssetDatabase);
			types[32] = typeof(UnityEditor.EditorGUI);
			types[33] = typeof(UnityEditor.EditorGUILayout);
			types[34] = typeof(UnityEditor.EditorGUIUtility);
			types[35] = typeof(UnityEditor.EditorStyles);
			types[36] = typeof(UnityEngine.GUI);
			types[37] = typeof(UnityEngine.GUILayout);
			types[38] = typeof(UnityEngine.GUILayoutUtility);
			types[39] = typeof(UnityEngine.GUIStyle);
			types[40] = typeof(UnityEngine.GUIContent);
			types[41] = typeof(UnityEngine.Texture);
			types[42] = typeof(UnityEngine.Color);
			types[43] = typeof(UnityEngine.Color32);
			types[44] = typeof(UnityEngine.Vector2);
			types[45] = typeof(UnityEngine.Vector3);
			types[46] = typeof(UnityEngine.Vector4);
			types[47] = typeof(UnityEngine.Quaternion);
			types[48] = typeof(UnityEngine.Rect);
			types[49] = typeof(UnityEngine.RectOffset);
			types[50] = typeof(UnityEngine.Matrix4x4);
			types[51] = typeof(UnityEngine.AnimationCurve);
			types[52] = typeof(UnityEngine.Bounds);
			types[53] = typeof(UnityEngine.BoundsInt);
			types[54] = typeof(UnityEngine.LayerMask);
			types[55] = typeof(UnityEngine.Vector2Int);
			types[56] = typeof(UnityEngine.Vector3Int);
			types[57] = typeof(UnityEngine.RectInt);
			types[58] = typeof(UnityEngine.BoundsInt);
			types[59] = typeof(NAF.Inspector.EditorIcons);
			unityObjects.CopyTo(types, 60);

			return types;
		}

		static PropertyFieldCompiler()
		{
			PropertyFieldCompiler<bool>.Caster = CastBool;
			PropertyFieldCompiler<string>.Caster = CastString;
			PropertyFieldCompiler<Texture>.Caster = CastTexture;
			PropertyFieldCompiler<GUIStyle>.Caster = CastGUIStyle;

			PropertyFieldCompiler<double>.Caster = Convert.ToDouble;
			PropertyFieldCompiler<float>.Caster = Convert.ToSingle;
			PropertyFieldCompiler<int>.Caster = Convert.ToInt32;
			PropertyFieldCompiler<long>.Caster = Convert.ToInt64;
			PropertyFieldCompiler<short>.Caster = Convert.ToInt16;
			PropertyFieldCompiler<byte>.Caster = Convert.ToByte;
			PropertyFieldCompiler<sbyte>.Caster = Convert.ToSByte;
			PropertyFieldCompiler<uint>.Caster = Convert.ToUInt32;
			PropertyFieldCompiler<ulong>.Caster = Convert.ToUInt64;
			PropertyFieldCompiler<ushort>.Caster = Convert.ToUInt16;
			PropertyFieldCompiler<char>.Caster = Convert.ToChar;
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

		private static Texture? CastTexture(object? obj)
		{
			if (obj == null)
				return null;

			if (obj is Texture texture)
				return texture;

			if (obj is string iconName)
			{
				Texture e = UnityInternals.EditorGUIUtility_LoadIcon(iconName);
				if (e != null)
					return e;
				throw new ArgumentException($"Cannot resolve texture from the string '{iconName}'");
			}

			throw new ArgumentException($"Cannot resolve style from type {obj!.GetType()} (value: {obj})");
		}

		public static GUIStyle? CastGUIStyle(object? obj)
		{
			if (obj == null)
				return null;

			if (obj is GUIStyle style)
				return style;

			if (obj is string styleName)
				return new GUIStyle(styleName);

			throw new ArgumentException($"Cannot resolve style from type {obj!.GetType()} (value: {obj})");
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

			public Func<object?, object?, T?> Compiled
			{
				get {
					if (exception != null)
						ExceptionDispatchInfo.Throw(exception);

					return compiled!;
				}
			}

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

			public static implicit operator CompileValue(Func<object?, object?, T?> compiled) => new CompileValue(compiled);
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

		public static Func<object?, object?, T?> GetOrCreate(SerializedProperty property, string expression)
		{
			PropertyTargets.Cache targets = PropertyTargets.GetOrCreate(property);
			return GetOrCreate(targets.ParentType, targets.ValueType, expression);
		}

		public static Task<Func<object?, object?, T?>> GetOrAsyncCreate(in SerializedProperty property, string expression)
		{
			return GetOrAsyncCreate(PropertyTargets.GetOrAsyncCreate(property), expression);
		}

		public static async Task<Func<object?, object?, T?>> GetOrAsyncCreate(Task<PropertyTargets.Cache> targetsCacheTask, string expression)
		{
			PropertyTargets.Cache targets = await targetsCacheTask;
			return await GetOrAsyncCreate(targets.ParentType, targets.ValueType, expression);
		}

		public static Func<object?, object?, T?> GetOrCreate(Type hostType, Type fieldType, string expression)
		{
			if (string.IsNullOrEmpty(expression))
				throw new ArgumentException("Expression cannot be empty.", nameof(expression));
			if (hostType == null)
				throw new ArgumentNullException(nameof(hostType));
			if (fieldType == null)
				throw new ArgumentNullException(nameof(fieldType));

			CompileKey key = new CompileKey(hostType, expression, fieldType);
			if (!compile_cache.TryGetValue(key, out CompileValue value))
				compile_cache[key] = Create(key);

			return value.Compiled;
		}

		public static Task<Func<object?, object?, T?>> GetOrAsyncCreate(Type hostType, Type fieldType, string expression)
		{
			if (string.IsNullOrEmpty(expression))
				throw new ArgumentException("Expression cannot be empty.", nameof(expression));
			if (hostType == null)
				throw new ArgumentNullException(nameof(hostType));
			if (fieldType == null)
				throw new ArgumentNullException(nameof(fieldType));

			CompileKey key = new CompileKey(hostType, expression, fieldType);
			if (compile_cache.TryGetValue(key, out CompileValue value))
				return Task.FromResult(value.Compiled);
			
			return Task.Run(() => (compile_cache[key] = Create(key)).Compiled);
		}

		private static CompileValue Create(in CompileKey key)
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
			catch (Exception e)
			{
				return new CompileValue(e);
			}
			finally
			{
				PropertyFieldCompiler.CompilerPool.Return(compiler);
			}
		}
	}
}
#nullable restore