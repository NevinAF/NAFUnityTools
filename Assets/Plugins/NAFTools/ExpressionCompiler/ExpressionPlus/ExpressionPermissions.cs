namespace NAF.ExpressionCompiler
{
	using System;
	using System.Linq.Expressions;
	using System.Reflection;

	public ref partial struct Parser
	{
		private void RequiresCanRead(in Term term)
		{
			Expression expr = ExpectTerm(term);

			switch (expr.NodeType)
			{
				case ExpressionType.Index:
					IndexExpression index = (IndexExpression)expr;
					if (index.Indexer != null && !index.Indexer.CanRead) {
						throw new SymanticException(expression, term, "Indexer '" + index.Indexer.Name + "' does not have a get accessor and cannot be used in the given context");
					}
					break;
				case ExpressionType.MemberAccess:
					MemberExpression member = (MemberExpression)expr;
					MemberInfo memberInfo = member.Member;
					if (memberInfo.MemberType == MemberTypes.Property) {
						PropertyInfo prop = (PropertyInfo)memberInfo;
						if (!prop.CanRead) {
							throw new SymanticException(expression, term, "Property '" + prop.Name + "' does not have a get accessor and cannot be used in the given context");
						}
					}
					break;
			}
		}

		private void RequiresCanWrite(in Term term)
		{
			Expression expr = ExpectTerm(term);

			switch (expr.NodeType) {
				case ExpressionType.Index:
					IndexExpression index = (IndexExpression)expr;
					if (index.Indexer != null && !index.Indexer.CanWrite)
						throw new SymanticException(expression, term, "Indexer '" + index.Indexer.Name + "' does not have a set accessor and cannot be used in an assignment");
					break;
				case ExpressionType.MemberAccess:
					MemberExpression member = (MemberExpression)expr;
					switch (member.Member.MemberType) {
						case MemberTypes.Property:
							PropertyInfo prop = (PropertyInfo)member.Member;
							if (!prop.CanWrite)
								throw new SymanticException(expression, term, "Property '" + prop.Name + "' does not have a set accessor and cannot be used in an assignment");
							break;
						case MemberTypes.Field:
							FieldInfo field = (FieldInfo)member.Member;
							if (field.IsInitOnly || field.IsLiteral)
								throw new SymanticException(expression, term, "Field '" + field.Name + "' is readonly and cannot be used in an assignment");
							break;
					}
					break;
				case ExpressionType.Parameter:
					break;

				default:
					throw new SymanticException(expression, term, "The write parameter of an assignment must be a variable, property or indexer");
			}
		}
	}
}