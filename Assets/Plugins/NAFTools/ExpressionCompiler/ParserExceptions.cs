#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Collections.Generic;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using System.Text;

	public class ParserException : System.Exception
	{
		internal ParserException(string message) : base(message)
		{
		}

		internal ParserException(string message, System.Exception innerException) : base(message, innerException)
		{
		}

		internal ParserException(ReadOnlySpan<char> expression, int index, int length, string message) : this(ComposeMessage(expression, message, (Token.Span(index, length), '^')))
		{
		}

		internal ParserException(ReadOnlySpan<char> expression, int index, int length, string message, System.Exception innerException) : this(ComposeMessage(expression, message, (Token.Span(index, length), '^')), innerException)
		{
		}

		internal ParserException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message) : this(expression, stringSpan.start, stringSpan.length, message)
		{
		}

		internal ParserException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message, System.Exception innerException) : this(expression, stringSpan.start, stringSpan.length, message, innerException)
		{
		}

		internal static string ComposeMessage(ReadOnlySpan<char> expression, string message, params (IStringSpan span, char token)[] underline)
		{
			int min = underline.Min(u => u.span.start);
			int max = underline.Max(u => u.span.start + u.span.length);
			int length = max - min;

			Span<char> underlineSpan = stackalloc char[max];
			underlineSpan.Fill(' ');

			foreach ((IStringSpan span, char token) in underline)
				underlineSpan.Slice(span.start, span.length).Fill(token);

			return $"{message} [{min}, {length}]\n    {expression.ToString()}\n    {underlineSpan.ToString()}";
		}
	}

	public class SyntacticException : ParserException
	{
		internal SyntacticException(ReadOnlySpan<char> expression, int index, int length, string message) : base(expression, index, length, message) { }
		internal SyntacticException(ReadOnlySpan<char> expression, int index, int length, string message, System.Exception innerException) : base(expression, index, length, message, innerException) { }
	}

	public class SymanticException : ParserException
	{
		internal SymanticException(ReadOnlySpan<char> expression, int index, int length, string message) : base(expression, index, length, message) { }
		internal SymanticException(ReadOnlySpan<char> expression, int index, int length, string message, System.Exception innerException) : base(expression, index, length, message, innerException) { }
		internal SymanticException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message) : base(expression, stringSpan, message) { }
		internal SymanticException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message, System.Exception innerException) : base(expression, stringSpan, message, innerException) { }

		internal SymanticException(ReadOnlySpan<char> expression, in TokenType expected, in Token token, string? message) : this(
			expression,
			token,
			(string.IsNullOrEmpty(message) ? string.Empty : message + "\n") + $"Expected token type '{expected}' but got '{token.type}'")
		{
		}
	}

	public class InternalException : ParserException
	{
		public InternalException(string message) : base(message) { }
		public InternalException(ReadOnlySpan<char> expression, int index, int length, string message) : base(expression, index, length, message) { }
		public InternalException(ReadOnlySpan<char> expression, int index, int length, string message, System.Exception innerException) : base(expression, index, length, message, innerException) { }
		internal InternalException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message) : base(expression, stringSpan, message) { }
		internal InternalException(ReadOnlySpan<char> expression, IStringSpan stringSpan, string message, System.Exception innerException) : base(expression, stringSpan, message, innerException) { }
	}

	public class MemberAccessException : ParserException
	{
		internal MemberAccessException(ReadOnlySpan<char> expression, IStringSpan term, IStringSpan member, string message) : base(ComposeMessage(expression, message, (term, '-'), (Token.Span(term.start + term.length - 1), '.'), (member, '^')))
		{
		}

		internal MemberAccessException(ReadOnlySpan<char> expression, IStringSpan term, IStringSpan member, StackSegment<Term> givenArgs, IEnumerable<MemberInfo> members) : this(expression, term, member, "No members were found that matches the arguments provided.\n    Given Arguments:" + string.Join(", ", givenArgs.ToArray().Select(e => e.expression?.Type)) + "\n    Found Members:\n        " + string.Join("\n        ", members.Select(m => MemberColorizer.Default.DeclarationContent(m).RichText)))
		{
		}
	}

	public class OperationException : ParserException
	{
		internal OperationException(ReadOnlySpan<char> expression, Token op, Term left, Term right, string message) : base(ComposeMessage(expression, message, (left, 'l'), (right, 'r'), (op, '^')))
		{
		}

		internal OperationException(ReadOnlySpan<char> expression, Token question, Token colon, Term test, Term left, Term right, string message) : base(ComposeMessage(expression, message, (test, 't'), (left, 'l'), (right, 'r'), (question, '?'), (colon, ':')))
		{
		}

		internal OperationException(ReadOnlySpan<char> expression, Token op, Term term, string message) : base(ComposeMessage(expression, message, (term, '^'), (op, '^')))
		{
		}

		internal OperationException(ReadOnlySpan<char> expression, Token op, Term left, Term right, System.Exception innerException) : base(ComposeMessage(expression, $"Cannot apply operator '{op.type}' to operands of type '{left.expression!.Type}' and '{right.expression!.Type}'.", (left, 'l'), (right, 'r'), (op, '^')), innerException)
		{
		}

		internal OperationException(ReadOnlySpan<char> expression, Token op, Term term, System.Exception innerException) : base(ComposeMessage(expression, $"Cannot apply operator '{op.type}' to operand of type '{term.expression!.Type}'.", (term, '^'), (op, '^')), innerException)
		{
		}
	}
}