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
			BindingFlags nonPublic = BindingFlags.NonPublic | BindingFlags.Instance;

			ConstructorInfo unaryCI = typeof(UnaryExpression).GetConstructor(nonPublic, null, new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Type), typeof(MethodInfo) }, null)!;
			EmitUtils.BoxedMember(unaryCI, out UnaryConstructor);
			
			ConstructorInfo logicalBinaryCI = assembly.GetType("System.Linq.Expressions.LogicalBinaryExpression")!.GetConstructor(nonPublic, null, new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression) }, null)!;
			EmitUtils.BoxedMember(logicalBinaryCI, out LogicalBinaryConstructor);

			ConstructorInfo assignBinaryCI = assembly.GetType("System.Linq.Expressions.AssignBinaryExpression")!.GetConstructor(nonPublic, null, new Type[] { typeof(Expression), typeof(Expression) }, null)!;
			EmitUtils.BoxedMember(assignBinaryCI, out AssignBinaryConstructor);

			ConstructorInfo methodBinaryCI = assembly.GetType("System.Linq.Expressions.MethodBinaryExpression")!.GetConstructor(nonPublic, null, new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression), typeof(Type), typeof(MethodInfo) }, null)!;
			EmitUtils.BoxedMember(methodBinaryCI, out MethodBinaryConstructor);

			ConstructorInfo simpleBinaryCI = assembly.GetType("System.Linq.Expressions.SimpleBinaryExpression")!.GetConstructor(nonPublic, null, new Type[] { typeof(ExpressionType), typeof(Expression), typeof(Expression), typeof(Type) }, null)!;
			EmitUtils.BoxedMember(simpleBinaryCI, out SimpleBinaryConstructor);

			ConstructorInfo fullConditionalCI = assembly.GetType("System.Linq.Expressions.FullConditionalExpression")!.GetConstructor(nonPublic, null, new Type[] { typeof(Expression), typeof(Expression), typeof(Expression) }, null)!;
			EmitUtils.BoxedMember(fullConditionalCI, out FullConditionalConstructor);
		}
	}
}