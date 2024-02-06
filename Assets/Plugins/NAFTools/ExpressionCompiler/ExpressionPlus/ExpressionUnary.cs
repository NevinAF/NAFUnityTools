using System;
using System.Diagnostics;
using System.Linq.Expressions;
using System.Reflection;
using System.Reflection.Emit;

#nullable enable

namespace NAF.ExpressionCompiler
{
	public ref partial struct Parser
	{
		internal Expression Convert(in Term term, Type type)
		{
			RequiresCanRead(term);
			Expression expr = ExpectTerm(term);

			if (TypeUtils.AreEquivalent(expr.Type, type))
				return expr;

			if (TypeUtils.IsAssignableUsing(expr.Type, type, out MethodInfo? method))
				return ExpressionConstructors.UnaryConstructor(ExpressionType.Convert, expr, type, method);

			throw new SymanticException(expression, term, "No coercion operator is defined from type '" + expr.Type.Name + "' to '" + type.Name + "'.");
		}

		internal Expression? TryConvert(in Term term, Type type)
		{
			RequiresCanRead(term);
			Expression expr = ExpectTerm(term);

			if (TypeUtils.AreEquivalent(expr.Type, type))
				return expr;

			if (TypeUtils.IsAssignableUsing(expr.Type, type, out MethodInfo? method))
				return ExpressionConstructors.UnaryConstructor(ExpressionType.Convert, expr, type, method);

			return null;
		}

		internal Expression Convert(in Term term, Type type, MethodInfo method)
		{
			RequiresCanRead(term);
			Expression expr = ExpectTerm(term);
			// TODO: Not null
			TypeUtils.ValidateType(type);

			if (TypeUtils.AreEquivalent(expr.Type, type))
				return expr;

			return ExpressionConstructors.UnaryConstructor(ExpressionType.Convert, expr, type, method);
		}

		private UnaryExpression GetUserDefinedUnaryOperator(ExpressionType unaryType, string name, in Term term, in Token op)
		{
			Expression expr = ExpectTerm(term);

			var unaryResult = TypeUtils.FindUnaryOperator(TypeUtils.GetNonNullableType(expr.Type), expr.Type, name) ??
				throw new OperationException(expression, op, term, "The unary operator '" + unaryType + "' is not defined for type '" + expr.Type.Name + "'");

			if (unaryResult.Coercion != null)
				expr = Expression.Convert(expr, unaryResult.Coercion.ReturnType, unaryResult.Coercion);

			return ExpressionConstructors.UnaryConstructor(unaryType, expr, unaryResult.Operator.ReturnType, unaryResult.Operator);
		}

		internal void MakeUnary(ref Term term, in Token op)
		{
			RequiresCanRead(term);
			Expression expr = ExpectTerm(term);

			string? methodName;
			ExpressionType type;

			switch (op.type)
			{
				case TokenType.Plus:
					if (Type.GetTypeCode(expr.Type) == TypeCode.Char)
					{
						term.expression = ExpressionConstructors.UnaryConstructor(ExpressionType.Convert, expr, typeof(int), null);
						goto setTermLength;
					}
					else {
						type = ExpressionType.UnaryPlus;
						methodName = TypeUtils.IsArithmetic(expr.Type) ? null : "op_UnaryPlus";
					}
					break;

				case TokenType.Minus:
					type = ExpressionType.Negate;
					if (!TypeUtils.IsArithmetic(expr.Type))
						methodName = "op_UnaryNegation";
					else if (TypeUtils.IsUnsignedInt(expr.Type))
						throw new InvalidOperationException("The unary '-' operator cannot be applied to an operand of the unsigned type '" + expr.Type.Name + "'");
					else methodName = null;
					break;

				case TokenType.LogicalNegation:
					type = ExpressionType.Not;
					methodName = TypeUtils.IsIntegerOrBool(expr.Type) ? null : "op_LogicalNot";
					break;

				case TokenType.Complement:
					type = ExpressionType.OnesComplement;
					methodName = TypeUtils.IsInteger(expr.Type) ? null : "op_OnesComplement";
					break;

				case TokenType.Increment:
					type = op.start > term.start ? ExpressionType.PostIncrementAssign : ExpressionType.PreIncrementAssign;
					methodName = TypeUtils.IsArithmetic(expr.Type) ? null : "op_Increment";
					break;

				case TokenType.Decrement:
					type = op.start > term.start ? ExpressionType.PostDecrementAssign : ExpressionType.PreDecrementAssign;
					methodName = TypeUtils.IsArithmetic(expr.Type) ? null : "op_Decrement";
					break;

				default:
					throw new InternalException(expression, op.start, op.length, $"The prefix unary operator '{op.type}' is not implemented.");
			}

			if (methodName != null)
				term.expression = GetUserDefinedUnaryOperator(type, methodName, term, op);
			else term.expression = ExpressionConstructors.UnaryConstructor(type, expr, expr.Type, null);

		setTermLength:
			if (op.start < term.start)
			{
				term.start = op.start;
				term.length = term.start + term.length - op.start;
			}
			else term.length = op.start + op.length - term.start;
		}
	}
}