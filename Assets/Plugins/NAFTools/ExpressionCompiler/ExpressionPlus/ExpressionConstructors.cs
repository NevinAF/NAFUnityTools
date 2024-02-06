using System;
using System.Linq.Expressions;
using System.Reflection;

#nullable enable

namespace NAF.ExpressionCompiler
{
	public static partial class ExpressionConstructors
	{
		public static readonly Func<ExpressionType, Expression, Type, MethodInfo?, UnaryExpression> UnaryConstructor;
		// public static readonly Func<Expression, Expression, BinaryExpression> BinaryExpressionConstructor;
		public static readonly Func<ExpressionType, Expression, Expression, BinaryExpression> LogicalBinaryConstructor;
		public static readonly Func<Expression, Expression, BinaryExpression> AssignBinaryConstructor;
		// public static readonly Func<Expression, Expression, LambdaExpression, BinaryExpression> CoalesceConversionBinaryConstructor;
		// public static readonly Func<ExpressionType, Expression, Expression, Type, MethodInfo, LambdaExpression, BinaryExpression> OpAssignMethodConversionBinaryConstructor;
		public static readonly Func<ExpressionType, Expression, Expression, Type, MethodInfo, BinaryExpression> MethodBinaryConstructor;
		public static readonly Func<ExpressionType, Expression, Expression, Type, BinaryExpression> SimpleBinaryConstructor;
		// FullConditionalExpression
		public static readonly Func<Expression, Expression, Expression, ConditionalExpression> FullConditionalConstructor;

		static ExpressionConstructors()
		{
			Assembly assembly = Assembly.GetAssembly(typeof(Expression))!;
			UnaryConstructor = (Func<ExpressionType, Expression, Type, MethodInfo?, UnaryExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.UnaryExpression")!,
					new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Type), typeof(MethodInfo) },
					typeof(Func<ExpressionType, Expression, Type, MethodInfo?, UnaryExpression>)
				);

			LogicalBinaryConstructor = (Func<ExpressionType, Expression, Expression, BinaryExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.LogicalBinaryExpression")!,
					new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression) },
					typeof(Func<ExpressionType, Expression, Expression, BinaryExpression>)
				);

			AssignBinaryConstructor = (Func<Expression, Expression, BinaryExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.AssignBinaryExpression")!,
					new Type[] { typeof(Expression), typeof(Expression) },
					typeof(Func<Expression, Expression, BinaryExpression>)
				);

			MethodBinaryConstructor = (Func<ExpressionType, Expression, Expression, Type, MethodInfo, BinaryExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.MethodBinaryExpression")!,
					new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression), typeof(Type), typeof(MethodInfo) },
					typeof(Func<ExpressionType, Expression, Expression, Type, MethodInfo, BinaryExpression>)
				);

			SimpleBinaryConstructor = (Func<ExpressionType, Expression, Expression, Type, BinaryExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.SimpleBinaryExpression")!,
					new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression), typeof(Type) },
					typeof(Func<ExpressionType, Expression, Expression, Type, BinaryExpression>)
				);

			FullConditionalConstructor = (Func<Expression, Expression, Expression, ConditionalExpression>)
				EmitUtils.CreateConstructor(
					assembly.GetType("System.Linq.Expressions.FullConditionalExpression")!,
					new Type[] { typeof(Expression), typeof(Expression), typeof(Expression) },
					typeof(Func<Expression, Expression, Expression, ConditionalExpression>)
				);
		}
	}
}