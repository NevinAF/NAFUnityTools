#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Reflection;
	using System.Text;

	public static class ReflectionUtility
	{
		private static Assembly[]? _allAssemblies = null;
		/// <summary>
		/// An array of all assemblies that are referenced from any other assembly based on the entry assembly.
		/// </summary>
		public static Assembly[] AllAssemblies
		{
			get {
				if (_allAssemblies != null)
					return _allAssemblies;

		#if UNITY_EDITOR
				return _allAssemblies = System.AppDomain.CurrentDomain.GetAssemblies();
		#else

				var assemblies = new HashSet<Assembly>();
				var queue = new Queue<Assembly>();

				queue.Enqueue(Assembly.GetEntryAssembly()!);
				assemblies.Add(queue.Peek());

				while(queue.Count > 0)
				{
					Assembly next = queue.Dequeue();

					foreach(AssemblyName reference in next.GetReferencedAssemblies())
					{
						var assembly = Assembly.Load(reference);
						if (assemblies.Add(assembly))
							queue.Enqueue(assembly);
					}
				}

				return _allAssemblies = assemblies.ToArray();
		#endif
			}
		}

		public static Type[]? _allTypes = null;
		/// <summary>
		/// An array of all declared types in all assemblies.
		/// </summary>
		public static Type[] AllDeclaringTypes
		{
			get {
				if (_allTypes != null)
					return _allTypes;

				var types = new HashSet<Type>();
				for (int a = 0; a < AllAssemblies.Length; a++)
				{
					Assembly assembly = AllAssemblies[a];
					Type[] array = assembly.GetTypes();
					for (int t = 0; t < array.Length; t++)
					{
						Type type = array[t];

						if (type.DeclaringType == null)
							types.Add(type);
					}
				}

				return _allTypes = types.ToArray();
			}
		}

		/// <summary>
		/// Returns a string that represents the declaration of the specified member.
		/// </summary>
		/// <param name="member"></param>
		/// <returns></returns>
		/// <exception cref="ArgumentException"></exception>
		/// <exception cref="ArgumentOutOfRangeException"></exception>
		public static string DeclarationString(MemberInfo member)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			bool isPublic;
			bool isStatic;
			string declaration;

			string ParameterString(ParameterInfo[] parameters)
			{
				return $"{string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}";
			}

			string Accessors(bool basePublic, params MethodInfo?[] infos)
			{
				StringBuilder builder = new StringBuilder();

				foreach (var info in infos)
				{
					if (info == null)
						continue;

					if (builder.Length == 0)
						builder.Append(" {");

					if (info.IsPublic && !basePublic)
						builder.Append("public ");
					else if (!info.IsPublic && basePublic)
						builder.Append("private ");

					builder.Append(info.Name.AsSpan(0, info.Name.IndexOf("_")));
					builder.Append(';');
				}

				if (builder.Length > 0)
					builder.Append(" }");

				return builder.ToString();
			}

			switch (member.MemberType)
			{
				case MemberTypes.Constructor:
					ConstructorInfo constructor = (ConstructorInfo)member;
					isPublic = constructor.IsPublic;
					isStatic = constructor.IsStatic;
					declaration = $"{constructor.DeclaringType}({ParameterString(constructor.GetParameters())})";
					break;

				case MemberTypes.Event:
					EventInfo @event = (EventInfo)member;
					isPublic = @event.RaiseMethod?.IsPublic == true || @event.AddMethod?.IsPublic == true || @event.RemoveMethod?.IsPublic == true;
					isStatic = @event.RaiseMethod?.IsStatic == true || @event.AddMethod?.IsStatic == true || @event.RemoveMethod?.IsStatic == true;
					declaration = $"event {@event.EventHandlerType?.Name} {@event.Name}{Accessors(isPublic, @event.RaiseMethod, @event.AddMethod, @event.RemoveMethod)}";
					break;

				case MemberTypes.Field:
					FieldInfo field = (FieldInfo)member;
					isPublic = field.IsPublic;
					isStatic = field.IsStatic;
					if (field.IsLiteral)
						declaration = $"const {field.FieldType.Name} {field.Name}";
					else declaration = $"{field.FieldType.Name} {field.Name}";
					break;

				case MemberTypes.Method:
					MethodInfo method = (MethodInfo)member;
					isPublic = method.IsPublic;
					isStatic = method.IsStatic;
					declaration = $"{method.ReturnType.Name} {method.Name}({ParameterString(method.GetParameters())})";
					break;

				case MemberTypes.Property:
					PropertyInfo property = (PropertyInfo)member;
					isPublic = property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true;
					isStatic = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true;
					declaration = $"{property.PropertyType.Name} {property.Name}{Accessors(isPublic, property.GetMethod, property.SetMethod)}";
					break;

				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					Type type = (Type)member;
					isPublic = type.IsPublic;
					isStatic = type.IsAbstract && type.IsSealed;

					StringBuilder builder = new StringBuilder();
					builder.Append(type.IsAbstract && !isStatic ? "abstract " : "");
					builder.Append(type.IsSealed && !isStatic ? "sealed " : "");
					builder.Append(type.IsValueType ? "struct " : "class ");
					builder.Append(type.Name);
					if (type.IsGenericType || type.IsGenericTypeDefinition)
						builder.Append($"<{string.Join(", ", type.GetGenericArguments().Select(t => t.Name))}>");

					IEnumerable<Type> extends = type.GetInterfaces();
					if (type.BaseType != null && type.BaseType != typeof(object))
						extends = extends.Prepend(type.BaseType);

					if (extends.Any())
						builder.Append($" : {string.Join(", ", extends.Select(t => t.Name))}");

					declaration = builder.ToString();
					break;

				case MemberTypes.Custom:
				case MemberTypes.All:
					throw new ArgumentException("Member type is not supported: " + member.MemberType, nameof(member));

				default:
					throw new ArgumentOutOfRangeException(nameof(member));
			}

			StringBuilder result = new StringBuilder();
			result.Append(isPublic ? "public " : "private ");
			result.Append(isStatic ? "static " : "");
			result.Append(declaration);

			return result.ToString();
		}

		/// <summary>
		/// Returns a Type matching the specified primitive type name if it exists.
		/// </summary>
		/// <param name="span"></param>
		/// <returns></returns>
		public static Type? MatchesPrimitive(ReadOnlySpan<char> span)
		{
			if (span.Length > 7 || span.Length < 3)
				return null;

			switch (span[0])
			{
				case 'i':
					if (span.Equals("int", StringComparison.Ordinal))
						return typeof(int);
					break;

				case 'u':
					if (span.Equals("uint", StringComparison.Ordinal))
						return typeof(uint);
					else if (span.Equals("ulong", StringComparison.Ordinal))
						return typeof(ulong);
					else if (span.Equals("ushort", StringComparison.Ordinal))
						return typeof(ushort);
					break;

				case 'l':
					if (span.Equals("long", StringComparison.Ordinal))
						return typeof(long);
					break;

				case 's':
					if (span.Equals("short", StringComparison.Ordinal))
						return typeof(short);
					else if (span.Equals("string", StringComparison.Ordinal))
						return typeof(string);
					else if (span.Equals("sbyte", StringComparison.Ordinal))
						return typeof(sbyte);
					break;

				case 'b':
					if (span.Equals("byte", StringComparison.Ordinal))
						return typeof(byte);
					else if (span.Equals("bool", StringComparison.Ordinal))
						return typeof(bool);
					break;

				case 'c':
					if (span.Equals("char", StringComparison.Ordinal))
						return typeof(char);
					break;

				case 'f':
					if (span.Equals("float", StringComparison.Ordinal))
						return typeof(float);
					break;

				case 'd':
					if (span.Equals("double", StringComparison.Ordinal))
						return typeof(double);
					else if (span.Equals("decimal", StringComparison.Ordinal))
						return typeof(decimal);
					break;

				case 'v':
					if (span.Equals("void", StringComparison.Ordinal))
						return typeof(void);
					break;

				case 'o':
					if (span.Equals("object", StringComparison.Ordinal))
						return typeof(object);
					break;
			}

			return null;
		}

		/// <summary>
		/// Returns the precedence of the specified numeric type, useful for determining the casting order of numarical operations.
		/// </summary>
		/// <param name="type"></param>
		/// <returns></returns>
		public static int NumaricTypePrecedence(this Type type)
		{
			int typeCode = (int)Type.GetTypeCode(type);
			if (typeCode > (int)TypeCode.Decimal || typeCode < (int)TypeCode.Char)
				return -1;

			return typeCode;
		}
	}
}
#nullable restore