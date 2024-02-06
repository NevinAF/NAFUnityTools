#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Threading.Tasks;
	using UnityEditor;

	public static class PropertyTargets
	{
		public delegate GrabberResult ValueGrabber(UnityEngine.Object target, Span<int> arrayIndices);

		public readonly struct GrabberValue
		{
			public readonly Type ValueType;
			public readonly Type ParentType;
			public readonly ValueGrabber Grabber;

			public GrabberValue(Type valueType, Type parentType, ValueGrabber grabber)
			{
				ValueType = valueType;
				ParentType = parentType;
				Grabber = grabber;
			}
		}

		private readonly struct GrabberKey : IEquatable<GrabberKey>
		{
			public readonly Type host;
			public readonly string path;

			public GrabberKey(Type host, string path)
			{
				this.host = host;
				this.path = path;
			}

			public readonly bool Equals(GrabberKey other)
			{
				return object.Equals(host, other.host) && string.Equals(path, other.path);
			}

			public override readonly bool Equals(object? obj)
			{
				if (obj == null)
				{
					return false;
				}

				return obj is GrabberKey cache && Equals(cache);
			}

			public override readonly int GetHashCode()
			{
				return (((host != null) ? host.GetHashCode() : 0) * 397) ^ ((path != null) ? path.GetHashCode() : 0);
			}
		}

		public readonly struct GrabberResult
		{
			public readonly object? Value;
			public readonly object? Parent;

			public GrabberResult(object? value, object? parent)
			{
				Value = value;
				Parent = parent;
			}

			public static ConstructorInfo Constructor { get; } = typeof(GrabberResult).GetConstructor(new[] { typeof(object), typeof(object) })!;
		}

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

		private struct ResultCache
		{
			public SerializedObject serializedObject;
			public string propertyPath;
			public Memory<object?> FieldValues;
			public Type FieldType;
			public Memory<object?> ParentValues;
			public Type ParentType;

			public bool Matches(SerializedProperty property) => property.serializedObject == serializedObject && string.Equals(property.propertyPath, propertyPath);
			public Result ToSpan() => new Result(FieldValues.Span, FieldType, ParentValues.Span, ParentType);
		}

		private static readonly Dictionary<GrabberKey, GrabberValue?> s_MethodInfoFromPropertyPathCache = new();
		private static ResultCache s_lastCache = default;

		private static object?[] s_valueBuffer = new object?[8];
		private static object?[] s_parentBuffer = new object?[8];


		public static Result GetValues(SerializedProperty property)
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
			GrabberKey key = new GrabberKey(targetType, s_lastCache.propertyPath);
			GrabberValue value = GetNestedTargetGrabber(key) ??
				throw new Exception("Could not find field info for property path '" + s_lastCache.propertyPath + "' on target object of type " + targetType.Name + ".");

			int conservativeMax = (s_lastCache.propertyPath.Length - 1) / 14;
			Span<int> arrayInts = stackalloc int[conservativeMax];
			IndicesInPath(arrayInts, s_lastCache.propertyPath);

			for (int i = 0; i < targets.Length; i++)
			{
				GrabberResult r = value.Grabber(targets[i], arrayInts);
				s_valueBuffer[i] = r.Value;
				s_parentBuffer[i] = r.Parent;
			}

			s_lastCache.FieldValues = s_valueBuffer.AsMemory(0, targets.Length);
			s_lastCache.FieldType = value.ValueType;
			s_lastCache.ParentValues = s_parentBuffer.AsMemory(0, targets.Length);
			s_lastCache.ParentType = value.ParentType;
			return s_lastCache.ToSpan();
		}

		public static GrabberValue Load(Type hostType, string propertyPath)
		{
			GrabberKey key = new GrabberKey(hostType, propertyPath);
			return GetNestedTargetGrabber(key) ??
				throw new Exception("Could not find field info for property path '" + propertyPath + "' on target object of type " + hostType.Name + ".");
		}

		public static Task<GrabberValue> Load(in SerializedProperty property)
		{
			Type hostType = property.serializedObject.targetObject.GetType();
			string propertyPath = property.propertyPath;

			return Task.Run(() => Load(hostType, propertyPath));
		}

		private static int SpanIndexer(Span<int> span, int index) => span[index];
		private static MethodInfo? mi_SpanIndexer = null;
		private static MethodInfo MI_SpanIndexer => mi_SpanIndexer ??=
			typeof(PropertyTargets).GetMethod(nameof(SpanIndexer), BindingFlags.Static | BindingFlags.NonPublic)!;

		private static GrabberValue? GetNestedTargetGrabber(in GrabberKey key)
		{
			if (s_MethodInfoFromPropertyPathCache.TryGetValue(key, out GrabberValue? grabber))
				return grabber;

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

					index++; // index is at the ']' character right now. The next character must be a '.'.
					start = index;

					if (index >= length)
						goto exit; // We are done with the path, so we can exit the loops.
				} // Index is now at the '.' character after the array index.

				index++; // Cannot have two '.' in a row, and cannot start with a '.'

				while (index < length && path[index] != '.') // Look for end or next '.'
					index++;

				string fieldName = path.Slice(start, index - start).ToString();
				FieldInfo? field = host!.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic | BindingFlags.Public | BindingFlags.FlattenHierarchy);

				if (field == null)
				{
					host = null;
					s_MethodInfoFromPropertyPathCache.Add(key, null);
					return null;
				}

				parent = current;
				current = Expression.Field(current, field);
				host = field.FieldType;

				index++; // index is at the start of the next part.
				start = index;
			} // End of while loop

		exit:
			Expression result = Expression.New(GrabberResult.Constructor,
				Expression.Convert(current, typeof(object)),
				Expression.Convert(parent!, typeof(object))
			);

			grabber = new GrabberValue(current.Type, parent!.Type,
				Expression.Lambda<ValueGrabber>(result, targetParameter, arrayIndicesParameter).Compile()
			);

			s_MethodInfoFromPropertyPathCache[key] = grabber;
			return grabber;
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
	}
}
#nullable restore