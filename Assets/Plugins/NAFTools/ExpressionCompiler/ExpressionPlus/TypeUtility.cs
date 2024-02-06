using System;
using System.Linq.Expressions;
using System.Diagnostics;
using System.Reflection;

#nullable enable

namespace NAF.ExpressionCompiler {

	public static class TypeUtils
	{
		public static bool IsAssignableUsing(Type source, Type dest, out MethodInfo? method)
		{
			if (source == typeof(void) || dest == typeof(void))
			{
				method = null;
				return false;
			}

			if (AreEquivalent(source, dest))
			{
				method = null;
				return true;
			}

			// Primitive runtime conversions
			// All conversions amongst enum, bool, char, integer and float types
			// (and their corresponding nullable types) are legal except for
			// nonbool==>bool and nonbool==>bool?
			// Since we have already covered bool==>bool, bool==>bool?, etc, above,
			// we can just disallow having a bool or bool? destination type here.
			if (IsConvertible(source) && IsConvertible(dest) && GetNonNullableType(dest) != typeof(bool))
			{
				method = null;
				return true;
			}


			if (dest == typeof(object) || dest.IsAssignableFrom(source))
			{
				method = null;
				return true;
			}

			return (method = GetUserDefinedCoercionMethod(source, dest, false)) != null;
		}

		public struct BinaryOperatorResult
		{
			public MethodInfo Operator;
			public MethodInfo? LeftCoercion;
			public MethodInfo? RightCoercion;

			public BinaryOperatorResult(MethodInfo op, MethodInfo? left, MethodInfo? right)
			{
				Operator = op;
				LeftCoercion = left;
				RightCoercion = right;
			}
		}

		public struct UnaryOperatorResult
		{
			public MethodInfo Operator;
			public MethodInfo? Coercion;

			public UnaryOperatorResult(MethodInfo op, MethodInfo? operand)
			{
				Operator = op;
				Coercion = operand;
			}
		}

		internal static BinaryOperatorResult? FindBinaryOperator(Type hostType, Type leftType, Type rightType, string name)
		{
			var methods = hostType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			BinaryOperatorResult result = default;
			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo mi = methods[i];
				if (mi.Name != name) continue;

				ParameterInfo[] pis = mi.GetParametersCached();
				if (IsAssignableUsing(leftType, pis[0].ParameterType, out result.LeftCoercion))
				{
					if (IsAssignableUsing(rightType, pis[1].ParameterType, out result.RightCoercion))
					{
						result.Operator = mi;
						return result;
					}
				}
			}
			return null;
		}

		internal static UnaryOperatorResult? FindUnaryOperator(Type hostType, Type operandType, string name)
		{
			var methods = hostType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);

			UnaryOperatorResult result = default;
			for (int i = 0; i < methods.Length; i++)
			{
				MethodInfo mi = methods[i];
				if (mi.Name != name) continue;

				ParameterInfo[] pis = mi.GetParametersCached();
				if (IsAssignableUsing(operandType, pis[0].ParameterType, out result.Coercion))
				{
					result.Operator = mi;
					return result;
				}
			}
			return null;
		}

		internal static Type GetNonNullableType(this Type type) {
			if (IsNullableType(type)) {
				return type.GetGenericArguments()[0];
			}
			return type;
		}


		internal static bool IsNullableType(this Type type) {
			return type.IsGenericType && type.GetGenericTypeDefinition() == typeof(Nullable<>);
		}

		public static bool IsNumeric(Type type) {
			type = GetNonNullableType(type);
			if (!type.IsEnum) {
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Char:
					case TypeCode.SByte:
					case TypeCode.Byte:
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.Double:
					case TypeCode.Single:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						return true;
				}
			}
			return false;
		}

		internal static bool IsPrimitive(Type type) {
			type = GetNonNullableType(type);
			if (type.IsEnum)
				return true;
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.Char:
				case TypeCode.Double:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.SByte:
				case TypeCode.Single:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
			}
			return false;
		}

		internal static bool IsInteger(Type type) {
			type = GetNonNullableType(type);
			if (type.IsEnum) {
				return false;
			}
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
				default:
					return false;
			}
		}


		internal static bool IsArithmetic(Type type) {
			type = GetNonNullableType(type);
			if (!type.IsEnum) {
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Int16:
					case TypeCode.Int32:
					case TypeCode.Int64:
					case TypeCode.Double:
					case TypeCode.Single:
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						return true;
				}
			}
			return false;
		}

		internal static bool IsUnsignedInt(Type type) {
			type = GetNonNullableType(type);
			if (!type.IsEnum) {
				switch (Type.GetTypeCode(type)) {
					case TypeCode.UInt16:
					case TypeCode.UInt32:
					case TypeCode.UInt64:
						return true;
				}
			}
			return false;
		}

		internal static bool IsIntegerOrBool(Type type) {
			type = GetNonNullableType(type);
			if (!type.IsEnum) {
				switch (Type.GetTypeCode(type)) {
					case TypeCode.Int64:
					case TypeCode.Int32:
					case TypeCode.Int16:
					case TypeCode.UInt64:
					case TypeCode.UInt32:
					case TypeCode.UInt16:
					case TypeCode.Boolean:
					case TypeCode.SByte:
					case TypeCode.Byte:
						return true;
				}
			}
			return false;
		}

		internal static bool AreEquivalent(Type t1, Type t2)
		{
#if CLR2 || SILVERLIGHT
			return t1 == t2;
#else
			return t1 == t2 || t1.IsEquivalentTo(t2);
#endif
		}

		internal static bool AreReferenceAssignable(Type dest, Type src) {
			// WARNING: This actually implements "Is this identity assignable and/or reference assignable?"
			if (AreEquivalent(dest, src)) {
				return true;
			}
			if (!dest.IsValueType && !src.IsValueType && dest.IsAssignableFrom(src)) {
				return true;
			}
			return false;
		}

		internal static bool HasRefConversion(Type source, Type dest) {
			if (source == null || dest == null)
				throw new ArgumentNullException();

			// void -> void conversion is handled elsewhere
			// (it's an identity conversion)
			// All other void conversions are disallowed.
			if (source.IsByRef) {
				source = source.GetElementType()!;
			}

			if (dest.IsByRef) {
				dest = dest.GetElementType()!;
			}

			Type nnSourceType = TypeUtils.GetNonNullableType(source);
			Type nnDestType = TypeUtils.GetNonNullableType(dest);

			// Down conversion
			if (nnSourceType.IsAssignableFrom(nnDestType)) {
				return true;
			}
			// Up conversion
			if (nnDestType.IsAssignableFrom(nnSourceType)) {
				return true;
			}
			// Variant delegate conversion
			if (IsLegalExplicitVariantDelegateConversion(source, dest))
				return true;
				
			// Object conversion
			if (source == typeof(object) || dest == typeof(object)) {
				return true;
			}
			return false;
		}

		private static bool IsCovariant(Type t)
		{
			if (t == null) throw new ArgumentNullException(nameof(t));
			return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Covariant);
		}

		private static bool IsContravariant(Type t)
		{
			if (t == null) throw new ArgumentNullException(nameof(t));
			return 0 != (t.GenericParameterAttributes & GenericParameterAttributes.Contravariant);
		}

		private static bool IsInvariant(Type t)
		{
			if (t == null) throw new ArgumentNullException(nameof(t));
			return 0 == (t.GenericParameterAttributes & GenericParameterAttributes.VarianceMask);
		}

		private static bool IsDelegate(Type t)
		{
			if (t == null) throw new ArgumentNullException(nameof(t));
			return t.IsSubclassOf(typeof(System.MulticastDelegate));
		}

		internal static bool IsLegalExplicitVariantDelegateConversion(Type source, Type dest)
		{
			if (source == null || dest == null)
				throw new ArgumentNullException();

			// There *might* be a legal conversion from a generic delegate type S to generic delegate type  T, 
			// provided all of the follow are true:
			//   o Both types are constructed generic types of the same generic delegate type, D<X1,... Xk>.
			//     That is, S = D<S1...>, T = D<T1...>.
			//   o If type parameter Xi is declared to be invariant then Si must be identical to Ti.
			//   o If type parameter Xi is declared to be covariant ("out") then Si must be convertible
			//     to Ti via an identify conversion,  implicit reference conversion, or explicit reference conversion.
			//   o If type parameter Xi is declared to be contravariant ("in") then either Si must be identical to Ti, 
			//     or Si and Ti must both be reference types.

			if (!IsDelegate(source) || !IsDelegate(dest) || !source.IsGenericType || !dest.IsGenericType)
				return false;

			Type genericDelegate = source.GetGenericTypeDefinition();

			if (dest.GetGenericTypeDefinition() != genericDelegate)
				return false;

			Type[] genericParameters = genericDelegate.GetGenericArguments();
			Type[] sourceArguments = source.GetGenericArguments();
			Type[] destArguments = dest.GetGenericArguments();

			Debug.Assert(genericParameters != null);
			Debug.Assert(sourceArguments != null);
			Debug.Assert(destArguments != null);
			Debug.Assert(genericParameters!.Length == sourceArguments!.Length);
			Debug.Assert(genericParameters.Length == destArguments!.Length);

			for (int iParam = 0; iParam < genericParameters.Length; ++iParam)
			{
				Type sourceArgument = sourceArguments[iParam];
				Type destArgument = destArguments[iParam];

				Debug.Assert(sourceArgument != null && destArgument != null);
			   
				// If the arguments are identical then this one is automatically good, so skip it.
				if (AreEquivalent(sourceArgument!, destArgument!))
				{
					continue;
				}
				
				Type genericParameter = genericParameters[iParam];

				Debug.Assert(genericParameter != null);

				if (IsInvariant(genericParameter!))
				{
					return false;
				}
		
				if (IsCovariant(genericParameter!))
				{
					if (!HasRefConversion(sourceArgument!, destArgument!))
					{
						return false;
					}
				}
				else if (IsContravariant(genericParameter!))
				{
					if (sourceArgument!.IsValueType || destArgument!.IsValueType)
					{
						return false;
					}
				}
			}
			return true;
		}

		internal static bool IsConvertible(Type type) {
			type = GetNonNullableType(type);
			if (type.IsEnum) {
				return true;
			}
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Boolean:
				case TypeCode.Byte:
				case TypeCode.SByte:
				case TypeCode.Int16:
				case TypeCode.Int32:
				case TypeCode.Int64:
				case TypeCode.UInt16:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
				case TypeCode.Single:
				case TypeCode.Double:
				case TypeCode.Char:
					return true;
				default:
					return false;
			}
		}

		internal static bool HasReferenceEquality(Type left, Type right) {
			if (left.IsValueType || right.IsValueType) {
				return false;
			}

			// If we have an interface and a reference type then we can do 
			// reference equality.

			// If we have two reference types and one is assignable to the
			// other then we can do reference equality.

			return left.IsInterface || right.IsInterface ||
				AreReferenceAssignable(left, right) ||
				AreReferenceAssignable(right, left);
		}

		internal static bool HasBuiltInEqualityOperator(Type left, Type right) {
			// If we have an interface and a reference type then we can do 
			// reference equality.
			if (left.IsInterface && !right.IsValueType) {
				return true;
			}
			if (right.IsInterface && !left.IsValueType) {
				return true;
			}
			// If we have two reference types and one is assignable to the
			// other then we can do reference equality.
			if (!left.IsValueType && !right.IsValueType) {
				if (AreReferenceAssignable(left, right) || AreReferenceAssignable(right, left)) {
					return true;
				}
			}
			// Otherwise, if the types are not the same then we definitely 
			// do not have a built-in equality operator.
			if (!AreEquivalent(left, right)) {
				return false;
			}
			// We have two identical value types, modulo nullability.  (If they were both the 
			// same reference type then we would have returned true earlier.)
			Debug.Assert(left.IsValueType);
			// Equality between struct types is only defined for numerics, bools, enums,
			// and their nullable equivalents.
			Type nnType = GetNonNullableType(left);
			if (nnType == typeof(bool) || IsNumeric(nnType) || nnType.IsEnum) {
				return true;
			}
			return false;
		}

		internal static bool IsImplicitlyConvertible(Type source, Type destination) {
			return AreEquivalent(source, destination) ||                // identity conversion
				IsImplicitNumericConversion(source, destination) ||
				IsImplicitReferenceConversion(source, destination) ||
				IsImplicitBoxingConversion(source, destination) ||
				IsImplicitNullableConversion(source, destination);
		}


		public static MethodInfo? GetUserDefinedCoercionMethod(Type convertFrom, Type convertToType, bool implicitOnly) {
			// check for implicit coercions first
			Type nnExprType = TypeUtils.GetNonNullableType(convertFrom);
			Type nnConvType = TypeUtils.GetNonNullableType(convertToType);
			// try exact match on types
			MethodInfo[] eMethods = nnExprType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			MethodInfo? method = FindConversionOperator(eMethods, convertFrom, convertToType, implicitOnly);
			if (method != null) {
				return method;
			}
			MethodInfo[] cMethods = nnConvType.GetMethods(BindingFlags.Static | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.FlattenHierarchy);
			method = FindConversionOperator(cMethods, convertFrom, convertToType, implicitOnly);
			if (method != null) {
				return method;
			}
			return null;
		}

		internal static MethodInfo? FindConversionOperator(MethodInfo[] methods, Type typeFrom, Type typeTo, bool implicitOnly) {
			foreach (MethodInfo mi in methods) {
				if (mi.Name != "op_Implicit" && (implicitOnly || mi.Name != "op_Explicit")) {
					continue;
				}
				if (!typeTo.IsAssignableFrom(mi.ReturnType)) {
					continue;
				}
				ParameterInfo[] pis = mi.GetParametersCached();
				if (!pis[0].ParameterType.IsAssignableFrom(typeFrom)) {
					continue;
				}
				return mi;
			}
			return null;
		}

		

		[System.Diagnostics.CodeAnalysis.SuppressMessage("Microsoft.Maintainability", "CA1502:AvoidExcessiveComplexity")]
		private static bool IsImplicitNumericConversion(Type source, Type destination) {
			TypeCode tcSource = Type.GetTypeCode(source);
			TypeCode tcDest = Type.GetTypeCode(destination);

			switch (tcSource) {
				case TypeCode.SByte:
					switch (tcDest) {
						case TypeCode.Int16:
						case TypeCode.Int32:
						case TypeCode.Int64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Byte:
					switch (tcDest) {
						case TypeCode.Int16:
						case TypeCode.UInt16:
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Int16:
					switch (tcDest) {
						case TypeCode.Int32:
						case TypeCode.Int64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.UInt16:
					switch (tcDest) {
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Int32:
					switch (tcDest) {
						case TypeCode.Int64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.UInt32:
					switch (tcDest) {
						case TypeCode.UInt32:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Int64:
				case TypeCode.UInt64:
					switch (tcDest) {
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Char:
					switch (tcDest) {
						case TypeCode.UInt16:
						case TypeCode.Int32:
						case TypeCode.UInt32:
						case TypeCode.Int64:
						case TypeCode.UInt64:
						case TypeCode.Single:
						case TypeCode.Double:
						case TypeCode.Decimal:
							return true;
					}
					return false;
				case TypeCode.Single:
					return (tcDest == TypeCode.Double);
			}
			return false;
		}

		private static bool IsImplicitReferenceConversion(Type source, Type destination) {
			return destination.IsAssignableFrom(source);
		}
		private static bool IsImplicitBoxingConversion(Type source, Type destination) {
			if (source.IsValueType && (destination == typeof(object) || destination == typeof(System.ValueType)))
				return true;
			if (source.IsEnum && destination == typeof(System.Enum))
				return true;
			return false;
		}

		private static bool IsImplicitNullableConversion(Type source, Type destination) {
			if (IsNullableType(destination))
				return IsImplicitlyConvertible(GetNonNullableType(source), GetNonNullableType(destination));
			return false;
		}

		internal static bool IsSameOrSubclass(Type type, Type subType) {
			return AreEquivalent(type, subType) || subType.IsSubclassOf(type);
		}

		internal static void ValidateType(Type type) {
			if (type.IsGenericTypeDefinition) {
				throw new InvalidOperationException("Type " + type.Name + " is a generic type definition.");
			}
			if (type.ContainsGenericParameters) {
				throw new InvalidOperationException("Type " + type.Name + " contains generic parameters.");
			}
		}

		//from TypeHelper
		internal static Type? FindGenericType(Type definition, Type? type) {
			while (type != null && type != typeof(object)) {
				if (type.IsGenericType && AreEquivalent(type.GetGenericTypeDefinition(), definition)) {
					return type;
				}
				if (definition.IsInterface) {
					foreach (Type itype in type.GetInterfaces()) {
						Type? found = FindGenericType(definition, itype);
						if (found != null)
							return found;
					}
				}
				type = type.BaseType;
			}
			return null;
		}

		internal static bool IsUnsigned(Type type) {
			type = GetNonNullableType(type);
			switch (Type.GetTypeCode(type)) {
				case TypeCode.Byte:
				case TypeCode.UInt16:
				case TypeCode.Char:
				case TypeCode.UInt32:
				case TypeCode.UInt64:
					return true;
				default:
					return false;
			}
		}


		private static readonly Assembly _mscorlib = typeof(object).Assembly;
		private static readonly Assembly _systemCore = typeof(Expression).Assembly;

		/// <summary>
		/// We can cache references to types, as long as they aren't in
		/// collectable assemblies. Unfortunately, we can't really distinguish
		/// between different flavors of assemblies. But, we can at least
		/// create a ---- for types in mscorlib (so we get the primitives)
		/// and System.Core (so we find Func/Action overloads, etc).
		/// </summary>
		internal static bool CanCache(this Type t) {
			// Note: we don't have to scan base or declaring types here.
			// There's no way for a type in mscorlib to derive from or be
			// contained in a type from another assembly. The only thing we
			// need to look at is the generic arguments, which are the thing
			// that allows mscorlib types to be specialized by types in other
			// assemblies.

			var asm = t.Assembly;
			if (asm != _mscorlib && asm != _systemCore) {
				// Not in mscorlib or our assembly
				return false;
			}

			if (t.IsGenericType) {
				foreach (Type g in t.GetGenericArguments()) {
					if (!CanCache(g)) {
						return false;
					}
				}
			}

			return true;
		}

		
	}
}