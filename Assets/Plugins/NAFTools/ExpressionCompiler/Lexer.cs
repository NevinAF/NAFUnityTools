#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Runtime.CompilerServices;

	internal ref struct Lexer
	{
		private ReadOnlySpan<char> _source;
		private int _position;
		private Token peek;

		internal Lexer(ReadOnlySpan<char> source)
		{
			_source = source;
			_position = 0;
			peek = default;
		}
	
		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Token Peek()
		{
			if (peek.type == TokenType.Invalid)
				peek = NextToken();
			return peek;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Token Return(Token token)
		{
			if (peek.type != TokenType.Invalid && !peek.Equals(token))
				throw new InternalException(_source, token, "Cannot return a token when there is already a token in the peek buffer.");

			peek = token;
			return token;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal readonly ReadOnlySpan<char> GetTokenSpan(Token token)
		{
			return _source.Slice(token.start, token.length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal Token Expected(in TokenType expected, string? message = null)
		{
			Token token = NextToken();
			if (token.type != expected)
				throw new SymanticException(_source, expected, token, message);
			return token;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool PeekIs(in TokenType type)
		{
			return Peek().type == type;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		internal bool AcceptIf(in TokenType type)
		{
			if (PeekIs(type))
			{
				NextToken();
				return true;
			}
			return false;
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private Token Get(TokenType type, int length = 1)
		{
			int start = _position;
			_position += length;
			return new Token(type, start, length);
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private char? PeekUpcoming(int offset = 1)
		{
			int position = _position + offset;
			return position < _source.Length ? _source[position] : (char?)null;
		}

		internal Token NextToken()
		{
			if (peek.type != TokenType.Invalid)
			{
				Token token = peek;
				peek = default;
				return token;
			}

		NextToken: // Label to prevent adding a loop or additional stack calls
			if (_position >= _source.Length)
				return Get(TokenType.EndOfFile, 0);

			char c = _source[_position];

			switch (c)
			{
				case ' ':
				case '\t':
				case '\r':
				case '\n':
					_position++;
					goto NextToken;

				case '(': return Get(TokenType.ParenthesisOpen);
				case ')': return Get(TokenType.ParenthesisClose);
				case '[': return Get(TokenType.BracketOpen);
				case ']': return Get(TokenType.BracketClose);
				case '{': return Get(TokenType.BraceOpen);
				case '}': return Get(TokenType.BraceClose);

				case '?': return PeekUpcoming() switch {
						'?' => PeekUpcoming(2) switch {
								'=' => Get(TokenType.NullCoalescingAssignment, 3),
								_ => Get(TokenType.NullCoalescing, 2),
							},
						'.' => Get(TokenType.NullConditional, 1), // The null conditional usually includes access, but this makes the parser able to handle the "NullConditional" parse without knowing the access type.
						'[' => Get(TokenType.NullConditional, 1), // The null conditional usually includes access, but this makes the parser able to handle the "NullConditional" parse without knowing the access type.
						_ => Get(TokenType.ConditionalQuestion),
					};

				case ':': return Get(TokenType.ConditionalColon);

				case '+': return PeekUpcoming() switch {
						'+' => Get(TokenType.Increment, 2),
						'=' => Get(TokenType.AdditionAssignment, 2),
						_ => Get(TokenType.Plus),
					};

				case '-': return PeekUpcoming() switch {
						'-' => Get(TokenType.Decrement, 2),
						'=' => Get(TokenType.SubtractionAssignment, 2),
						'>' => Get(TokenType.MemberAccess, 2),
						_ => Get(TokenType.Minus),
					};

				case '*': return PeekUpcoming() switch {
						'=' => Get(TokenType.MultiplicationAssignment, 2),
						_ => Get(TokenType.Multiplication),
					};

				case '/': return PeekUpcoming() switch {
						'=' => Get(TokenType.DivisionAssignment, 2),
						_ => Get(TokenType.Division),
					};

				case '%': return PeekUpcoming() switch {
						'=' => Get(TokenType.ModulusAssignment, 2),
						_ => Get(TokenType.Modulus),
					};

				case '&': return PeekUpcoming() switch {
						'&' => Get(TokenType.ConditionalAnd, 2),
						'=' => Get(TokenType.BitwiseAndAssignment, 2),
						_ => Get(TokenType.BitwiseAnd),
					};

				case '|': return PeekUpcoming() switch {
						'|' => Get(TokenType.ConditionalOr, 2),
						'=' => Get(TokenType.BitwiseOrAssignment, 2),
						_ => Get(TokenType.BitwiseOr),
					};

				case '^': return PeekUpcoming() switch {
						'=' => Get(TokenType.BitwiseXorAssignment, 2),
						_ => Get(TokenType.BitwiseXor),
					};

				case '~': return Get(TokenType.Complement);

				case '!': return PeekUpcoming() switch {
						'=' => Get(TokenType.Inequality, 2),
						_ => Get(TokenType.LogicalNegation),
					};

				case '=': return PeekUpcoming() switch {
						'=' => Get(TokenType.Equality, 2),
						// '>' => Get(TokenType.Lambda, 2),
						'?' => Get(TokenType.NullCoalescing, 2),
						_ => Get(TokenType.Assignment),
					};

				case '<': return PeekUpcoming() switch {
						'=' => Get(TokenType.LessThanOrEqual, 2),
						'<' => PeekUpcoming(2) switch {
								'=' => Get(TokenType.LeftShiftAssignment, 3),
								_ => Get(TokenType.LeftShift, 2),
							},
						_ => Get(TokenType.LessThan),
					};

				case '>': return PeekUpcoming() switch {
						'=' => Get(TokenType.GreaterThanOrEqual, 2),
						'>' => PeekUpcoming(2) switch {
								'=' => Get(TokenType.RightShiftAssignment, 3),
								'>' => PeekUpcoming(3) switch {
										'=' => Get(TokenType.UnsignedRightShiftAssignment, 4),
										_ => Get(TokenType.UnsignedRightShift, 3),
									},
								_ => Get(TokenType.RightShift, 2),
							},
						_ => Get(TokenType.GreaterThan),
					};

				case '.':
					if (_position + 1 < _source.Length && char.IsDigit(_source[_position + 1]))
						return AsNumber();
					return Get(TokenType.MemberAccess);

				case ',': return Get(TokenType.Comma);

				case '\'': return AsChar() ?? AsString('\'');
				case '\"': return AsString('\"');

				default:
					{
						if (char.IsDigit(c)) return AsNumber();
						if (char.IsLetter(c) || c == '_') return AsIdentifier();

						throw new SyntacticException(_source, _position, 1, $"Unexpected start of a token '{c}'");
					}
			}
		}

		private Token AsNumber()
		{
			int start = _position + 1;

			while (start < _source.Length)
			{
				char c = _source[start];
				if (c == '_' || char.IsLetterOrDigit(c))
				{
				}
				else if ((c == '+' || c == '-') && _source[start - 1] == 'e')
				{
				}
				else if (c == '.')
				{
					if (start + 1 >= _source.Length || !char.IsDigit(_source[start + 1]))
						break;
					start++;
				}
				else break;

				start++;
			}

			return Get(TokenType.Number, start - _position);
		}

		private Token AsString(char quote)
		{
			int start = _position + 1;

			while (start < _source.Length)
			{
				char c = _source[start];

				if (c == quote)
					return Get(TokenType.String, start - _position + 1);

				if (c == '\\')
					start++;

				start++;
			}

			throw new SyntacticException(_source, _position, _source.Length - _position, "String was not terminated. Expected " + quote + " before the end of the input.");
		}

		private Token AsIdentifier()
		{
			int start = _position + 1;

			while (start < _source.Length && (char.IsLetterOrDigit(_source[start]) || _source[start] == '_'))
				start++;

			int length = start - _position;

			if (length >= 3 && length <= 6)
			{
				ReadOnlySpan<char> span = _source.Slice(_position, length);
				switch (length)
				{
					case 3:
						if (span.Equals("new", StringComparison.Ordinal))
							return Get(TokenType.New, length);
						break;

					case 4:
						if (span.Equals("true", StringComparison.Ordinal))
							return Get(TokenType.True, length);
						if (span.Equals("null", StringComparison.Ordinal))
							return Get(TokenType.Null, length);
						if (span.Equals("base", StringComparison.Ordinal))
							return Get(TokenType.Base, length);
						if (span.Equals("this", StringComparison.Ordinal))
							return Get(TokenType.This, length);
						break;

					case 5:
						if (span.Equals("false", StringComparison.Ordinal))
							return Get(TokenType.False, length);
						break;

					case 6:
						if (span.Equals("typeof", StringComparison.Ordinal))
							return Get(TokenType.Typeof, length);
						break;
				}
			}

			return Get(TokenType.Identifier, length);
		}

		private Token? AsChar()
		{
			if (_position + 2 < _source.Length)
			{
				if (_source[_position + 1] == '\\')
				{
					if (_position + 3 < _source.Length && _source[_position + 3] == '\'')
						return Get(TokenType.Character, 4);
				}
				else if (_source[_position + 2] == '\'')
					return Get(TokenType.Character, 3);
			}

			return null;
		}
	}
}
#nullable restore