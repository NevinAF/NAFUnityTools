#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Concurrent;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using UnityEditor;
	using UnityEngine;

	public static class PropertyTargets
	{
		public delegate (object?, object?) Resolver(UnityEngine.Object target, Span<int> arrayIndices);

		public readonly struct Cache
		{
			public readonly Type ValueType;
			public readonly Type ParentType;
			public readonly Resolver Resolver;

			public Cache(Type valueType, Type parentType, Resolver resolver)
			{
				ValueType = valueType;
				ParentType = parentType;
				Resolver = resolver;
			}
		}

		private readonly struct Key : IEquatable<Key>
		{
			public readonly Type host;
			public readonly string path;

			public Key(Type host, string path)
			{
				this.host = host;
				this.path = path;
			}

			public readonly bool Equals(Key other)
			{
				return object.Equals(host, other.host) && string.Equals(path, other.path);
			}

			public override readonly bool Equals(object? obj)
			{
				if (obj == null)
				{
					return false;
				}

				return obj is Key cache && Equals(cache);
			}

			public override readonly int GetHashCode()
			{
				return (((host != null) ? host.GetHashCode() : 0) * 397) ^ ((path != null) ? path.GetHashCode() : 0);
			}
		}

		public static ConstructorInfo ResolverResult { get; } = typeof((object?, object?)).GetConstructor(new[] { typeof(object), typeof(object) })!;

		public readonly ref struct Result
		{
			public readonly Span<object?> FieldValues;
			public readonly Type FieldType;
			public readonly Span<object?> ParentValues;
			public readonly Type ParentType;

			public int Length => FieldValues.Length;

			public Result(Span<object?> fieldValues, Type fieldType, Span<object?> parentValues, Type parentType)
			{
				if (fieldValues.Length != parentValues.Length)
					throw new System.ArgumentException("Field values and parent values must be the same length.");

				FieldValues = fieldValues;
				FieldType = fieldType;
				ParentValues = parentValues;
				ParentType = parentType;
			}
		}

		private struct MemoResult
		{
			public SerializedObject serializedObject;
			public string propertyPath;
			public Memory<object?> FieldValues;
			public Memory<object?> ParentValues;
			public Type FieldType;
			public Type ParentType;

			public bool Matches(SerializedProperty property) => property.serializedObject == serializedObject && string.Equals(property.propertyPath, propertyPath);
			public Result ToSpan() => new Result(FieldValues.Span, FieldType, ParentValues.Span, ParentType);
		}

		private static readonly ConcurrentDictionary<Key, Cache?> s_MethodInfoFromPropertyPathCache = new();
		private static MemoResult s_lastCache = default;

		private static object?[] s_valueBuffer = new object?[8];
		private static object?[] s_parentBuffer = new object?[8];


		public static Result Resolve(SerializedProperty property)
		{
			if (s_lastCache.Matches(property))
				return s_lastCache.ToSpan();

			var targets = (s_lastCache.serializedObject = property.serializedObject).targetObjects;

			if (targets.Length == 0)
			{
				s_lastCache.propertyPath = string.Empty;
				return default;
			}

			if (targets.Length > s_valueBuffer.Length)
			{
				Array.Resize(ref s_valueBuffer, s_valueBuffer.Length * 2);
				Array.Resize(ref s_parentBuffer, s_valueBuffer.Length * 2);
			}

			Type targetType = targets[0].GetType();
			s_lastCache.propertyPath = property.propertyPath;
			Key key = new Key(targetType, s_lastCache.propertyPath);
			Cache value = GetOrCreate(key) ??
				throw CreateFieldException(key);

			int conservativeMax = (s_lastCache.propertyPath.Length - 1) / 14;
			Span<int> arrayInts = stackalloc int[conservativeMax];
			IndicesInPath(arrayInts, s_lastCache.propertyPath);

			for (int i = 0; i < targets.Length; i++)
				(s_valueBuffer[i], s_parentBuffer[i]) = value.Resolver(targets[i], arrayInts);

			s_lastCache.FieldValues = s_valueBuffer.AsMemory(0, targets.Length);
			s_lastCache.FieldType = value.ValueType;
			s_lastCache.ParentValues = s_parentBuffer.AsMemory(0, targets.Length);
			s_lastCache.ParentType = value.ParentType;
			return s_lastCache.ToSpan();
		}

		public static Cache GetOrCreate(SerializedProperty property)
		{
			Type hostType = property.serializedObject.targetObject.GetType();
			string propertyPath = property.propertyPath;

			Key key = new Key(hostType, propertyPath);
			return GetOrCreate(key) ??
				throw CreateFieldException(key);
		}

		public static Task<Cache> GetOrAsyncCreate(in SerializedProperty property)
		{
			Type hostType = property.serializedObject.targetObject.GetType();
			string propertyPath = property.propertyPath;

			Key key = new Key(hostType, propertyPath);
			if (s_MethodInfoFromPropertyPathCache.TryGetValue(key, out Cache? grabber))
			{
				if (grabber != null)
					return Task.FromResult(grabber.Value);
				else return Task.FromException<Cache>(CreateFieldException(key));
			}

			return Task.Run(
				() => (s_MethodInfoFromPropertyPathCache[key] = Create(key)) ??
					throw CreateFieldException(key)
			);
		}

		private static Cache? GetOrCreate(in Key key)
		{
			if (s_MethodInfoFromPropertyPathCache.TryGetValue(key, out Cache? grabber))
				return grabber;

			return s_MethodInfoFromPropertyPathCache[key] = Create(key);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private static Exception CreateFieldException(Key key)
		{
			return new Exception($"Could not find field info for property path '{key.path}' on target object of type {key.host.Name}.");
		}

		private static int SpanIndexer(Span<int> span, int index) => span[index];
		private static MethodInfo? mi_SpanIndexer = null;
		private static MethodInfo MI_SpanIndexer => mi_SpanIndexer ??=
			typeof(PropertyTargets).GetMethod(nameof(SpanIndexer), BindingFlags.Static | BindingFlags.NonPublic)!;


		private static Cache? Create(in Key key)
		{
			if (string.IsNullOrEmpty(key.path) || key.path[0] == '.')
				throw new ArgumentException("Path must not be empty nor start with a '.'", nameof(key.path));

			Type? host = key.host;

			ParameterExpression targetParameter = Expression.Parameter(typeof(UnityEngine.Object), "target");
			Expression target = Expression.Convert(targetParameter, host!);

			ParameterExpression arrayIndicesParameter = Expression.Parameter(typeof(Span<int>), "arrayIndices");
			int arrayIndicesIndex = 0;

			Expression current = target;
			Expression? parent = null;

			ReadOnlySpan<char> path = key.path.AsSpan();
			int start = 0;
			int index = 1; // cannot start with a '.' so we can skip the first character.
			int length = path.Length;

			while (index < length)
			{
				// Check to see if the following text is ".Array.data["
				// 13 = "Array.data[x]".length
				while (index + 13 <= length &&
					path[index +  0] == 'A' &&
					path[index +  1] == 'r' &&
					path[index +  2] == 'r' &&
					path[index +  3] == 'a' &&
					path[index +  4] == 'y' &&
					path[index +  5] == '.' &&
					path[index +  6] == 'd' &&
					path[index +  7] == 'a' &&
					path[index +  8] == 't' &&
					path[index +  9] == 'a' &&
					path[index + 10] == '[')
				{
					index += 11; // index is now at the start of the number.
					start = index;
					index++; // Must have at least one number.
					while (index < length && path[index] != ']')
						index++;

					// We compile so the path is independent of the array indices. Instead, pull from the parameter based on the number of arrays entered.
					Expression indexExpression = Expression.Call(MI_SpanIndexer, arrayIndicesParameter, Expression.Constant(arrayIndicesIndex++));

					if (host!.IsArray)
					{
						// parent = current;
						current = Expression.ArrayAccess(current, indexExpression);
						host = host.GetElementType();
					}
					else if (host.IsGenericType && host.GetGenericTypeDefinition() == typeof(List<>))
					{
						// parent = current;
						current = Expression.Property(current, "Item", indexExpression);
						host = host.GetGenericArguments()[0];
					}

					index += 2; // index is at the ']' character right now and the next character must be a '.'.
					start = index;

					if (index >= length)
						goto exit; // We are done with the path, so we can exit the loops.
				} // Index is now at the '.' character after the array index.

				index++; // Cannot have two '.' in a row, and cannot start with a '.'

				while (index < length && path[index] != '.') // Look for end or next '.'
					index++;

				string fieldName = path.Slice(start, index - start).ToString();
				FieldInfo? field;
				Type? typeIterator = host;

				while(true)
				{
					field = typeIterator!.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public);

					if (field != null)
						break;
	
					typeIterator = typeIterator.BaseType;
					if (typeIterator == null || typeIterator == typeof(UnityEngine.Object))
					{
						UnityEngine.Debug.LogWarning("Could not find field '" + fieldName + "' on type " + host.Name + ".");
						s_MethodInfoFromPropertyPathCache[key] = null;
						return null;
					}
				}

				parent = current;
				current = Expression.Field(current, field);
				host = field.FieldType;

				index++; // index is at the start of the next part.
				start = index;
			} // End of while loop

		exit:
			Expression result = Expression.New(ResolverResult,
				Expression.Convert(current, typeof(object)),
				Expression.Convert(parent!, typeof(object))
			);

			return new Cache(current.Type, parent!.Type,
				Expression.Lambda<Resolver>(result, targetParameter, arrayIndicesParameter).Compile()
			);
		}

		internal static bool IsArrayOrList(this Type listType)
		{
			if (listType.IsArray)
			{
				return true;
			}
			else if (listType.IsGenericType && listType.GetGenericTypeDefinition() == typeof(List<>))
			{
				return true;
			}
			return false;
		}

		internal static Type? GetArrayOrListElementType(this Type listType)
		{
			if (listType.IsArray)
			{
				return listType.GetElementType()!;
			}
			else if (listType.IsGenericType)
			{
				return listType.GetGenericArguments()[0];
			}
			return null;
		}

		public static bool IsElementOfArray(this SerializedProperty property)
		{
			return property.propertyPath.EndsWith("]");
		}

		private static void IndicesInPath(Span<int> result, ReadOnlySpan<char> input)
		{
			// Must at mimimum start with "a.Array.data"
			int index = 12;
			int length = input.Length;
			int resultIndex = 0;
			while (index < (length - 2)) // Must end with [0], so we can skip the last two characters.
			{
				if (input[index] == '[')
				{
					index++;
					int startIndex = index;
					index++; // Must have at least one number.
					while (index < (length - 1) && input[index] != ']') // Must end with ], so we can skip the last character.
						index++;
					
					ReadOnlySpan<char> span = input.Slice(startIndex, index - startIndex);
					result[resultIndex++] = int.Parse(span);
				}
				index++;
			}
		}

		// private static unsafe int LastIndexInPath(string input)
		// {
		// 	fixed (char* ptr = input)
		// 	{
		// 		int index = input.Length - 1;
		// 		while (index >= 14) // Must start with "a.Array.data[0]", where ']' is index 14
		// 		{
		// 			if (ptr[index] == ']')
		// 			{
		// 				index--;
		// 				int endIndex = index;
		// 				// Must end with "a.Array.data[", so we can skip the last 12 characters.
		// 				while (index >= 13 && input[index] != '[') 
		// 					index--;

		// 				ReadOnlySpan<char> span = new ReadOnlySpan<char>(ptr + index + 1, endIndex - index);
		// 				return int.Parse(span);
		// 			}
		// 			index--;
		// 		}
		// 	}

		// 	return -1;
		// }

		public static void ModifyNumberProperty(this SerializedProperty property, Func<long, long> integer, Func<double, double> floating)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer: {
					long old = property.longValue;
					long mod = integer(old);
					if (mod != old)
						property.longValue = mod;
					return;
				}
				case SerializedPropertyType.Vector2Int: {
					Vector2Int old = property.vector2IntValue;
					Vector2Int mod = new Vector2Int((int)integer(old.x), (int)integer(old.y));
					if (mod != old)
						property.vector2IntValue = mod;
					return;
				}
				case SerializedPropertyType.Vector3Int: {
					Vector3Int old = property.vector3IntValue;
					Vector3Int mod = new Vector3Int((int)integer(old.x), (int)integer(old.y), (int)integer(old.z));
					if (mod != old)
						property.vector3IntValue = mod;
					return;
				}
				case SerializedPropertyType.RectInt: {
					RectInt old = property.rectIntValue;
					RectInt mod = new RectInt((int)integer(old.x), (int)integer(old.y), (int)integer(old.width), (int)integer(old.height));
					if (mod.x != old.x || mod.y != old.y || mod.width != old.width || mod.height != old.height)
						property.rectIntValue = mod;
					return;
				}
				case SerializedPropertyType.BoundsInt: {
					BoundsInt old = property.boundsIntValue;
					BoundsInt mod = new BoundsInt(new Vector3Int((int)integer(old.position.x), (int)integer(old.position.y), (int)integer(old.position.z)), new Vector3Int((int)integer(old.size.x), (int)integer(old.size.y), (int)integer(old.size.z)));
					if (mod.position.x != old.position.x || mod.position.y != old.position.y || mod.position.z != old.position.z || mod.size.x != old.size.x || mod.size.y != old.size.y || mod.size.z != old.size.z)
						property.boundsIntValue = mod;
					return;
				}
				case SerializedPropertyType.Float: {
					double old = property.doubleValue;
					double mod = floating(old);
					if (mod != old)
						property.doubleValue = mod;
					return;
				}
				case SerializedPropertyType.Vector2: {
					Vector2 old = property.vector2Value;
					Vector2 mod = new Vector2((float)floating(old.x), (float)floating(old.y));
					if (mod != old)
						property.vector2Value = mod;
					return;
				}
				case SerializedPropertyType.Vector3: {
					Vector3 old = property.vector3Value;
					Vector3 mod = new Vector3((float)floating(old.x), (float)floating(old.y), (float)floating(old.z));
					if (mod != old)
						property.vector3Value = mod;
					return;
				}
				case SerializedPropertyType.Vector4: {
					Vector4 old = property.vector4Value;
					Vector4 mod = new Vector4((float)floating(old.x), (float)floating(old.y), (float)floating(old.z), (float)floating(old.w));
					if (mod != old)
						property.vector4Value = mod;
					return;
				}
				case SerializedPropertyType.Rect: {
					Rect old = property.rectValue;
					Rect mod = new Rect((float)floating(old.x), (float)floating(old.y), (float)floating(old.width), (float)floating(old.height));
					if (mod.x != old.x || mod.y != old.y || mod.width != old.width || mod.height != old.height)
						property.rectValue = mod;
					return;
				}
				case SerializedPropertyType.Bounds: {
					Bounds old = property.boundsValue;
					Bounds mod = new Bounds(new Vector3((float)floating(old.center.x), (float)floating(old.center.y), (float)floating(old.center.z)), new Vector3((float)floating(old.size.x), (float)floating(old.size.y), (float)floating(old.size.z)));
					if (mod.center.x != old.center.x || mod.center.y != old.center.y || mod.center.z != old.center.z || mod.size.x != old.size.x || mod.size.y != old.size.y || mod.size.z != old.size.z)
						property.boundsValue = mod;
					return;
				}
				default:
					throw new NotImplementedException("Use ModifyNumberProperty only with numeric types!");
			}
		}

		public static void SetPropertyValue(SerializedProperty property, object value)
		{
			switch (property.propertyType)
			{
				case SerializedPropertyType.Integer:
					property.intValue = (int)value;
					break;
				case SerializedPropertyType.Boolean:
					property.boolValue = (bool)value;
					break;
				case SerializedPropertyType.Float:
					property.floatValue = (float)value;
					break;
				case SerializedPropertyType.String:
					property.stringValue = (string)value;
					break;
				case SerializedPropertyType.Color:
					property.colorValue = (UnityEngine.Color)value;
					break;
				case SerializedPropertyType.ObjectReference:
					property.objectReferenceValue = (UnityEngine.Object)value;
					break;
				case SerializedPropertyType.LayerMask:
					property.intValue = (int)value;
					break;
				case SerializedPropertyType.Enum:
					property.enumValueIndex = (int)value;
					break;
				case SerializedPropertyType.Vector2:
					property.vector2Value = (UnityEngine.Vector2)value;
					break;
				case SerializedPropertyType.Vector3:
					property.vector3Value = (UnityEngine.Vector3)value;
					break;
				case SerializedPropertyType.Vector4:
					property.vector4Value = (UnityEngine.Vector4)value;
					break;
				case SerializedPropertyType.Rect:
					property.rectValue = (UnityEngine.Rect)value;
					break;
				case SerializedPropertyType.ArraySize:
					property.arraySize = (int)value;
					break;
				case SerializedPropertyType.Character:
					property.intValue = (char)value;
					break;
				case SerializedPropertyType.AnimationCurve:
					property.animationCurveValue = (UnityEngine.AnimationCurve)value;
					break;
				case SerializedPropertyType.Bounds:
					property.boundsValue = (UnityEngine.Bounds)value;
					break;
				case SerializedPropertyType.Quaternion:
					property.quaternionValue = (UnityEngine.Quaternion)value;
					break;
				case SerializedPropertyType.ExposedReference:
					property.exposedReferenceValue = (UnityEngine.Object)value;
					break;
				case SerializedPropertyType.FixedBufferSize:
					throw new Exception("FixedBufferSize is not supported.");
				case SerializedPropertyType.Vector2Int:
					property.vector2IntValue = (UnityEngine.Vector2Int)value;
					break;
				case SerializedPropertyType.Vector3Int:
					property.vector3IntValue = (UnityEngine.Vector3Int)value;
					break;
				case SerializedPropertyType.RectInt:
					property.rectIntValue = (UnityEngine.RectInt)value;
					break;
				case SerializedPropertyType.BoundsInt:
					property.boundsIntValue = (UnityEngine.BoundsInt)value;
					break;
				default:
					throw new ArgumentOutOfRangeException();
			}
		}
	}
}
#nullable restore