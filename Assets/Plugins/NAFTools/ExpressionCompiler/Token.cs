namespace NAF.ExpressionCompiler
{
	internal interface IStringSpan
	{
		int start { get; }
		int length { get; }
	}

	internal struct Token : IStringSpan
	{
		public TokenType type;
		public int start { get; set; }
		public int length { get; set; }

		public Token(TokenType type, int start, int length = 1)
		{
			this.type = type;
			this.start = start;
			this.length = length;
		}

		public override string ToString()
		{
			return $"[{start}, {length}, {type}]";
		}

		public static IStringSpan Span(int start, int length = 1)
		{
			return new Token(TokenType.Invalid, start, length);
		}
	}
}