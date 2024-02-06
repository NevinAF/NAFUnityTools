#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Collections.Generic;
	using System.Globalization;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;


	/// <summary>
	/// Parses C# code without any allocations, using a compiler context for buffers.
	/// </summary>
	public ref partial struct Parser
	{
		#region Public (Static) API

		/// <summary> Parses the expression into a single expression. </summary>
		/// <param name="compiler"> The compiler context for buffers and utility. </param>
		/// <param name="expression"> The expression to parse. </param>
		/// <param name="parameters"> The parameters to use when parsing. </param>
		/// <returns> The parsed expression. </returns>
		public static Expression SingleExpression(Compiler compiler, ReadOnlySpan<char> expression, params Expression[] parameters)
		{
			Parser parser = new Parser(compiler, expression, parameters);
			Term term = parser.ParseExpression();
			parser.ExpectEnd();

			return parser.ExpectExpression(term);
		}

		#endregion Public (Static) API

		#region Private Parsing

		private const BindingFlags _flags = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static | BindingFlags.FlattenHierarchy;

		/// <summary>The compiler context for buffers and utility. </summary>
		private readonly Compiler compiler;
		/// <summary>The expression to parse. </summary>
		private readonly ReadOnlySpan<char> expression;
		/// <summary>The parameters to use when parsing. </summary>
		private readonly Expression[] parameters;
		/// <summary>The lexer used to get tokens. </summary>
		/// <remarks>Lexer is not readonly because it is a ref struct that changes</remarks>
		private Lexer lexer;

		/// <summary> Creates a new parser. </summary>
		/// <param name="compiler"> The compiler context for buffers and utility. </param>
		/// <param name="expression"> The expression to parse. </param>
		/// <param name="parameters"> The parameters to use when parsing. </param>
		private Parser(Compiler compiler, ReadOnlySpan<char> expression, params Expression[] parameters)
		{
			this.compiler = compiler;
			this.expression = expression;
			this.parameters = parameters;
			this.lexer = new Lexer(expression);
		}

		/// <summary> Expect the end of the expression, useful for catching unexpected ending characters or extra code that was not used. </summary>
		private void ExpectEnd()
		{
			lexer.Expected(TokenType.EndOfFile, "Expected the end of the expression, but there is still more to parse.");
		}

		internal readonly Expression ExpectExpression(in Term term)
		{
			if (term.expression == null)
				throw new SymanticException(expression, term.start, term.length, "Expected an expression but got a static type reference.");
			return term.expression;
		}

		internal readonly Expression ExpectTerm(in Term term)
		{
			Expression e = ExpectExpression(term);
			if (e.Type == typeof(void))
				throw new SymanticException(expression, term.start, term.length, "Void expressions cannot be used as terms.");
			return e;
		}

		internal readonly Type ExpectType(in Term term)
		{
			if (term.staticTypeReference == null)
				throw new SymanticException(expression, term.start, term.length, "Expected a static type reference but got an expression.");
			return term.staticTypeReference;
		}

		/// <summary> Parses a "term (operator term)*" pattern. </summary>
		private Term ParseExpression()
		{
			// Get all terms and operators so that they can be ordered by precedence.
			StackSegment<Term> terms = compiler.CreateTermSegment();
			StackSegment<Token> operators = compiler.CreateOperatorSegement();
			int topPrecedence = int.MaxValue;

			terms.Push(ParseTerm());

			while (true)
			{
				Token operatorToken = lexer.NextToken();

				if (operatorToken.type.IsTerminatingPunctuation())
				{
					lexer.Return(operatorToken);
					break;
				}

				if (!operatorToken.type.IsOperator())
					throw new SymanticException(expression, operatorToken, $"Expected an operator or puncuation, but got '{operatorToken.type}'");

				int precedence = operatorToken.type.MultiaryPrecedence();
				Term term = ParseTerm();

				// Special case for Ternary Conditional Operator (non-binary operator)
				if (operatorToken.type == TokenType.ConditionalColon)
				{
				}

				// If the new operator should be evaluated after the previous operator, evaluate the previous operator first.
				else while (operators.Count > 0 && precedence >= topPrecedence)
				{
					Term right = terms.Pop();
					terms.Push(TopOperator(ref terms, ref operators, right));

					if (operators.Count == 0) break;
					topPrecedence = operators.Peek().type.MultiaryPrecedence();
				}

				terms.Push(term);
				operators.Push(operatorToken);
				topPrecedence = precedence;
			}

			// Apply all remaining operators
			{
				Term right = terms.Pop();
				while (operators.Count > 0 && terms.Count > 0)
					right = TopOperator(ref terms, ref operators, right);

				if (terms.Count > 0)
					throw new InternalException(expression,right, "There should be no terms left after applying all operators.");
				if (operators.Count > 0)
					throw new InternalException(expression,right, "There should be no operators left after applying all operators.");

				// terms.Dispose(); // <- Does nothing as the list is empty.
				// operators.Dispose(); // <- Does nothing as the list is empty.

				return right;
			}
		}

		private Term TopOperator(ref StackSegment<Term> terms, ref StackSegment<Token> operators, Term right)
		{
			if (terms.Count == 0)
				throw new InternalException(expression, right, "There should always be at least one stack term when evaluating operators. This is an internal error.");
			if (terms.Count != operators.Count)
				throw new InternalException(expression, right, "There should always be one more term than operators. This is an internal error.");

			Token op = operators.Pop();
			Term left = terms.Pop();

			if (op.type != TokenType.ConditionalColon)
				return MakeBinary(op, left, right);


			// Evaluate until the top operator is the '?', which marks the end of the true term.
			while (operators.Count > 0 && operators.Peek().type != TokenType.ConditionalQuestion)
				left = MakeBinary(operators.Pop(), terms.Pop(), left);

			if (operators.Count == 0)
				throw new SymanticException(expression, op, "The ternary conditional operator ':' must always be preceded by a '?' operator.");

			Token q = operators.Pop(); // Remove the '?'
			Term conditionTerm = terms.Pop();

			return Conditional(q, op, conditionTerm, left, right);
		}

		/// <summary> Parses a single term. </summary>
		private Term ParseTerm()
		{
			Token prefixOperator = default;

			Token token = lexer.NextToken();
			int termStart = token.start;

			// Check for prefix unary operators
			if (token.type.IsPrefixUnary())
			{
				prefixOperator = token;
				token = lexer.NextToken();
			}

			// Check for term starters (literals, identifiers, parenthesis, etc.)
			Term current = token.type switch
			{
				TokenType.Identifier => ParseStaticIdentifier(token),
				TokenType.Number => ParseNumber(token),
				TokenType.String => ParseString(token),
				TokenType.Character => ParseCharacter(token),
				TokenType.True => new Term(Expression.Constant(true), token.start, token.length),
				TokenType.False => new Term(Expression.Constant(false), token.start, token.length),
				TokenType.Null => new Term(Expression.Constant(null), token.start, token.length),
				TokenType.Base => ParseBase(token),
				TokenType.This => ParseThis(token),
				TokenType.BraceOpen => ParseParameterIndex(token),
				TokenType.Typeof => ParseTypeOf(token),
				TokenType.ParenthesisOpen => ParseParenthesis(token),
				TokenType.New => ParseNew(token),
				_ => throw new SymanticException(expression, token, $"Expected the start of a term (like an identifier, literal, group, cast..), but got '{token.type}'"),
			};

			// Apply prefix unary operators
			if (prefixOperator.type != TokenType.Invalid)
				MakeUnary(ref current, prefixOperator);

			FinishOutTerm(ref current);
			return current;
		}

		private void FinishOutTerm(ref Term current)
		{
			Token token = lexer.NextToken();
			switch (token.type)
			{
				case TokenType.Increment:
				case TokenType.Decrement:
					{
						MakeUnary(ref current, token);
						return; // Must be the end of the term.
					}

				case TokenType.MemberAccess:
					{
						current.length += token.length;
						ParseMemberAccess(ref current);
						FinishOutTerm(ref current);
						return;
					}

				case TokenType.BracketOpen:
					{
						current.length += token.length;
						ParseIndexer(ref current, token);
						FinishOutTerm(ref current);
						return;
					}

				case TokenType.NullConditional:
					{
						current.length += token.length;
						NullConditional(ref current, token); // This finishes out the term.
						return;
					}

				default:
					// End of term. Return the token to the peek buffer.
					lexer.Return(token);
					return;
			}
		}

		private Term ParseStaticIdentifier(in Token token)
		{
			ReadOnlySpan<char> span = lexer.GetTokenSpan(token);
		
			// First, check for primitive types that don't share an identifier with its type.
			Type? primitive = ReflectionUtility.MatchesPrimitive(span);
			if (primitive != null)
				return new Term(primitive, token.start, token.length);

			// Check to see if identifier is a member on the host type.
			MemberAccessException? fallbackException = null;
			if (parameters.Length != 0)
			{
				MemberInfo[] members = parameters[0].Type.GetMember(span.ToString(), _flags);
				if (members.Length > 0)
				{
					try {
						Term memberTerm = new Term(parameters![0], token.start, 0);
						ParseMemberAccess(ref memberTerm, members, token);
						return memberTerm;
					}
					// If there was issues getting the member, it's likely a static member.
					catch (MemberAccessException e) { fallbackException = e; }
				}
			}

			Span<Token> pathParts = stackalloc Token[8]; // maximum number of nested namespaces plus type.
			int pathLength = 1;

			pathParts[0] = token;
			while (lexer.AcceptIf(TokenType.MemberAccess) && pathLength < pathParts.Length - 1)
			{
				pathParts[pathLength] = lexer.Expected(TokenType.Identifier, "An idendifier should always follow a member access operator '.', unless the dot is a part of a number.");
				pathLength++;
			}
			pathParts = pathParts[..pathLength];

			bool includeGeneric = lexer.PeekIs(TokenType.LessThan);

			int bestLength = 0;
			Type? bestType = null;
			MemberInfo[]? bestMembers = null;

			Type[] referenceTypes = compiler.ReferenceTypes;
			for (int t = 0; t < referenceTypes.Length; t++)
			{
				Type type = referenceTypes[t];

				int matchLength = MatchPathV2(lexer, pathParts, type, includeGeneric);

				if (matchLength > bestLength)
				{
					if (matchLength != pathParts.Length)
					{
						// get the relevant part
						string path =  lexer.GetTokenSpan(pathParts[matchLength]).ToString();

						bestMembers = type.GetMember(path, _flags);
						if (bestMembers.Length == 0)
						{
							bestMembers = null;
							continue;
						}
					}
					else bestMembers = null;

					bestLength = matchLength;
					bestType = type;
				}
			}


			if (bestType == null)
			{
				if (fallbackException != null)
					throw fallbackException;

				Token parts = pathParts[0];
				parts.length = pathParts[^1].start + pathParts[^1].length - parts.start;
				throw new ParserException(expression, token, $"The identifier path '{lexer.GetTokenSpan(parts).ToString()}' did not resolve to any <namespace>.<type>.<members>. Make sure the type and members exist.");
			}

			// The whole path has been used to get the type, so just return the type.
			if (bestLength == pathParts.Length)
			{
				return CheckForGenerics(bestType, token);
			}

			Term term = new Term(bestType, token.start, lexer.Peek().start - token.start);

			// For all of the best length not used, manually apply the member access.
			ParseMemberAccess(ref term, bestMembers!, pathParts[bestLength]);
			for (int i = bestLength + 1; i < pathParts.Length; i++)
			{
				Token part = pathParts[i];
				ParseMemberAccess(ref term, part);
			}

			return term;
		}

		private Term ParseNumber(in Token token)
		{
			ReadOnlySpan<char> _span = lexer.GetTokenSpan(token);

			try {
				Expression expression = NumberExpression(_span);
				return new Term(expression, token.start, token.length);
			}
			catch (Exception e)
			{
				throw new ParserException(expression, token, "Invalid number format", e);
			}
		}

		private Term ParseString(in Token token)
		{
			ReadOnlySpan<char> span = lexer.GetTokenSpan(token);
			// Remove escaped characters (anything affer a \)
			Span<char> buffer = stackalloc char[span.Length];
			int i = 0;
			for (int j = 1; j < span.Length - 1; j++)
			{
				char c = span[j];
				if (c == '\\')
				{
					j++;
					c = span[j];
				}
				buffer[i++] = c;
			}

			return new Term(Expression.Constant(buffer.Slice(0, i).ToString(), typeof(string)), token.start, token.length);
		}

		private Term ParseCharacter(in Token token)
		{
			ReadOnlySpan<char> span = lexer.GetTokenSpan(token);

			Expression expression = (span.Length == 4) ?
				Expression.Constant(span[2], typeof(char)) :
				Expression.Constant(span[1], typeof(char));

			return new Term(expression, token.start, token.length);
		}

		private Term ParseBase(in Token token)
		{
			if (parameters.Length == 0)
				throw new ParserException(expression, token, "Cannot use the keyword 'base' in a static context");
			Type? baseType = parameters[0].Type.BaseType;
			if (baseType == null)
				throw new ParserException(expression, token, "Keyword 'base' is being used as member access on type '" + parameters[0].Type + ". It should only ever be used as a identifier to represent the target object (like normal 'base' keyword in C#).");

			return new Term(Expression.Convert(parameters![0], baseType), token.start, token.length);
		}

		private Term ParseThis(in Token token)
		{
			if (parameters.Length == 0)
				throw new ParserException(expression, token, "Cannot use the keyword 'this' in a static context");

			return new Term(parameters[0], token.start, token.length);
		}

		private Term ParseParameterIndex(in Token token)
		{
			Token number = lexer.Expected(TokenType.Number, "An expression started with a curly brace '{' is always interpreted as an input parameter by index, and should follow the '{<int>}' format.");
			int index = int.Parse(lexer.GetTokenSpan(number));
			Token close = lexer.Expected(TokenType.BraceClose, "An expression started with a curly brace '{' is always interpreted as an input parameter by index, and should follow the '{<int>}' format.");

			int start = token.start;
			int length = close.start + close.length - start;

			if (parameters == null)
				throw new SymanticException(expression, start, length, "A parameter by index term, '{<int>}' can only be used when the expression has parameters.");

			if (index < 0 || index >= parameters!.Length)
				throw new SymanticException(expression, start, length, $"The parameter by index term '{{<int>}}' has an out of range index: '{index}' given out of {parameters.Length} parameters.");

			return new Term(parameters[index], start, length);
		}

		private Term ParseTypeOf(in Token token)
		{
			lexer.Expected(TokenType.ParenthesisOpen, "The keyword 'typeof' should always be followed by a parenthesis '(' and then an Identifier.");
			Type type = ExpectType(ParseExpression());
			Token end = lexer.Expected(TokenType.ParenthesisClose, "The keyword 'typeof' should always be followed by a parenthesis '(' and then an Identifier.");

			return new Term(Expression.Constant(type, typeof(Type)), token.start, end.start + end.length - token.start);
		}

		private Term ParseParenthesis(in Token token)
		{
			Term result = ParseExpression();
			Token close = lexer.Expected(TokenType.ParenthesisClose, "A parenthesis '(' must always be closed with a parenthesis ')'.");

			result.start = token.start;

			// Check for cast
			if (result.staticTypeReference != null)
			{
				Term term = ParseTerm(); // Parse a term right after this

				try {
					result.expression = Convert(term, result.staticTypeReference);
					result.length = term.start + term.length - result.start;
				}
				catch (Exception e) {
					result.length = term.start + term.length - result.start;
					throw new SymanticException(expression, result, "Term was interpreted as a cast '(type)Term', but there is no cast between (term) " + ExpectTerm(term).Type + " and (type) " + result.staticTypeReference + ".", e);
				}
			}
			else result.length = close.start + close.length - token.start;

			return result;
		}

		private Term ParseNew(in Token token)
		{
			Term type = ParseTerm();
			Type t = ExpectType(type);

			lexer.Expected(TokenType.ParenthesisOpen, "The 'new' keyword must always be followed by a type and then a parenthesis '(' and then a list of arguments.");
			using var arguments = ParseArguments(TokenType.ParenthesisClose);

			ConstructorInfo[] constructors = t.GetConstructors(_flags);
			int index = SetupBestParameterMatch(type, constructors, arguments, out var convertedArguments);

			if (index == -1 || convertedArguments == null)
				throw new MemberAccessException(expression, token, type, arguments, constructors);

			return new Term(Expression.New(constructors[index], convertedArguments), token.start, token.length + type.length + 2);
		}

		private void ParseMemberAccess(ref Term target)
		{
			Token token = lexer.Expected(TokenType.Identifier, "An idendifier should always follow a member access operator '.', unless the dot is a part of a number.");
			ParseMemberAccess(ref target, token);
		}

		private void ParseMemberAccess(ref Term target, in Token token)
		{
			string member = lexer.GetTokenSpan(token).ToString();
			Type type = target.staticTypeReference ?? ExpectTerm(target).Type;
			MemberInfo[] members = type.GetMember(member, _flags);

			ParseMemberAccess(ref target, members, token);
		}

		private void ParseMemberAccess(ref Term target, MemberInfo[] members, in Token token)
		{
			bool staticContext = target.staticTypeReference != null;
			Type type = staticContext ? ExpectType(target) : ExpectTerm(target).Type;

			target.length = token.start + token.length - target.start;

			if (members.Length == 0)
				throw new MemberAccessException(expression, target, token, $"No member with name '{lexer.GetTokenSpan(token).ToString()}' found on type '{type}'.");

			MemberInfo memberInfo = members[0];
			Expression? hostParameter = parameters?.FirstOrDefault();

			if (memberInfo.MemberType == MemberTypes.NestedType)
			{
				Type nestedType = (Type)memberInfo;
				if (staticContext || target.expression == hostParameter)
				{
					target.staticTypeReference = nestedType;
					return;
				}
				throw new MemberAccessException(expression, target, token, $"Nested types cannot be access through instances of a type. Directly use '{type.Name}.{lexer.GetTokenSpan(token).ToString()}' instead to access the static reference to the nested type.");
			}

			target.staticTypeReference = null; // Won't need this anymore;


			if (memberInfo.MemberType is MemberTypes.Field)
			{
				FieldInfo fieldInfo = (FieldInfo)memberInfo;
				if (fieldInfo.IsLiteral)
				{
					target.expression = Expression.Constant(fieldInfo.GetValue(null));
					return;
				}

				if (members.Length != 1)
					throw new InternalException(expression, token, $"Multiple members with name '{lexer.GetTokenSpan(token).ToString()}' found on type '{type}'. Fields should never be able to provide overloads! Members:\n\t{string.Join("\n\t", members.Select(m => MemberColorizer.Default.DeclarationContent(m).RichText))}");

				if (staticContext)
				{
					if (!fieldInfo.IsStatic)
						throw new MemberAccessException(expression, target, token, $"Member fields cannot be access through static context. An instance of the object must be used to get '{type.Name}.{lexer.GetTokenSpan(token).ToString()}'.");
				}
				else if (fieldInfo.IsStatic)
				{
					if (target.expression == hostParameter)
						target.expression = null;
					else throw new MemberAccessException(expression, target, token, $"Static fields cannot be access through instances of a type. Directly use '{type.Name}.{lexer.GetTokenSpan(token).ToString()}' instead to access the static field.");
				}

				target.expression = Expression.Field(target.expression, fieldInfo);
				return;
			}

			if (memberInfo.MemberType is MemberTypes.Property)
			{
				PropertyInfo propertyInfo = (PropertyInfo)memberInfo;
				if (members.Length != 1)
					throw new InternalException(expression, token, $"Multiple members with name '{lexer.GetTokenSpan(token).ToString()}' found on type '{type}'. Properties that are not indexers ('[]') should never be able to provide overloads! Members:\n\t{string.Join("\n\t", members.Select(m => MemberColorizer.Default.DeclarationContent(m).RichText))}");

				ParameterInfo[] parameters = propertyInfo.GetIndexParameters();
				if (parameters.Length > 0)
					throw new InternalException(expression, token, $"Properties with parameters cannot be access through a member access operator as they have no implicit name.");

				MethodInfo method = propertyInfo.GetMethod ?? propertyInfo.SetMethod ?? throw new InternalException(expression, token, $"Property '{lexer.GetTokenSpan(token).ToString()}' on type '{type}' has no get or set method. Inside expressions, properties can only be accessed or set.");
				if (staticContext)
				{
					if (!method.IsStatic)
						throw new MemberAccessException(expression, target, token, $"Member properties cannot be access through static context. An instance of the object must be used to get '{type.Name}.{lexer.GetTokenSpan(token).ToString()}'.");
				}
				else if (method.IsStatic)
				{
					if (target.expression == hostParameter)
						target.expression = null;
					else throw new MemberAccessException(expression, target, token, $"Static properties cannot be access through instances of a type. Directly use '{type.Name}.{lexer.GetTokenSpan(token).ToString()}' instead to access the static property.");
				}

				target.expression = Expression.Property(target.expression, propertyInfo);
				return;
			}

			if (memberInfo.MemberType is MemberTypes.Constructor)
			{
				throw new MemberAccessException(expression, target, token, $"Constructors cannot be called directly. Use the 'new' keyword instead.");
			}

			else if (memberInfo.MemberType is MemberTypes.TypeInfo or MemberTypes.NestedType)
			{
				throw new InternalException(expression, token, $"Member '{lexer.GetTokenSpan(token).ToString()}' on type '{type}' is a type. Only nested types can be accessed through a member access operator.");
			}

			else if (memberInfo.MemberType is MemberTypes.Custom or MemberTypes.All)
			{
				throw new InternalException(expression, token, $"Member '{lexer.GetTokenSpan(token).ToString()}' on type '{type}' is of an unknown type: '{memberInfo.GetType()}' ({memberInfo.MemberType}).\n\t\t{string.Join("\n\t\t", members.Select(m => MemberColorizer.Default.DeclarationContent(m).RichText))}");
			}

			lexer.Expected(TokenType.ParenthesisOpen, memberInfo.MemberType == MemberTypes.Event ? "Events must be raised and cannot be used in any other way." : "Methods must be called and cannot be interpreted as a delegate.");
			using StackSegment<Term> arguments = ParseArguments(TokenType.ParenthesisClose);

			if (memberInfo.MemberType is MemberTypes.Event)
			{
				EventInfo eventInfo = (EventInfo)memberInfo;
				MethodInfo method = eventInfo.GetRaiseMethod() ??
					throw new MemberAccessException(expression, target, token, $"Event '{lexer.GetTokenSpan(token).ToString()}' on type '{type}' has no raise method. Inside expressions, events can only be raised.");

				if (members.Length != 1)
					throw new InternalException(expression, token, $"Multiple members with name '{lexer.GetTokenSpan(token).ToString()}' found on type '{type}'. Events should never be able to provide overloads! Members:\n\t{string.Join("\n\t", members.Select(m => MemberColorizer.Default.DeclarationContent(m).RichText))}");

				if (staticContext)
				{
					if (!method.IsStatic)
						throw new MemberAccessException(expression, target, token, $"Static events cannot be access through static context. An instance of the object must be used to raise '{type.Name}.{lexer.GetTokenSpan(token).ToString()}'.");
				}
				else if (method.IsStatic)
				{
					if (target.expression == hostParameter)
						target.expression = null;
					else throw new MemberAccessException(expression, target, token, $"Static events cannot be access through instances of a type. Directly use '{type.Name}.{lexer.GetTokenSpan(token).ToString()}' instead to access the static event.");
				}

				var convertedArguments = ConvertArguments(method.GetParameters(), arguments);

				if (convertedArguments != null)
				{
					target.expression = Expression.Call(target.expression, method, convertedArguments);
					return;
				}
			}

			else if (memberInfo.MemberType is MemberTypes.Method)
			{
				MethodInfo methodInfo = (MethodInfo)memberInfo;
				if (staticContext)
				{
					if (!methodInfo.IsStatic)
						throw new MemberAccessException(expression, target, token, $"Member methods cannot be access through static context. An instance of the object must be used to call '{type.Name}.{lexer.GetTokenSpan(token).ToString()}'.");
				}
				else if (methodInfo.IsStatic)
				{
					if (target.expression == hostParameter)
						target.expression = null;
					else throw new MemberAccessException(expression, target, token, $"Static methods cannot be access through instances of a type. Directly use '{type.Name}.{lexer.GetTokenSpan(token).ToString()}' instead to access the static method.");
				}

				Expression[]? convertedArguments;
				MethodInfo? method;

				if (members.Length == 1)
				{
					convertedArguments = ConvertArguments(methodInfo.GetParameters(), arguments);
					method = methodInfo;
				}
				else {

					int index = SetupBestParameterMatch(target, members, arguments, out convertedArguments);

					if (index != -1)
						method = (MethodInfo)members[index];
					else method = null;
				}

				if (convertedArguments != null && method != null)
				{
					target.expression = Expression.Call(target.expression, method, convertedArguments);
					return;
				}
			}

			throw new MemberAccessException(expression, target, token, arguments, members);
		}

		private void ParseIndexer(ref Term term, in Token token)
		{
			Expression target = ExpectTerm(term);

			using var arguments = ParseArguments(TokenType.BracketClose);

			// Array access is special:
			if (target.Type.IsArray && arguments.Count == target.Type.GetArrayRank())
			{
				var buffer = compiler.ConverBuffer(arguments.Count);
				for (int i = 0; i < arguments.Count; i++)
				{
					if (ExpectTerm(arguments[i]).Type != typeof(int))
					try {
						buffer[i] = Convert(arguments[i], typeof(int));
					}
					catch (ArgumentException e)
					{
						throw new SymanticException(expression, term, $"Array indexers require integer parameter(s). Parameter {i} with type {ExpectTerm(arguments[i]).Type} could not be converted to an integer.", e);
					}
					else buffer[i] = ExpectTerm(arguments[i]);
				}

				term.expression = Expression.ArrayAccess(target, buffer);
				return;
			}

			PropertyInfo[] properties = target.Type.GetProperties(_flags);
			int index = SetupBestParameterMatch(term, properties, arguments, out var convertedArguments);

			if (index == -2)
				throw new MemberAccessException(expression, term, token, $"No indexer properties ('Item' properties) found on type '{target.Type}'. Cannot use indexer operator '[]' on type that does not have an indexer property. If this is an array, make sure the Rank matches the number of arguments.");

			else if (index == -1)
				throw new MemberAccessException(expression, term, token, arguments, properties);

			term.expression = Expression.Property(target, properties[index], convertedArguments);
		}

		private void NullConditional(ref Term term, in Token token)
		{
			Expression expression = ExpectTerm(term);

			Type type = expression.Type;
			Expression hasValue;
			if (type.IsValueType)
			{
				try {
					hasValue = Expression.Property(expression, "HasValue");
					term.expression = Expression.Property(expression, "Value");
				}
				catch {
					throw new OperationException(this.expression, token, term, $"Cannot use a null conditionl a Non-Nullable value type: '{type.Name}'.");
				}
			}
			else {
				Term nullTerm = new Term(Expression.Constant(null, type), token.start, token.length);
				hasValue = MakeComparison(ExpressionType.NotEqual, token, term, nullTerm, "op_Inequality");
			}

			FinishOutTerm(ref term);
			Expression evaluatesTo = ExpectExpression(term);

			if (evaluatesTo.Type == typeof(void))
			{
				term.expression = Expression.IfThen(hasValue, evaluatesTo);
				return;
			}

			if (evaluatesTo.Type.IsValueType)
			{
				if (!evaluatesTo.Type.IsGenericType || evaluatesTo.Type.GetGenericTypeDefinition() != typeof(Nullable<>))
				{
					try
					{
						evaluatesTo = Expression.Convert(evaluatesTo, typeof(Nullable<>).MakeGenericType(evaluatesTo.Type));
					}
					catch
					{
						throw new InternalException(this.expression, term.start, term.length, $"Cannot convert expression of type '{evaluatesTo.Type}' to Nullable type.");
					}
				}
			}

			term.expression = Expression.Condition(
				hasValue,
				evaluatesTo,
				Expression.Constant(null, evaluatesTo.Type)
			);
		}


		private StackSegment<Term> ParseArguments(TokenType closeToken)
		{
			StackSegment<Term> args = compiler.CreateArgumentSegment();

			if (lexer.AcceptIf(closeToken))
				return args;

			while (true)
			{
				args.Push(ParseExpression());

				if (lexer.AcceptIf(closeToken))
					break;
				lexer.Expected(TokenType.Comma, $"Arguments must be separated by a comma ','. The last argument should be followed by a {closeToken}.");
			}

			return args;
		}

		private Term CheckForGenerics(Type type, in Token context)
		{
			// There may be a generic type argument list.
			if (lexer.AcceptIf(TokenType.LessThan))
			{
				if (!type.IsGenericType)
					throw new ParserException(expression, context, "Cannot use generic type arguments on a non-generic type.");

				int count = 0;
				while (lexer.AcceptIf(TokenType.Comma))
					count++;

				if (count == 0) // keep as a generic
				{
					Type[] types = ParseTypeArguments(TokenType.GreaterThan);

					if (types.Length != 0)
						try {
							type = type.MakeGenericType(types);
						}
						catch (System.Exception e)
						{
							int start = context.start;
							int length = lexer.Peek().start - start;
							throw new ParserException(expression, start, length, "Failed to create generic type from type arguments. Expected " + string.Join(", ", type.GetGenericArguments().Select(t => t.FullName ?? t.Name)) + " but got " + string.Join(", ", types.Select(t => t.FullName ?? t.Name)), e);
						}
				}
				else lexer.Expected(TokenType.GreaterThan, "Generic type arguments must be separated by a comma ',' and end with a greater than '>'.");
			}
			else if (type.IsGenericType)
				throw new ParserException(expression, context, "Generic type arguments are required for type '" + type + "'.");

			return new Term(type, context.start, lexer.Peek().start - context.start);
		}

		private Type[] ParseTypeArguments(TokenType closeToken)
		{
			if (lexer.PeekIs(closeToken))
				return Array.Empty<Type>();

			List<Type> arguments = new List<Type>();

			while (true)
			{
				arguments.Add(ExpectType(ParseExpression()));

				if (lexer.AcceptIf(closeToken))
					break;
				lexer.Expected(TokenType.Comma, $"Arguments must be separated by a comma ','. The last argument should be followed by a {closeToken}.");
			}

			return arguments.ToArray();
		}

		private static int MatchPathV2(Lexer lexer, Span<Token> pathParts, Type type, bool includeGeneric)
		{
			if (pathParts.Length == 0)
				throw new ArgumentException("Path must contain at least one part.", nameof(pathParts));

			string name = type.Name;
			ReadOnlySpan<char> nameSpan = name.AsSpan();

		

			int nameIndex = pathParts.Length - 1;

			// Remove generic ending numbers.
			int endingIterator = nameSpan.Length - 1;
			while (nameSpan[endingIterator] >= '0' && nameSpan[endingIterator] <= '9')
				endingIterator--;
		
			// IFF the type ends with `XXX, it is a generic type.
			if (nameSpan[endingIterator] == '`')
			{
				if (!includeGeneric)
					return 0;

				nameSpan = nameSpan.Slice(0, endingIterator);

				// Because a generic type MUST be preceded by <types..>, we know the all parts must be apart of the type name.
				Token part = pathParts[nameIndex];
				if (nameSpan.Length != part.length)
					return 0;

				if (!lexer.GetTokenSpan(part).Equals(nameSpan, StringComparison.Ordinal))
					return 0;
			}
			else
				// See if the name matches any of the part (starting from the end)
				for (; nameIndex >= 0; nameIndex--)
				{
					Token part = pathParts[nameIndex];

					if (nameSpan.Length != part.length)
						continue;

					if (lexer.GetTokenSpan(part).Equals(nameSpan, StringComparison.Ordinal))
						break;
				}

			if (nameIndex == -1)
				return 0;

			nameIndex--;

			if (nameIndex == -1)
				return 1; // The type name matched the first part (the type name

			// Now make sure that the full name matches the rest of the path
			string? space = type.Namespace;
			if (space == null)
				return 0;

			int result = nameIndex;

			ReadOnlySpan<char> spaceSpan = space.AsSpan();
			int spaceStart = spaceSpan.Length;
			for (; nameIndex >= 0; nameIndex--)
			{
				Token part = pathParts[nameIndex];
				spaceStart -= part.length;

				if (spaceStart == 0)
				{
					if (nameIndex != 0)
						return 0;
				}
				else if (spaceSpan[spaceStart - 1] != '.')
					return 0;

				var spacePart = spaceSpan.Slice(spaceStart, part.length);
				var pathPart = lexer.GetTokenSpan(part);
				if (!spacePart.Equals(pathPart, StringComparison.Ordinal))
					return 0;
			}

			return result + 1;
		}

		private static Expression NumberExpression(in ReadOnlySpan<char> _span)
		{
			Span<char> span = stackalloc char[_span.Length];

			// Remove all underscores
			int i = 0;
			for (int j = 0; j < _span.Length; j++)
			{
				char c = _span[j];
				if (c != '_')
					span[i++] = c;
			}
			span = span.Slice(0, i);

			// Check for 0x and 0b. Must be Hex or Binary
			if (span.Length >= 3 && span[0] == '0')
			{
				NumberStyles numberStyle = NumberStyles.None;
				char c = span[1];
				if (c == 'x' || c == 'X')
				{
					span = span.Slice(2);
					numberStyle = NumberStyles.HexNumber;
				}
				else if (c == 'b' || c == 'B')
				{
					span = span.Slice(2);
					numberStyle = (NumberStyles)1024; // Binary. Throws an exception if not supported
				}

				if (numberStyle != NumberStyles.None)
				{
					switch (span[span.Length - 1])
					{
						case 'l':
						case 'L':
							if (span[span.Length - 2] == 'u' || span[span.Length - 2] == 'U')
								return Expression.Constant(ulong.Parse(span[..^2], numberStyle));
							return Expression.Constant(long.Parse(span[..^1], numberStyle));

						case 'u':
						case 'U':
							return Expression.Constant(uint.Parse(span[..^1], numberStyle));

						default:
							return Expression.Constant(int.Parse(span, numberStyle));
					}
				}
			}

			switch (span[span.Length - 1])
			{
				case 'f':
				case 'F':
					return Expression.Constant(float.Parse(span[..^1]));
				case 'd':
				case 'D':
					return Expression.Constant(double.Parse(span[..^1]));
				case 'm':
				case 'M':
					return Expression.Constant(decimal.Parse(span[..^1]));
				case 'l':
				case 'L':
					{
						if (span[span.Length - 2] == 'u' || span[span.Length - 2] == 'U')
							return Expression.Constant(ulong.Parse(span[..^2]));
						return Expression.Constant(long.Parse(span[..^1]));
					}
				case 'u':
				case 'U':
					return Expression.Constant(uint.Parse(span[..^1]));

				default:
					// If contains a dot, it's a double
					if (span.IndexOf('.') != -1)
						return Expression.Constant(double.Parse(span));
					return Expression.Constant(int.Parse(span));
			}
		}

		#endregion Private Parsing
	}
}
#nullable restore