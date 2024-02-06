#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;

	public class Compiler
	{
		private Type[] _referenceTypes;
		public Type[] ReferenceTypes => _referenceTypes;

		public Compiler(Type[]? staticSearchTypes = null)
		{
			_referenceTypes = staticSearchTypes ?? ReflectionUtility.AllDeclaringTypes;
		}

		#region Internal Helpers For Parser

		/// <summary>
		/// Expressions have multiple terms, separated by operators. This stack is used to store the terms as they are parsed, before the order of operations is determined.
		/// </summary>
		private SegmentableStack<Term> _termStack = new SegmentableStack<Term>(16);

		/// <summary>
		/// Expressions have multiple terms, separated by operators. This stack is used to store the operators as they are parsed, before the order of operations is determined.
		/// </summary>
		private SegmentableStack<Token> _operatorStack = new SegmentableStack<Token>(16);

		/// <summary>
		/// Arguments (for generic types, methods, accessors..) must be parsed before due to overloading. This stack is used to store the arguments as they are parsed, before overload resolution is determined.
		/// </summary>
		private SegmentableStack<Term> _argumentStack = new SegmentableStack<Term>(16);

		/// <summary>
		/// When determining the best overload, the parameter infos are used several times to optimize best matches. This buffer stores the parameter infos for each overload.
		/// </summary>
		private ParameterInfo[][] _parameterBuffer = new ParameterInfo[8][];
		/// <summary>
		/// Depending on the call, the arguments may need to be converted to the parameter types. This buffer stores the converted arguments to avoid overriding the original arguments in the case of a conversion failure. In addition, all System.Linq.Expression methods use a temporary array parameter to set up classes. This contains temporary arrays that do not need to be allocated every time to avoid garbage collection. Each index matches a temporary array of the same size (as spans are not supported by Linq).
		/// </summary>
		private List<Expression[]?> _convertBuffer = new List<Expression[]?>(4);

		/// <summary> Creates a disposable segment to be used as a stack for terms. Only the latest segment not disposed is valid for modification. See <see cref="StackSegment{T}"/> for more information. </summary>
		internal StackSegment<Term> CreateTermSegment() => _termStack.NewSpan();
		/// <summary> Creates a disposable segment to be used as a stack for operators. Only the latest segment not disposed is valid for modification. See <see cref="StackSegment{T}"/> for more information. </summary>
		internal StackSegment<Token> CreateOperatorSegement() => _operatorStack.NewSpan();
		/// <summary> Creates a disposable segment to be used as a stack for arguments. Only the latest segment not disposed is valid for modification. See <see cref="StackSegment{T}"/> for more information. </summary>
		internal StackSegment<Term> CreateArgumentSegment() => _argumentStack.NewSpan();

		
		/// <summary>
		/// Returns an Expression buffer of the given size. Should only be used temporarily (not preserved between parsing calls).
		/// </summary>
		/// <param name="size">The size of the buffer.</param>
		/// <returns>An Expression buffer of the given size.</returns>
		internal Expression[] ConverBuffer(int size)
		{
			while (_convertBuffer.Count <= size)
				_convertBuffer.Add(null);

			return _convertBuffer[size] ??= new Expression[size];
		}

		/// <summary>
		/// Returns a ParameterInfo buffer of the given size. Should only be used temporarily (not preserved between parsing calls).
		/// </summary>
		/// <param name="size">The size of the buffer.</param>
		/// <returns>A ParameterInfo buffer of the given size.</returns>
		internal Span<ParameterInfo[]> ParameterBuffer(int size)
		{
			while (_parameterBuffer.Length < size)
				_parameterBuffer = new ParameterInfo[_parameterBuffer.Length * 2][];

			return _parameterBuffer.AsSpan(0, size);
		}

		#endregion

		#region Raw Compile

		/// <summary>Parses a C# expression into an Expression object. </summary>
		/// <param name="expression">The expression to be parsed as a C# expression. The code will complie as though written from within the class of the first type parameter, meaning members do not need to be prefix by the parameter number. All parameters are referenced by {number}, and parameter {0} can also be referenced by the 'this' keyword. </param>
		/// <param name="parameters">The parameters of the delegate. The number of parameters must match the number of parameters in the expression. </param>
		/// <returns>An Expression object representing the expression.</returns>
		public Expression Parse(ReadOnlySpan<char> expression, params Expression[] parameters)
		{
			return Parser.SingleExpression(this, expression, parameters);
		}

		/// <summary> Compiles a C# expression into a delegate. </summary>
		/// <param name="expression">The expression to be parsed as a C# expression. The code will complie as though written from within the class of the first type parameter, meaning members do not need to be prefix by the parameter number. All parameters are referenced by {number}, and parameter {0} can also be referenced by the 'this' keyword. </param>
		/// <param name="parameters">The types of the parameters of the delegate. The number of parameters must match the number of parameters in the expression. </param>
		/// <returns>A delegate representing the expression.</returns>
		public Delegate Compile(ReadOnlySpan<char> expression, params Type[] parameters)
		{
			ParameterExpression[] parameterExpressions = new ParameterExpression[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
				parameterExpressions[i] = Expression.Parameter(parameters[i]);

			var body = Parser.SingleExpression(this, expression, parameterExpressions);
			return Expression.Lambda(body, parameterExpressions).Compile();
		}

		#endregion

		private Expression ConvertIfNecessary(Expression expression, Type? type)
		{
			if (type == null || expression.Type == type)
				return expression;
			else return Expression.Convert(expression, type);
		}

		#region Anonomus Compile

		/// <summary>
		/// Compiles an expression into a function with 'object' type parameters and return types. Calling the resulting function with the incorrect parameter types will still throw an exception. Useful for compiling expressions of runtime types without needing to cast the to the correct 'Func' type.
		/// </summary>
		/// <param name="expression">The expression to be parsed as a C# expression. The code will complie as though written from within the class of the first type parameter, meaning members do not need to be prefix by the parameter number. All parameters are referenced by {number}, and parameter {0} can also be referenced by the 'this' keyword. </param>
		/// <returns>An anonomus function representing the expression. The parameters and return of the function are immediatly casted from/to 'object' before/after evaluations. </returns>
		public Func<object> CompileAnonomus(string expression)
		{
			var body = Parser.SingleExpression(this, expression);

			body = ConvertIfNecessary(body, typeof(object));
			return Expression.Lambda<Func<object>>(body).Compile();
		}

		/// <inheritdoc cref="CompileAnonomus(string)"/>
		public Func<object?, object> CompileAnonomus(ReadOnlySpan<char> expression, Type t0)
		{
			ParameterExpression parameterExpression = Expression.Parameter(typeof(object), "p0");

			var body = Parser.SingleExpression(this, expression,
				ConvertIfNecessary(parameterExpression, t0)
			);

			body = ConvertIfNecessary(body, typeof(object));
			return Expression.Lambda<Func<object?, object>>(body, parameterExpression).Compile();
		}

		/// <inheritdoc cref="CompileAnonomus(string)"/>
		public Func<object?, object?, object> CompileAnonomus(ReadOnlySpan<char> expression, Type t0, Type t1)
		{
			ParameterExpression parameterExpression0 = Expression.Parameter(typeof(object), "p0");
			ParameterExpression parameterExpression1 = Expression.Parameter(typeof(object), "p1");

			var body = Parser.SingleExpression(this, expression,
				ConvertIfNecessary(parameterExpression0, t0),
				ConvertIfNecessary(parameterExpression1, t1)
			);

			body = ConvertIfNecessary(body, typeof(object));
			return Expression.Lambda<Func<object?, object?, object>>(body, parameterExpression0, parameterExpression1).Compile();
		}

		/// <inheritdoc cref="CompileAnonomus(string)"/>
		public Func<object?, object?, object?, object> CompileAnonomus(ReadOnlySpan<char> expression, Type t0, Type t1, Type t2)
		{
			ParameterExpression parameterExpression0 = Expression.Parameter(typeof(object), "p0");
			ParameterExpression parameterExpression1 = Expression.Parameter(typeof(object), "p1");
			ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object), "p2");

			var body = Parser.SingleExpression(this, expression,
				ConvertIfNecessary(parameterExpression0, t0),
				ConvertIfNecessary(parameterExpression1, t1),
				ConvertIfNecessary(parameterExpression2, t2)
			);

			body = ConvertIfNecessary(body, typeof(object));
			return Expression.Lambda<Func<object?, object?, object?, object>>(body, parameterExpression0, parameterExpression1, parameterExpression2).Compile();
		}

		/// <inheritdoc cref="CompileAnonomus(string)"/>
		public Func<object?, object?, object?, object?, object> CompileAnonomus(ReadOnlySpan<char> expression, Type t0, Type t1, Type t2, Type t3)
		{
			ParameterExpression parameterExpression0 = Expression.Parameter(typeof(object), "p0");
			ParameterExpression parameterExpression1 = Expression.Parameter(typeof(object), "p1");
			ParameterExpression parameterExpression2 = Expression.Parameter(typeof(object), "p2");
			ParameterExpression parameterExpression3 = Expression.Parameter(typeof(object), "p3");

			var body = Parser.SingleExpression(this, expression,
				ConvertIfNecessary(parameterExpression0, t0),
				ConvertIfNecessary(parameterExpression1, t1),
				ConvertIfNecessary(parameterExpression2, t2),
				ConvertIfNecessary(parameterExpression3, t3)
			);

			body = ConvertIfNecessary(body, typeof(object));
			return Expression.Lambda<Func<object?, object?, object?, object?, object>>(body, parameterExpression0, parameterExpression1, parameterExpression2, parameterExpression3).Compile();
		}

		#endregion

		#region Dynamic Compile

		/// <summary>
		/// Compiles an expression into a function with 'object[]' type parameter and 'object' return type. Calling the resulting function with the incorrect parameter types will still throw an exception. Useful for compiling expressions of runtime types without needing to cast the to the correct 'Func' type.
		/// </summary>
		/// <param name="expression">The expression to be parsed as a C# expression. The code will complie as though written from within the class of the first type parameter, meaning members do not need to be prefix by the parameter number. All parameters are referenced by {number}, and parameter {0} can also be referenced by the 'this' keyword. </param>
		/// <returns>An anonomus function representing the expression. The parameters and return of the function are immediatly casted from/to 'object' before/after evaluating the compiled expression. </returns>
		public Func<object?[], object> CompileDynamic(ReadOnlySpan<char> expression, params Type[] parameters)
		{
			if (parameters == null)
				throw new ArgumentNullException(nameof(parameters));

			ParameterExpression objectParam = Expression.Parameter(typeof(object[]), "parameters");

			Expression[] castParameters = new Expression[parameters.Length];
			for (int i = 0; i < parameters.Length; i++)
				castParameters[i] = ConvertIfNecessary(Expression.ArrayAccess(objectParam, Expression.Constant(i)), parameters[i]);
			var body = Parser.SingleExpression(this, expression, castParameters);

			body = ConvertIfNecessary(body, typeof(object));

			return Expression.Lambda<Func<object?[], object>>(body, objectParam).Compile();
		}

		#endregion
	}
}
#nullable restore