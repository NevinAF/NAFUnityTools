
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Diagnostics;
	using System.Linq.Expressions;
	using System.Reflection;

	public ref partial struct Parser
	{

		internal Term MakeBinary(in Token op, in Term left, in Term right)
		{
			RequiresCanRead(left);
			RequiresCanRead(right);

			Expression expr = op.type switch
			{
				// Arithmetic (no assignment)
				TokenType.Plus => MakeArithmetic(ExpressionType.Add, op, left, right, "op_Addition", false),
				//  => MakeArithmetic(ExpressionType.AddChecked, op, left, right, "op_Addition", false),
				TokenType.Minus => MakeArithmetic(ExpressionType.Subtract, op, left, right, "op_Subtraction", false),
				//  => MakeArithmetic(ExpressionType.SubtractChecked, op, left, right, "op_Subtraction", false),
				TokenType.Multiplication => MakeArithmetic(ExpressionType.Multiply, op, left, right, "op_Multiply", false),
				//  => MakeArithmetic(ExpressionType.MultiplyChecked, op, left, right, "op_Multiply", false),
				TokenType.Division => MakeArithmetic(ExpressionType.Divide, op, left, right, "op_Division", false),
				TokenType.Modulus => MakeArithmetic(ExpressionType.Modulo, op, left, right, "op_Modulus", false),
				TokenType.BitwiseAnd => MakeArithmetic(ExpressionType.And, op, left, right, "op_BitwiseAnd", false),
				TokenType.BitwiseOr => MakeArithmetic(ExpressionType.Or, op, left, right, "op_BitwiseOr", false),
				TokenType.BitwiseXor => MakeArithmetic(ExpressionType.ExclusiveOr, op, left, right, "op_ExclusiveOr", false),
				TokenType.LeftShift => MakeArithmetic(ExpressionType.LeftShift, op, left, right, "op_LeftShift", false),
				TokenType.RightShift => MakeArithmetic(ExpressionType.RightShift, op, left, right, "op_RightShift", false),

				// Arithmetic (assignment)
				TokenType.AdditionAssignment => MakeArithmetic(ExpressionType.AddAssign, op, left, right, "op_Addition", true),
				//  => MakeArithmetic(ExpressionType.AddAssignChecked, op, left, right, "op_Addition", true),
				TokenType.SubtractionAssignment => MakeArithmetic(ExpressionType.SubtractAssign, op, left, right, "op_Subtraction", true),
				//  => MakeArithmetic(ExpressionType.SubtractAssignChecked, op, left, right, "op_Subtraction", true),
				TokenType.MultiplicationAssignment => MakeArithmetic(ExpressionType.MultiplyAssign, op, left, right, "op_Multiply", true),
				//  => MakeArithmetic(ExpressionType.MultiplyAssignChecked, op, left, right, "op_Multiply", true),
				TokenType.DivisionAssignment => MakeArithmetic(ExpressionType.DivideAssign, op, left, right, "op_Division", true),
				TokenType.ModulusAssignment => MakeArithmetic(ExpressionType.ModuloAssign, op, left, right, "op_Modulus", true),
				TokenType.BitwiseAndAssignment => MakeArithmetic(ExpressionType.AndAssign, op, left, right, "op_BitwiseAnd", true),
				TokenType.BitwiseOrAssignment => MakeArithmetic(ExpressionType.OrAssign, op, left, right, "op_BitwiseOr", true),
				TokenType.BitwiseXorAssignment => MakeArithmetic(ExpressionType.ExclusiveOrAssign, op, left, right, "op_ExclusiveOr", true),
				TokenType.LeftShiftAssignment => MakeArithmetic(ExpressionType.LeftShiftAssign, op, left, right, "op_LeftShift", true),
				TokenType.RightShiftAssignment => MakeArithmetic(ExpressionType.RightShiftAssign, op, left, right, "op_RightShift", true),

				// Comparison
				TokenType.Equality => MakeComparison(ExpressionType.Equal, op, left, right, "op_Equality"),
				TokenType.Inequality => MakeComparison(ExpressionType.NotEqual, op, left, right, "op_Inequality"),
				TokenType.LessThan => MakeComparison(ExpressionType.LessThan, op, left, right, "op_LessThan"),
				TokenType.LessThanOrEqual => MakeComparison(ExpressionType.LessThanOrEqual, op, left, right, "op_LessThanOrEqual"),
				TokenType.GreaterThan => MakeComparison(ExpressionType.GreaterThan, op, left, right, "op_GreaterThan"),
				TokenType.GreaterThanOrEqual => MakeComparison(ExpressionType.GreaterThanOrEqual, op, left, right, "op_GreaterThanOrEqual"),

				// Boolean
				TokenType.ConditionalAnd => MakeBoolean(ExpressionType.AndAlso, op, left, right),
				TokenType.ConditionalOr => MakeBoolean(ExpressionType.OrElse, op, left, right),

				// Coalesce
				TokenType.NullCoalescing => Coalesce(op, left, right),
				TokenType.NullCoalescingAssignment => CoalesceAssign(op, left, right),

				// Assignment
				TokenType.Assignment => Assign(left, right),

				// Default
				_ => throw new OperationException(expression, op, left, right, $"The binary operator '{op.type}' is not implemented."),
			};

			return new Term(expr, left.start, right.start + right.length - left.start);
		}

		private Term MakeInequality(in Token op, in Term left, in Term right)
		{
			Expression expr = MakeComparison(ExpressionType.NotEqual, op, left, right, "op_Inequality");
			return new Term(expr, left.start, right.start + right.length - left.start);
		}

		private BinaryExpression GetUserDefinedBinaryOperator(ExpressionType binaryType, string name, in Token op, in Term left, in Term right)
		{
			Expression exprL = ExpectTerm(left);
			Expression exprR = ExpectTerm(right);

			Type nnLeftType = TypeUtils.GetNonNullableType(exprL.Type);
			Type nnRightType = TypeUtils.GetNonNullableType(exprR.Type);

			var operatorResult =
				TypeUtils.FindBinaryOperator(nnLeftType, exprL.Type, exprR.Type, name) ??
				TypeUtils.FindBinaryOperator(nnRightType, exprL.Type, exprR.Type, name) ??
				throw new OperationException(expression, op, left, right, "No operator '" + binaryType + "' is defined between types '" + exprL.Type.Name + "' and '" + exprR.Type.Name + "'.");

			if (operatorResult.LeftCoercion != null)
				exprL = Convert(left, operatorResult.LeftCoercion.ReturnType, operatorResult.LeftCoercion);

			if (operatorResult.RightCoercion != null)
				exprR = Convert(right, operatorResult.RightCoercion.ReturnType, operatorResult.RightCoercion);

			return ExpressionConstructors.MethodBinaryConstructor(binaryType, exprL, exprR, operatorResult.Operator.ReturnType, operatorResult.Operator);
		}

		private bool IsNumeric(in Term left, in Term right, ref Expression exprL, ref Expression exprR)
		{
			int leftNumaric = exprL.Type.NumaricTypePrecedence();
			int rightNumaric = exprR.Type.NumaricTypePrecedence();
			if (leftNumaric > 0 && rightNumaric > 0)
			{
				if (leftNumaric > rightNumaric)
				{
					exprR = Convert(right, exprL.Type);
					return leftNumaric != (int)TypeCode.Decimal;
				}
				else
				{
					exprL = Convert(left, exprR.Type);
					return rightNumaric != (int)TypeCode.Decimal;
				}
			}

			return false;
		}

		private BinaryExpression Assign(in Term left, in Term right)
		{
			Expression exprL = ExpectTerm(left);
			Expression exprR = Convert(right, exprL.Type);

			return ExpressionConstructors.AssignBinaryConstructor(exprL, exprR);
		}

		private BinaryExpression CoalesceAssign(in Token op, in Term left, Term right)
		{
			right.expression = Coalesce(op, left, right);

			Expression exprL = ExpectTerm(left);
			Expression exprR = Convert(right, exprL.Type);

			return ExpressionConstructors.AssignBinaryConstructor(exprL, exprR);
			
		}

		private static readonly MethodInfo ObjectConcat = typeof(string).GetMethod("Concat", new Type[] { typeof(object), typeof(object) })!;

		private BinaryExpression MakeArithmetic(ExpressionType type, in Token op, in Term left, in Term right, string methodName, bool isAssignment)
		{
			if (isAssignment)
				RequiresCanWrite(left);

			Expression exprL = ExpectTerm(left);
			Expression exprR = ExpectTerm(right);

			if (IsNumeric(left, right, ref exprL, ref exprR))
				return ExpressionConstructors.SimpleBinaryConstructor(type, exprL, exprR, exprL.Type);

			if (type == ExpressionType.Add || type == ExpressionType.AddAssign)
			{
				if (exprL.Type == typeof(string) || exprR.Type == typeof(string))
				{
					if (exprL.Type.IsValueType)
						exprL = Expression.Convert(exprL, typeof(object));

					if (exprR.Type.IsValueType)
						exprR = Expression.Convert(exprR, typeof(object));

					return ExpressionConstructors.MethodBinaryConstructor(type, exprL, exprR, typeof(string), ObjectConcat);
				}
			}

			return GetUserDefinedBinaryOperator(type, methodName, op, left, right);
		}

		private BinaryExpression MakeComparison(ExpressionType type, in Token op, in Term left, in Term right, string methodName)
		{
			Expression exprL = ExpectTerm(left);
			Expression exprR = ExpectTerm(right);

			if (IsNumeric(left, right, ref exprL, ref exprR))
				return ExpressionConstructors.LogicalBinaryConstructor(type, exprL, exprR);

			Type nnLeftType = TypeUtils.GetNonNullableType(exprL.Type);
			Type nnRightType = TypeUtils.GetNonNullableType(exprR.Type);

			if (TypeUtils.IsPrimitive(nnLeftType) && TypeUtils.IsPrimitive(nnRightType))
			{
				if (TypeUtils.IsNullableType(exprL.Type) || TypeUtils.IsNullableType(exprR.Type))
				{
					return ExpressionConstructors.SimpleBinaryConstructor(ExpressionType.Coalesce,
						ExpressionConstructors.SimpleBinaryConstructor(type, exprL, exprR, typeof(bool?)),
						Expression.Constant(false),
						typeof(bool)
					);
				}
				else return ExpressionConstructors.LogicalBinaryConstructor(type, exprL, exprR);
			}

			return GetUserDefinedBinaryOperator(type, methodName, op, left, right);
		}

		private BinaryExpression MakeBoolean(ExpressionType type, in Token op, in Term left, in Term right)
		{
			Expression exprL = ExpectTerm(left);
			Expression exprR = ExpectTerm(right);

			if (type != ExpressionType.AndAlso && type != ExpressionType.OrElse)
				throw new ArgumentException($"Invalid boolean type {type}");

			Type nnLeftType = TypeUtils.GetNonNullableType(exprL.Type);
			Type nnRightType = TypeUtils.GetNonNullableType(exprR.Type);

			if (Type.GetTypeCode(nnLeftType) != TypeCode.Boolean)
				throw new InvalidOperationException($"Cannot perform boolean operation on non-boolean type {nnLeftType}");

			if (Type.GetTypeCode(nnRightType) != TypeCode.Boolean)
				throw new InvalidOperationException($"Cannot perform boolean operation on non-boolean type {nnRightType}");

			if (TypeUtils.IsNullableType(exprL.Type) || TypeUtils.IsNullableType(exprR.Type))
				return ExpressionConstructors.SimpleBinaryConstructor(ExpressionType.Coalesce,
					ExpressionConstructors.SimpleBinaryConstructor(type, exprL, exprR, typeof(bool?)),
					Expression.Constant(false),
					typeof(bool)
				);
			else return ExpressionConstructors.LogicalBinaryConstructor(type, exprL, exprR);
		}

		private BinaryExpression Coalesce(in Token op, in Term left, in Term right)
		{
			Expression exprL = ExpectTerm(left);
			Expression exprR = ExpectTerm(right);

			Type resultType;

			if (exprL.Type.IsValueType)
			{
				if (!TypeUtils.IsNullableType(exprL.Type))
					throw new InvalidOperationException($"Cannot perform coalesce operation on non-nullable type {exprL.Type}");

				Type nnLeftType = exprL.Type.GetGenericArguments()[0];

				if (TypeUtils.IsNullableType(exprR.Type))
				{
					resultType = exprL.Type;
				}
				else resultType = nnLeftType;
			}
			else resultType = exprL.Type;

			exprR = TryConvert(right, resultType) ??
				throw new InvalidOperationException($"Cannot perform coalesce operation on non-equivalent types {exprL.Type} and {exprR.Type}. Type {exprR.Type} is not assignable to {(exprL.Type != resultType ? exprL.Type + " nor " + resultType : resultType)}.");

			return ExpressionConstructors.SimpleBinaryConstructor(ExpressionType.Coalesce, exprL, exprR, resultType);
		}
	}
}