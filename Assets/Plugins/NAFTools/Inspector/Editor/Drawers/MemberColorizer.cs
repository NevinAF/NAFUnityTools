using System;
using NAF.ExpressionCompiler;

namespace NAF.Inspector.Editor
{
	public class UnityMemberColorizer : MemberColorizer
	{
		public UnityMemberColorizer(params Type[] blacklistAttributes) : base(blacklistAttributes) { }

		public override string Colorize(string text, TokenType type)
		{
			string color = type switch
			{
				TokenType.Keyword => DEFAULT_KEYWORD_COLOR,
				TokenType.Class => DEFAULT_CLASS_COLOR,
				TokenType.Struct => DEFAULT_STRUCT_COLOR,
				TokenType.KeyType => DEFAULT_KEY_TYPE_COLOR,
				TokenType.Field => DEFAULT_FIELD_COLOR,
				TokenType.Property => DEFAULT_PROP_COLOR,
				TokenType.Method => DEFAULT_METHOD_COLOR,
				TokenType.Parameter => DEFAULT_PARAMETER_COLOR,
				TokenType.String => DEFAULT_STRING_COLOR,
				TokenType.Numeric => DEFAULT_NUMERIC_COLOR,
				_ => throw new ArgumentOutOfRangeException(nameof(type)),
			};

			return $"<color={color}>{text}</color>";
		}
	}
}