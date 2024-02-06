using System;
using System.Reflection;
using System.Reflection.Emit;
using System.Linq.Expressions;

namespace NAF.ExpressionCompiler
{
	public static class EmitUtils
	{
		public static Delegate CreateConstructor(Type type, Type[] parameterTypes, Type asFuncType)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			parameterTypes ??= Type.EmptyTypes;

			if (asFuncType == null)
			{
				Type[] parameterTypesPlusOne = new Type[parameterTypes.Length + 1];
				parameterTypes.CopyTo(parameterTypesPlusOne, 0);
				parameterTypesPlusOne[parameterTypes.Length] = type;
				asFuncType = Expression.GetFuncType(parameterTypesPlusOne);
			}

			DynamicMethod dm = new DynamicMethod("ctor", type, parameterTypes, type, true);
			ILGenerator il = dm.GetILGenerator();

			ConstructorInfo ctor = type.GetConstructor(BindingFlags.Instance | BindingFlags.NonPublic, null, parameterTypes, null) ??
				throw new ArgumentException("No constructor found matching the specified arguments");

			for (int i = 0; i < parameterTypes.Length; i++)
				il.Emit(OpCodes.Ldarg, i);

			il.Emit(OpCodes.Newobj, ctor);
			il.Emit(OpCodes.Ret);

			return dm.CreateDelegate(asFuncType);
		}

		public static Delegate FieldSetter(Type type, string fieldName)
		{
			if (type == null)
				throw new ArgumentNullException(nameof(type));

			if (fieldName == null)
				throw new ArgumentNullException(nameof(fieldName));

			FieldInfo field = type.GetField(fieldName, BindingFlags.Instance | BindingFlags.NonPublic) ??
				throw new ArgumentException("No field found matching the specified name");


			DynamicMethod dm = new DynamicMethod("setter", null, new Type[] { type, field.FieldType }, type, true);
			ILGenerator il = dm.GetILGenerator();

			il.Emit(OpCodes.Ldarg_0);
			il.Emit(OpCodes.Ldarg_1);
			il.Emit(OpCodes.Stfld, field);
			il.Emit(OpCodes.Ret);

			var asFuncType = Expression.GetFuncType(type, field.FieldType);
			return dm.CreateDelegate(asFuncType);
		}

		// public static Delegate GetFieldGetter(Type type, FieldInfo field, Type asFuncType)
		// {
		// 	if (type == null)
		// 		throw new ArgumentNullException(nameof(type));

		// 	if (field == null)
		// 		throw new ArgumentNullException(nameof(field));

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(type, field.FieldType);

		// 	DynamicMethod dm = new DynamicMethod("getter", field.FieldType, new Type[] { type }, type, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	il.Emit(OpCodes.Ldarg_0);
		// 	il.Emit(OpCodes.Ldfld, field);
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }

		// public static Delegate GetMethod(Type type, MethodInfo method, Type asFuncType)
		// {
		// 	if (type == null)
		// 		throw new ArgumentNullException(nameof(type));

		// 	if (method == null)
		// 		throw new ArgumentNullException(nameof(method));

		// 	ParameterInfo[] parameters = method.GetParametersCached();
		// 	Type[] parameterTypes = new Type[parameters.Length + 1];

		// 	parameterTypes[0] = type;
		// 	for (int i = 0; i < parameters.Length; i++)
		// 		parameterTypes[i + 1] = parameters[i].ParameterType;

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(parameterTypes);

		// 	DynamicMethod dm = new DynamicMethod("method", method.ReturnType, parameterTypes, type, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	il.Emit(OpCodes.Ldarg_0);
		// 	for (int i = 0; i < method.GetParameters().Length; i++)
		// 		il.Emit(OpCodes.Ldarg, i + 1);

		// 	il.Emit(OpCodes.Call, method);
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }

		// public static Delegate GetPropertyGetter(Type type, PropertyInfo property, Type asFuncType)
		// {
		// 	if (type == null)
		// 		throw new ArgumentNullException(nameof(type));

		// 	if (property == null)
		// 		throw new ArgumentNullException(nameof(property));

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(type, property.PropertyType);

		// 	DynamicMethod dm = new DynamicMethod("getter", property.PropertyType, new Type[] { type }, type, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	il.Emit(OpCodes.Ldarg_0);
		// 	il.Emit(OpCodes.Call, property.GetGetMethod(true));
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }

		// public static Delegate GetFieldGetter(FieldInfo staticField, Type asFuncType)
		// {
		// 	if (staticField == null)
		// 		throw new ArgumentNullException(nameof(staticField));

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(staticField.FieldType);

		// 	DynamicMethod dm = new DynamicMethod("getter", staticField.FieldType, Type.EmptyTypes, staticField.DeclaringType, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	il.Emit(OpCodes.Ldsfld, staticField);
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }

		// public static Delegate GetMethod(MethodInfo staticMethod, Type asFuncType)
		// {
		// 	if (staticMethod == null)
		// 		throw new ArgumentNullException(nameof(staticMethod));

		// 	ParameterInfo[] parameters = staticMethod.GetParametersCached();
		// 	Type[] parameterTypes = new Type[parameters.Length];

		// 	for (int i = 0; i < parameters.Length; i++)
		// 		parameterTypes[i] = parameters[i].ParameterType;

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(parameterTypes);

		// 	DynamicMethod dm = new DynamicMethod("method", staticMethod.ReturnType, parameterTypes, staticMethod.DeclaringType, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	for (int i = 0; i < parameters.Length; i++)
		// 		il.Emit(OpCodes.Ldarg, i);

		// 	il.Emit(OpCodes.Call, staticMethod);
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }

		// public static Delegate GetPropertyGetter(PropertyInfo staticProperty, Type asFuncType)
		// {
		// 	if (staticProperty == null)
		// 		throw new ArgumentNullException(nameof(staticProperty));

		// 	if (asFuncType == null)
		// 		asFuncType = Expression.GetFuncType(staticProperty.PropertyType);

		// 	DynamicMethod dm = new DynamicMethod("getter", staticProperty.PropertyType, Type.EmptyTypes, staticProperty.DeclaringType, true);
		// 	ILGenerator il = dm.GetILGenerator();

		// 	il.Emit(OpCodes.Call, staticProperty.GetGetMethod(true));
		// 	il.Emit(OpCodes.Ret);

		// 	return dm.CreateDelegate(asFuncType);
		// }
	}
}