using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace NAF.ExpressionCompiler
{
	public static class EmitUtils
	{
		private static void UnboxIfNeeded(ILGenerator il, Type current, Type target)
		{
			if (current != target)
			{
				if (target.IsValueType)
				{
					if (current != typeof(object))
						throw new ArgumentException("Cannot unbox (parameter) to a value type from a non-object type: " + current.Name + " to " + target.Name);

					il.Emit(OpCodes.Unbox_Any, target);
				}
				else {
					if (!current.IsAssignableFrom(target))
						throw new ArgumentException("Cannot cast (parameter) to a non-compatible type: " + current.Name + " to " + target.Name);
					il.Emit(OpCodes.Castclass, target);
				}
			}
		}

		private static void BoxIfNeeded(ILGenerator il, Type current, Type target)
		{
			if (current != target)
			{
				if (current.IsValueType)
				{
					if (target != typeof(object))
						throw new ArgumentException("Cannot box (return) to a non-object type: " + current.Name + " to " + target.Name);
				}
				else
				{
					if (!target.IsAssignableFrom(current))
						throw new ArgumentException("Cannot cast (return) to a non-compatible type: " + current.Name + " to " + target.Name);

					il.Emit(OpCodes.Castclass, target);
				}
			}
		}

		private static void EmitMethodCall(ILGenerator il, MethodBase method, Type[] funcParameters, Type funcReturnType)
		{
			ParameterInfo[] parameters = method.GetParameters();
			int funcIndex = 0;

			if (!method.IsStatic && !method.IsConstructor)
			{
				funcIndex = 1;
				il.Emit(OpCodes.Ldarg_0);
				UnboxIfNeeded(il, funcParameters[0], method.DeclaringType);
			}

			if (funcIndex + parameters.Length != funcParameters.Length)
				throw new ArgumentException("Invalid number of parameters");

			for (int i = 0; i < parameters.Length; i++, funcIndex++)
			{
				il.Emit(OpCodes.Ldarg, funcIndex);
				UnboxIfNeeded(il, funcParameters[funcIndex], parameters[i].ParameterType);
			}

			if (method.IsConstructor)
			{
				il.Emit(OpCodes.Newobj, (ConstructorInfo)method);
				BoxIfNeeded(il, method.DeclaringType, funcReturnType);
			}
			else
			{
				MethodInfo methodInfo = (MethodInfo)method;
				il.Emit(OpCodes.Call, methodInfo);

				if (funcReturnType != typeof(void))
				{
					BoxIfNeeded(il, methodInfo.ReturnType, funcReturnType);
				}
			}
		}

		public static void BoxedMember<T>(MemberInfo member, out T result) where T : MulticastDelegate
		{
			result = BoxedMember<T>(member);
		}

		public static T BoxedMember<T>(MemberInfo member) where T : MulticastDelegate
		{
			MethodInfo funcSignature = typeof(T).GetMethod("Invoke");

			ParameterInfo[] funcParamInfos = funcSignature.GetParametersCached();
			Type[] funcParams = new Type[funcParamInfos.Length];
			for (int i = 0; i < funcParamInfos.Length; i++)
				funcParams[i] = funcParamInfos[i].ParameterType;
			Type funcReturnType = funcSignature.ReturnType;

			DynamicMethod dynamicMethod = new DynamicMethod("Member", funcReturnType, funcParams, typeof(EmitUtils), true);
			ILGenerator il = dynamicMethod.GetILGenerator();

			switch (member.MemberType)
			{
				case MemberTypes.Constructor:
					EmitMethodCall(il, (ConstructorInfo)member, funcParams, funcReturnType);
					break;
				case MemberTypes.Method:
					EmitMethodCall(il, (MethodInfo)member, funcParams, funcReturnType);
					break;
				case MemberTypes.Property:
					PropertyInfo property = (PropertyInfo)member;
					if (funcReturnType != typeof(void))
					{
						if (property.CanRead)
							EmitMethodCall(il, property.GetGetMethod(), funcParams, funcReturnType);
						else
							throw new ArgumentException("Property has no getter");
					}
					else
					{
						if (property.CanWrite)
							EmitMethodCall(il, property.GetSetMethod(), funcParams, funcReturnType);
						else
							throw new ArgumentException("Property has no setter");
					}
					break;
				case MemberTypes.Field:
					FieldInfo field = (FieldInfo)member;
					bool isStatic = field.IsStatic;
					if (funcReturnType != typeof(void))
					{
						if (isStatic)
							il.Emit(OpCodes.Ldsfld, field);
						else
						{
							il.Emit(OpCodes.Ldarg_0);
							UnboxIfNeeded(il, funcParams[0], field.DeclaringType);
							il.Emit(OpCodes.Ldfld, field);
						}
						BoxIfNeeded(il, field.FieldType, funcReturnType);
					}
					else
					{
						if (isStatic)
						{
							il.Emit(OpCodes.Ldarg_0);
							UnboxIfNeeded(il, funcParams[0], field.FieldType);
							il.Emit(OpCodes.Stsfld, field);
						}
						else
						{
							il.Emit(OpCodes.Ldarg_0);
							UnboxIfNeeded(il, funcParams[0], field.DeclaringType);
							il.Emit(OpCodes.Ldarg_1);
							UnboxIfNeeded(il, funcParams[1], field.FieldType);
							il.Emit(OpCodes.Stfld, field);
						}
					}
					break;
				default:
					throw new ArgumentException("Member is not a field, property or method");
			}

			il.Emit(OpCodes.Ret);

			// Create the delegate
			return (T)dynamicMethod.CreateDelegate(typeof(T));
		}

	}
}