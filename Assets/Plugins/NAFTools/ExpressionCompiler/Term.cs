#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Linq.Expressions;

	internal struct Term : IStringSpan
	{
		internal Expression? expression;
		internal Type? staticTypeReference;
		public int start { get; set; }
		public int length { get; set; }

		internal readonly bool IsEmpty => expression == null && staticTypeReference == null;

		internal Term(Expression? expression, int start, int length)
		{
			this.expression = expression;
			this.staticTypeReference = null;
			this.start = start;
			this.length = length;
		}

		internal Term(Type? staticTypeReference, int start, int length)
		{
			this.expression = null;
			this.staticTypeReference = staticTypeReference;
			this.start = start;
			this.length = length;
		}

		internal Term(int start, int length)
		{
			this.expression = null;
			this.staticTypeReference = null;
			this.start = start;
			this.length = length;
		}

		public override string ToString()
		{
			return $"[{start}, {length}, {expression?.ToString() ?? staticTypeReference?.ToString() ?? "null"}]";
		}
	}
}
#nullable restore