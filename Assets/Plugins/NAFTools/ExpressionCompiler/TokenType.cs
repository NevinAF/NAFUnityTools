namespace NAF.ExpressionCompiler
{
	using System.Runtime.CompilerServices;

	internal enum TokenType
	{
		Invalid,

		/*
		 * Literals
		 */
		Number,
		Identifier,
		String,
		Character,
		True,
		False,
		Null,
		Base,
		This,
		/* *** */

		// Special Parenthesis Open
		ParenthesisOpen,
		BraceOpen,
		BracketOpen,

		/*
		 * Operators
		 */

		// Primary + Unary: Always applied immediately and thus always included in the term.
		// Note that tokens as lexical, and thus "plus" and "minus" have overlap with the additive operators.
		Typeof,
		New,
		MemberAccess,
		NullConditional,
		Increment,
		Decrement,
		Complement,
		LogicalNegation,

		// Range
		// SwitchWith

		// Multiplicative
		Multiplication,
		Division,
		Modulus,

		// Additive
		Plus,
		Minus,

		// Shift
		LeftShift,
		RightShift,
		UnsignedRightShift,

		// Relational
		LessThan,
		LessThanOrEqual,
		GreaterThan,
		GreaterThanOrEqual,

		// Equality
		Equality,
		Inequality,

		// LogicalAnd
		BitwiseAnd,
		// LogicalXor
		BitwiseXor,
		// LogicalOr
		BitwiseOr,
		// ConditionalAnd
		ConditionalAnd,
		// ConditionalOr
		ConditionalOr,
		// NullCoalescing
		NullCoalescing,
		// Conditional
		ConditionalQuestion,
		ConditionalColon,

		// Assignment
		Assignment,
		AdditionAssignment,
		SubtractionAssignment,
		MultiplicationAssignment,
		DivisionAssignment,
		ModulusAssignment,
		LeftShiftAssignment,
		RightShiftAssignment,
		UnsignedRightShiftAssignment,
		BitwiseAndAssignment,
		BitwiseXorAssignment,
		BitwiseOrAssignment,
		NullCoalescingAssignment,

		/* *** */

		// Punctuation
		Comma,
		ParenthesisClose,
		BracketClose,
		BraceClose,
		EndOfFile,
	}

	internal static class TokenTypeOverloads
	{
		const TokenType
			LITERALS_START = TokenType.Number,
			LITERALS_END = TokenType.This,
			PRIMARY_START = TokenType.Typeof,
			PRIMARY_END = TokenType.LogicalNegation,
			PREFIX_UNARY_START = TokenType.Increment,
			PREFIX_UNARY_END = TokenType.LogicalNegation,
			MULTIPLICATIVE_START = TokenType.Multiplication,
			MULTIPLICATIVE_END = TokenType.Modulus,
			ADDITIVE_START = TokenType.Plus,
			ADDITIVE_END = TokenType.Minus,
			SHIFT_START = TokenType.LeftShift,
			SHIFT_END = TokenType.UnsignedRightShift,
			RELATIONAL_START = TokenType.LessThan,
			RELATIONAL_END = TokenType.GreaterThanOrEqual,
			EQUALITY_START = TokenType.Equality,
			EQUALITY_END = TokenType.Inequality,
			ASSIGNMENT_START = TokenType.Assignment,
			ASSIGNMENT_END = TokenType.NullCoalescingAssignment,
			OPERATOR_START = PRIMARY_START,
			OPERATOR_END = ASSIGNMENT_END,
			MULTIARY_OPERATOR_START = MULTIPLICATIVE_START,
			MULTIARY_OPERATOR_END = ASSIGNMENT_END,
			PUNCUATION_TERM_START = TokenType.Comma,
			PUNCUATION_TERM_END = TokenType.EndOfFile;


		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsLiteral(this TokenType type)
		{
			return type >= LITERALS_START && type <= LITERALS_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsStrictPrimary(this TokenType type)
		{
			return type >= PRIMARY_START && type <= PRIMARY_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMultiplicative(this TokenType type)
		{
			return type >= MULTIPLICATIVE_START && type <= MULTIPLICATIVE_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAdditive(this TokenType type)
		{
			return type >= ADDITIVE_START && type <= ADDITIVE_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsShift(this TokenType type)
		{
			return type >= SHIFT_START && type <= SHIFT_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsRelational(this TokenType type)
		{
			return type >= RELATIONAL_START && type <= RELATIONAL_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsEquality(this TokenType type)
		{
			return type == TokenType.Equality || type == TokenType.Inequality;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsAssignment(this TokenType type)
		{
			return type >= ASSIGNMENT_START && type <= ASSIGNMENT_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsTerminatingPunctuation(this TokenType type)
		{
			return type >= PUNCUATION_TERM_START && type <= PUNCUATION_TERM_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsOperator(this TokenType type)
		{
			return type >= OPERATOR_START && type <= OPERATOR_END;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPostfixUnary(this TokenType type)
		{
			return type == TokenType.Increment || type == TokenType.Decrement;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsPrefixUnary(this TokenType type)
		{
			return (type >= PREFIX_UNARY_START && type <= PREFIX_UNARY_END) || type.IsAdditive();
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		public static bool IsMultiaryOperator(this TokenType type)
		{
			return type >= MULTIARY_OPERATOR_START && type <= MULTIARY_OPERATOR_END;
		}

		public static int MultiaryPrecedence(this TokenType type)
		{
			if (!type.IsMultiaryOperator()) // Includes unary operators
				return 0;

			// Leave space for range/switchwith in the future

			if (type <= EQUALITY_END)
			{
				if (type.IsMultiplicative())
					return 3;

				if (type.IsAdditive())
					return 4;

				if (type.IsShift())
					return 5;

				if (type.IsRelational())
					return 6;

				if (type.IsEquality())
					return 7;
			}

			if (type < ASSIGNMENT_START)
			{
				switch (type)
				{
					case TokenType.BitwiseAnd:
					case TokenType.BitwiseXor:
					case TokenType.BitwiseOr:
						return 8;
					case TokenType.ConditionalAnd:
						return 9;
					case TokenType.ConditionalOr:
						return 10;
					case TokenType.NullCoalescing:
						return 11;
					case TokenType.ConditionalQuestion:
					case TokenType.ConditionalColon:
						return 12;
				}
			}

			return 13;
		}
	}
}