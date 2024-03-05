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
		private Term Conditional(in Token question, in Token colon, in Term test, in Term ifTrue, in Term ifFalse)
		{
			RequiresCanRead(test);
			RequiresCanRead(ifTrue);
			RequiresCanRead(ifFalse);

			Expression exprTest = TryConvert(test, typeof(bool)) ??
				throw new InvalidOperationException("The 'test' expression of a ternary conditional statement must be of type 'bool'");
			Expression exprTrue = ExpectTerm(ifTrue);
			Expression exprFalse = ExpectTerm(ifFalse);

			bool trueIsNull = exprTrue is ConstantExpression trueConst && trueConst.Value is null;
			bool falseIsNull = exprFalse is ConstantExpression falseConst && falseConst.Value is null;

			if (trueIsNull && falseIsNull)
				return new Term(Expression.Constant(null, typeof(object)), test.start, ifFalse.start + ifFalse.length - test.start);

			if (trueIsNull)
			{
				exprTrue = Expression.Constant(null, exprFalse.Type);
			}
			else if (falseIsNull)
			{
				exprFalse = Expression.Constant(null, exprTrue.Type);
			}
			else
			{
				Expression? convert = TryConvert(ifTrue, exprFalse.Type);
		
				if (convert != null)
					exprTrue = convert;
				else
				{
					exprFalse = TryConvert(ifFalse, exprTrue.Type) ??
						throw new OperationException(expression, question, colon, test, ifTrue, ifFalse, "No coercion operator is defined between types '" + ExpectTerm(ifTrue).Type.Name + "' and '" + ExpectTerm(ifFalse).Type.Name + "'. The 'ifTrue' and 'ifFalse' expressions of a ternary conditional statement must be assinable to each others type (common types are not infered).");
				}
			}

			return new Term(ExpressionConstructors.FullConditionalConstructor(exprTest, exprTrue, exprFalse), test.start, ifFalse.start + ifFalse.length - test.start);
		}
	}
}