#nullable enable
namespace NAF.ExpressionCompiler
{
	using System;
	using System.Collections.Generic;
	using System.Linq.Expressions;
	using System.Reflection;

	public ref partial struct Parser
	{
		/// <summary>
		/// Given a list of members and parameters, this method will determine the best match for the given arguments. If no match is found, -1 is returned. In addition, the arguments are converted (and default parameters are added) to the best matching member.
		/// </summary>
		/// <param name="members">The members to be matched. Must have type 'Method', 'Constructor', or 'Property'.</param>
		/// <param name="arguments">The arguments to be passed into the method.</param>
		/// <param name="convertedArguments">The converted arguments to be passed into the method. Null if no match is found.</param>
		/// <returns>The member index of the best match. -1 if no match is found.</returns>
		/// <exception cref="InternalException">Thrown if the member type is not 'Method', 'Constructor', or 'Property'.</exception>
		private int SetupBestParameterMatch(in Term member, MemberInfo[] members, StackSegment<Term> arguments, out Expression[]? convertedArguments)
		{
			if (members == null || members.Length == 0)
			{
				convertedArguments = null;
				return -1;
			}

			Span<ParameterInfo[]> parameterBuffer = compiler.ParameterBuffer(members.Length);

			switch (members[0].MemberType)
			{
				case MemberTypes.Method:
				case MemberTypes.Constructor: 
					for (int i = 0; i < members.Length; i++)
						parameterBuffer[i] = ((MethodBase)members[i]).GetParametersCached();
					break;

				case MemberTypes.Property:
					bool containsIndexer = false;
					for (int i = 0; i < members.Length; i++)
					{
						parameterBuffer[i] = ((PropertyInfo)members[i]).GetIndexParameters();
						containsIndexer |= parameterBuffer[i].Length > 0;
					}

					if (containsIndexer == false)
					{
						convertedArguments = null;
						return -2;
					}
					break;

				default:
					throw new InternalException(expression, member, "Cannot setup parameter match for member type '" + members[0].MemberType + "'.");
			}

			return SetupBestParameterMatch(parameterBuffer, arguments, out convertedArguments);
		}

		/// <inheritdoc cref="SetupBestParameterMatch(MemberInfo[], StackSegment{Term}, out Expression[]?)"/>
		private int SetupBestParameterMatch(Span<ParameterInfo[]> parameterOptions, StackSegment<Term> arguments, out Expression[]? convertedArguments)
		{
			int membersCount = parameterOptions.Length;
			if (arguments.Count == 0)
			{
				for (int i = 0; i < membersCount; i++)
				{
					convertedArguments = ConvertArguments(parameterOptions[i], arguments);
					if (convertedArguments != null)
						return i;
				}
				convertedArguments = null;
				return -1;
			}

			if (membersCount == 1)
			{
				convertedArguments = ConvertArguments(parameterOptions[0], arguments);
				if (convertedArguments != null)
					return 0;
				return -1;
			}

			// if (membersCount > 1)
			Span<byte> parameterTypeMatches = (membersCount < 128) ? stackalloc byte[membersCount] : new byte[membersCount];
			int parameterCount = arguments.Count;

			// Setup parameter type matches. This is to make sure that methods without converted arguments are prioritized over ones with converted arguments.
			for (int i = 0; i < membersCount; i++)
			{
				ParameterInfo[] parameters = parameterOptions[i];

				if (parameters.Length < parameterCount)
				{
					parameterTypeMatches[i] = byte.MaxValue;
					continue;
				}

				byte matches = 0;
				for (int j = 0; j < arguments.Count; j++)
				{
					if (parameters[j].ParameterType == ExpectTerm(arguments[j]).Type)
						matches++;
				}

				// Identical signature found. Only one would exists and no convertions needed.
				if (matches == parameters.Length)
				{
					convertedArguments = compiler.ConverBuffer(parameters.Length);
					for (int j = 0; j < arguments.Count; j++)
						convertedArguments[j] = ExpectTerm(arguments[j]);
					return i;
				}

				parameterTypeMatches[i] = matches;
			}

			while (true)
			{
				int bestMatch = -1;
				byte bestMatchCount = 0;
				for (int i = 0; i < parameterTypeMatches.Length; i++)
				{
					if (parameterTypeMatches[i] == byte.MaxValue)
						continue;

					if (parameterTypeMatches[i] >= bestMatchCount)
					{
						bestMatch = i;
						bestMatchCount = parameterTypeMatches[i];
					}
				}

				if (bestMatch != -1)
				{
					ParameterInfo[] parameters = parameterOptions[bestMatch];
					convertedArguments = ConvertArguments(parameters, arguments);
					if (convertedArguments != null)
						return bestMatch;
					parameterTypeMatches[bestMatch] = byte.MaxValue;
				}
				else
				{
					convertedArguments = null;
					return -1;
				}
			}
		
		}

		/// <summary> Returns a list of arguments converted to match (and add defaults) the given parameters. Returns null if the arguments cannot be converted. </summary>
		/// <param name="parameters">The parameters to be matched.</param>
		/// <param name="arguments">The arguments to be converted.</param>
		/// <returns>A list of arguments converted to match (and add defaults) the given parameters. Returns null if the arguments cannot be converted.</returns>
		internal Expression[]? ConvertArguments(ParameterInfo[] parameters, StackSegment<Term> arguments)
		{
			if (arguments.Count > parameters.Length)
				return null;

			Expression[] buffer = compiler.ConverBuffer(parameters.Length);

			if (arguments.Count == 0 && parameters.Length == 0)
				return buffer;

			try {

				for (int i = arguments.Count; i < parameters.Length; i++)
					if (!parameters[i].HasDefaultValue)
						return null;

				for (int i = 0; i < arguments.Count; i++)
				{
					Expression? expr = TryConvert(arguments[i], parameters[i].ParameterType);
					if (expr == null) // failed to convert
						return null;

					buffer[i] = expr;
				}

				for (int i = arguments.Count; i < parameters.Length; i++)
					buffer[i] = Expression.Constant(parameters[i].DefaultValue, parameters[i].ParameterType);

				return buffer;
			}
			catch (ArgumentException)
			{
				return null;
			}
		}
	}
}