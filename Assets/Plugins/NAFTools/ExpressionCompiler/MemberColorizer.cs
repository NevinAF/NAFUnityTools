using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.CompilerServices;
using System.Text;

namespace NAF.ExpressionCompiler
{
	public class TerminalMemberColorizer : MemberColorizer
	{
		public TerminalMemberColorizer(params Type[] blacklistAttributes) : base(blacklistAttributes) { }

		public override string Colorize(string text, TokenType type)
		{
			string color = type switch
			{
				TokenType.Keyword => "\x1b[35m",
				TokenType.Class => "\x1b[32m",
				TokenType.Struct => "\x1b[32m",
				TokenType.KeyType => "\x1b[92m",
				TokenType.Field => "\x1b[94m",
				TokenType.Property => "\x1b[34m",
				TokenType.Method => "\x1b[33m",
				TokenType.Parameter => "\x1b[37m",
				TokenType.String => "\x1b[31m",
				TokenType.Numeric => "\x1b[93m",
				_ => throw new ArgumentOutOfRangeException(nameof(type)),
			};

			return $"{color}{text}\x1b[0m";
		}
	}

	public class PlainMemberColorizer : MemberColorizer
	{
		public PlainMemberColorizer(params Type[] blacklistAttributes) : base(blacklistAttributes) { }

		public override string Colorize(string text, TokenType type)
		{
			return text;
		}
	}

	public abstract class MemberColorizer
	{
		public static MemberColorizer _default = null;
		public static MemberColorizer Default
		{
			get => _default ??= new PlainMemberColorizer();
			set => _default = value;
		}

		public static readonly string DEFAULT_KEYWORD_COLOR = "#D65685";
		public static readonly string DEFAULT_CLASS_COLOR = "#4EC9B0";
		public static readonly string DEFAULT_STRUCT_COLOR = "#4ec971";
		public static readonly string DEFAULT_KEY_TYPE_COLOR = "#39c54c";
		public static readonly string DEFAULT_FIELD_COLOR = "#75C9F6";
		public static readonly string DEFAULT_PROP_COLOR = "#6a9de4";
		public static readonly string DEFAULT_METHOD_COLOR = "#DCDCAA";
		public static readonly string DEFAULT_PARAMETER_COLOR = "#bde8ff";
		public static readonly string DEFAULT_STRING_COLOR = "#CE9178";
		public static readonly string DEFAULT_NUMERIC_COLOR = "#B5CEA8";

		public readonly struct Result
		{
			public readonly string RichText;
			public readonly string PlainText;
			public readonly int Lines;

			public Result(string richText, string plainText, int lines)
			{
				RichText = richText;
				PlainText = plainText;
				Lines = lines;
			}
		}

		private readonly StringBuilder rich;
		private readonly StringBuilder plain;
		public readonly Type[] BlacklistAttributes;

		private int indent = 0;
		private int lines = 0;

		public MemberColorizer(params Type[] blacklistAttributes)
		{
			rich = new StringBuilder();
			plain = new StringBuilder();
			BlacklistAttributes = blacklistAttributes;
		}

		public abstract string Colorize(string text, TokenType type);

		public void AppendColorized(string text, TokenType type)
		{
			// rich.Append("<color=");
			// rich.Append(color);
			// rich.Append('>');
			// Append(text);
			// rich.Append("</color>");
			plain.Append(text);
			rich.Append(Colorize(text, type));
		}

		public void Append(ReadOnlySpan<char> text)
		{
			plain.Append(text);
			rich.Append(text);
		}

		public void Append(string text)
		{
			plain.Append(text);
			rich.Append(text);
		}

		public void Append(char c)
		{
			plain.Append(c);
			rich.Append(c);
		}

		public void Cut(int count)
		{
			plain.Length -= count;
			rich.Length -= count;
		}

		public void NewLine()
		{
			Append('\n');
			Span<char> indent = stackalloc char[this.indent * 4];
			indent.Fill(' ');
			Append(indent);
			lines++;
		}

		public enum TokenType
		{
			Keyword,
			Class,
			Struct,
			KeyType,
			Field,
			Property,
			Method,
			Parameter,
			String,
			Numeric,
		}

		public void AppendType(Type type)
		{
			if (type.IsGenericType || type.IsGenericTypeDefinition)
			{
				// If is nullable, just append '?'
				if (type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>))
				{
					AppendType(type.GetGenericArguments()[0]);
					Append('?');
					return;
				}

				string name = type.Name;
				name = name.Substring(0, name.IndexOf('`'));

				if (name == "ValueTuple" && type.Namespace == "System")
				{
					Append('(');
					foreach (Type arg in type.GetGenericArguments())
					{
						AppendType(arg);
						Append(", ");
					}
					Cut(2);
					Append(')');
					return;
				}

				AppendColorized(name, type.IsValueType ? TokenType.Struct : TokenType.Class);
				Append('<');
				foreach (Type arg in type.GetGenericArguments())
				{
					AppendType(arg);
					Append(", ");
				}
				Cut(2);
				Append('>');
				return;
			}

			if (type.IsEnum)
			{
				AppendColorized(type.Name, TokenType.Struct);
				return;
			}

			switch (Type.GetTypeCode(type))
			{
				case TypeCode.Boolean:
					AppendColorized("bool", TokenType.KeyType);
					break;
				case TypeCode.Byte:
					AppendColorized("byte", TokenType.KeyType);
					break;
				case TypeCode.Char:
					AppendColorized("char", TokenType.KeyType);
					break;
				case TypeCode.Decimal:
					AppendColorized("decimal", TokenType.KeyType);
					break;
				case TypeCode.Double:
					AppendColorized("double", TokenType.KeyType);
					break;
				case TypeCode.Int16:
					AppendColorized("short", TokenType.KeyType);
					break;
				case TypeCode.Int32:
					AppendColorized("int", TokenType.KeyType);
					break;
				case TypeCode.Int64:
					AppendColorized("long", TokenType.KeyType);
					break;
				case TypeCode.Object:
					if (type == typeof(object))
						AppendColorized("object", TokenType.KeyType);
					else if (type == typeof(void))
						AppendColorized("void", TokenType.KeyType);
					else goto default;
					break;
				case TypeCode.SByte:
					AppendColorized("sbyte", TokenType.KeyType);
					break;
				case TypeCode.Single:
					AppendColorized("float", TokenType.KeyType);
					break;
				case TypeCode.String:
					AppendColorized("string", TokenType.KeyType);
					break;
				case TypeCode.UInt16:
					AppendColorized("ushort", TokenType.KeyType);
					break;
				case TypeCode.UInt32:
					AppendColorized("uint", TokenType.KeyType);
					break;
				case TypeCode.UInt64:
					AppendColorized("ulong", TokenType.KeyType);
					break;
				case TypeCode.Empty:
					AppendColorized("void", TokenType.KeyType);
					break;
				default:
					AppendColorized(type.Name, type.IsValueType ? TokenType.Struct : TokenType.Class);
					break;
			}
		}

		public void AppendParameters(ParameterInfo[] parameters)
		{
			// return $"{string.Join(", ", parameters.Select(p => $"{p.ParameterType.Name} {p.Name}"))}";
			for (int i = 0; i < parameters.Length; i++)
			{
				if (i > 0)
					Append(", ");

				AppendType(parameters[i].ParameterType);
				Append(' ');
				AppendColorized(parameters[i].Name, TokenType.Parameter);
			}
		}

		public void AppendAccessors(bool basePublic, params MethodInfo[] infos)
		{
			Append(" { ");
			foreach (var info in infos)
			{
				if (info == null)
					continue;

				if (info.IsPublic && !basePublic)
					AppendColorized("public ", TokenType.Keyword);
				else if (!info.IsPublic && basePublic)
					AppendColorized("private ", TokenType.Keyword);

				string name = info.Name;
				AppendColorized(name.Substring(0, name.IndexOf("_")), TokenType.Keyword);
				Append("; ");
			}

			if (plain[^1] == '{')
			{
				Cut(3);
				Append(';');
			}
			else Append("}");
		}

		public void AppendScope(bool isPublic, bool isStatic)
		{
			if (isPublic)
				AppendColorized("public ", TokenType.Keyword);
			else AppendColorized("private ", TokenType.Keyword);

			if (isStatic)
				AppendColorized("static ", TokenType.Keyword);
		}

		public void AppendAttributes(IList<CustomAttributeData> attributes, bool backingField)
		{
			for (int i1 = 0; i1 < attributes.Count; i1++)
			{
				CustomAttributeData attribute = attributes[i1];

				if (BlacklistAttributes.Contains(attribute.Constructor.DeclaringType) || (backingField && attribute.Constructor.DeclaringType == typeof(CompilerGeneratedAttribute)))
					continue;

				Append("[");
				if (backingField)
				{
					rich.Append("<i>");
					Append("field: ");
					rich.Append("</i>");
				}

				string name = attribute.Constructor.DeclaringType.Name;
				if (name.EndsWith("Attribute"))
					name = name.Substring(0, name.Length - 9);

				AppendColorized(name, TokenType.Class);

				bool first = true;

				int argCount;
				ParameterInfo[] constructorParameters = attribute.Constructor.GetParameters();
				for (argCount = attribute.ConstructorArguments.Count; argCount > 0; argCount--)
				{

					if (!constructorParameters[argCount - 1].HasDefaultValue)
						break;
					object value = attribute.ConstructorArguments[argCount - 1].Value;
					object def = constructorParameters[argCount - 1].DefaultValue;

					if (value == null && def == null)
						continue;
					if (value.Equals(def))
						continue;

					break;
				}

				for (int i = 0; i < argCount; i++)
				{
					if (first)
					{
						Append('(');
						first = false;
					}
					else Append(", ");

					AppendCustomAttributeTypedArgument(attribute.ConstructorArguments[i], null);
				}

				for (int i = 0; i < attribute.NamedArguments.Count; i++)
				{
					if (first)
					{
						Append('(');
						first = false;
					}
					else Append(", ");

					var member = attribute.NamedArguments[i].MemberInfo;
					var memberType = (member as PropertyInfo)?.PropertyType ?? (member as FieldInfo)?.FieldType;
					AppendColorized(attribute.NamedArguments[i].MemberName, member is PropertyInfo ? TokenType.Property : TokenType.Field);
					Append(" = ");

					AppendCustomAttributeTypedArgument(attribute.NamedArguments[i].TypedValue, memberType);
				}

				if (!first) Append(')');

				Append(']');
				NewLine();
			}
		}

		public void AppendCustomAttributeTypedArgument(CustomAttributeTypedArgument data, Type memberType)
		{
			var value = data.Value;
			var argumentType = data.ArgumentType.IsEnum || value == null ? data.ArgumentType : value.GetType();

			void AppendCast()
			{
				bool typed = argumentType != memberType && memberType != typeof(object) && memberType != null;
				if (!typed) return;

				Append('(');
				AppendType(argumentType);
				Append(')');
			}

			if (argumentType == null) Append("<null>");

			if (argumentType.IsEnum && Enum.IsDefined(argumentType, value))
			{
				var enumValue = Enum.ToObject(argumentType, value);
				AppendCast();
				AppendType(argumentType);
				Append('.');
				AppendColorized(enumValue.ToString(), TokenType.Numeric);
			}


			else if (value == null)
			{
				AppendCast();
				AppendColorized("null", TokenType.Keyword);
			}
			else if (argumentType == typeof(string))
				AppendColorized($"\"{value}\"", TokenType.String);

			else if (argumentType == typeof(char))
				AppendColorized($"'{value}'", TokenType.String);

			else if (Type.GetTypeCode(argumentType) >= TypeCode.SByte && Type.GetTypeCode(argumentType) <= TypeCode.Decimal)
			{
				AppendCast();
				AppendColorized(value.ToString(), TokenType.Numeric);
			}

			else if (argumentType == typeof(bool))
			{
				AppendCast();
				AppendColorized((bool)value ? "true" : "false", TokenType.Keyword);
			}

			else if (argumentType == typeof(Type))
			{
				AppendColorized("typeof", TokenType.Keyword);
				Append('(');
				AppendType((Type)value);
				Append(')');
			}

			else if (argumentType.IsArray)
			{
				IList<CustomAttributeTypedArgument> array = value as IList<CustomAttributeTypedArgument>;

				Type elementType = argumentType.GetElementType();
				AppendColorized("new ", TokenType.Keyword);
				AppendType(elementType);
				Append(" { ");

				for (int ai = 0; ai < array.Count; ai++)
				{
					if (ai > 0) Append(", ");
					AppendCustomAttributeTypedArgument(array[ai], elementType);
				}

				Append(" }");
			}

			else {
				AppendCast();
				Append(value.ToString());
			}
		}

		public Result DeclarationContent(MemberInfo member, BindingFlags? expandTypeMembers = null)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			rich.Clear();
			plain.Clear();
			lines = 1;

			AddMember(member, expandTypeMembers);

			return new Result(rich.ToString(), plain.ToString(), lines);
		}

		public void AddMember(MemberInfo member, BindingFlags? expandTypeMembers = null)
		{
			if (member == null)
				throw new ArgumentNullException(nameof(member));

			var attributes = member.GetCustomAttributesData();
			bool backingField = false;

			string memberName = member.Name;
			if (memberName[0] == '<' && memberName.EndsWith(">k__BackingField"))
			{
				string propertyName = memberName.Substring(1, memberName.Length - 17);
				PropertyInfo property = member.DeclaringType.GetProperty(propertyName.ToString(), BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic);
				if (property != null)
				{
					member = property;
					backingField = true;
					memberName = propertyName;
				}
			}

			AppendAttributes(attributes, backingField);

			switch (member.MemberType)
			{
				case MemberTypes.Constructor: {
					ConstructorInfo constructor = (ConstructorInfo)member;
					AppendScope(constructor.IsPublic, constructor.IsStatic);
					AppendType(constructor.DeclaringType);
					Append('(');
					AppendParameters(constructor.GetParameters());
					Append(") { ... }");
					break;
				}

				case MemberTypes.Event: {
					EventInfo @event = (EventInfo)member;
					bool isPublic = @event.RaiseMethod?.IsPublic == true || @event.AddMethod?.IsPublic == true || @event.RemoveMethod?.IsPublic == true;
					bool isStatic = @event.RaiseMethod?.IsStatic == true || @event.AddMethod?.IsStatic == true || @event.RemoveMethod?.IsStatic == true;
					AppendScope(isPublic, isStatic);
					AppendColorized("event ", TokenType.Keyword);
					AppendType(@event.EventHandlerType ?? typeof(void));
					Append(' ');
					AppendColorized(memberName, TokenType.Field);
					AppendAccessors(isPublic, @event.RaiseMethod, @event.AddMethod, @event.RemoveMethod);
					break;
				}

				case MemberTypes.Field: {

					FieldInfo field = (FieldInfo)member;

					AppendScope(field.IsPublic, field.IsStatic);
					if (field.IsLiteral)
						AppendColorized("const ", TokenType.Keyword);
					AppendType(field.FieldType);
					Append(' ');
					AppendColorized(memberName, TokenType.Field);
					Append(';');

					
					break;
				}

				case MemberTypes.Method:{
					MethodInfo method = (MethodInfo)member;
					AppendScope(method.IsPublic, method.IsStatic);
					AppendType(method.ReturnType);
					Append(' ');
					AppendColorized(memberName, TokenType.Method);

					if (method.IsGenericMethod)
					{
						Append('<');
						foreach (Type arg in method.GetGenericArguments())
						{
							AppendType(arg);
							Append(", ");
						}
						Cut(2);
						Append('>');
					}

					Append('(');
					AppendParameters(method.GetParameters());
					Append(") { ... }");
					break;
				}

				case MemberTypes.Property: {
					PropertyInfo property = (PropertyInfo)member;
					bool isPublic = property.GetMethod?.IsPublic == true || property.SetMethod?.IsPublic == true;
					bool isStatic = property.GetMethod?.IsStatic == true || property.SetMethod?.IsStatic == true;
					AppendScope(isPublic, isStatic);
					AppendType(property.PropertyType);
					Append(' ');

					if (property.GetIndexParameters().Length > 0)
					{
						AppendColorized("this", TokenType.Keyword);
						Append('[');
						AppendParameters(property.GetIndexParameters());
						Append(']');
					}
					else AppendColorized(memberName, TokenType.Property);
					AppendAccessors(isPublic, property.GetMethod, property.SetMethod);
					break;
				}

				case MemberTypes.TypeInfo:
				case MemberTypes.NestedType:
					Type type = (Type)member;
					AppendScope(type.IsPublic, type.IsAbstract && type.IsSealed);

					// StringBuilder builder = new StringBuilder();
					if (type.IsAbstract && !type.IsSealed)
						AppendColorized("abstract ", TokenType.Keyword);
					if (type.IsSealed && !type.IsAbstract)
						AppendColorized("sealed ", TokenType.Keyword);
					AppendColorized(type.IsValueType ? "struct " : "class ", TokenType.Keyword);
					AppendType(type);

					IEnumerable<Type> extends = type.GetInterfaces();
					if (type.BaseType != null && type.BaseType != typeof(object))
						extends = extends.Prepend(type.BaseType);

					if (extends.Any())
					{
						Append(" : ");
						foreach (Type t in extends)
						{
							AppendType(t);
							Append(", ");
						}
						Cut(2);
					}
					if (!expandTypeMembers.HasValue)
						Append(" { ... }");
					else {
						NewLine();
						Append('{');
						lines++;
						indent++;
						foreach (var m in type.GetMembers(expandTypeMembers.Value))
						{
							NewLine();
							AddMember(m, expandTypeMembers);
						}
						indent--;
						NewLine();
						Append('}');
					}
					break;

				case MemberTypes.Custom:
				case MemberTypes.All:
					throw new ArgumentException("Member type is not supported: " + member.MemberType + " (" + member + ")", nameof(member));

				default:
					throw new ArgumentOutOfRangeException(nameof(member));
			}
		}
	}
}