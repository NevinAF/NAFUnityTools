
// namespace NAF.Inspector.Editor
// {
// 	using System;
// 	using System.Collections.Generic;
// 	using System.Diagnostics.CodeAnalysis;
// 	using System.Linq;
// 	using System.Linq.Expressions;
// 	using System.Reflection;
// 	using NAF.Inspector;
// 	using UnityEditor;
// 	using UnityEngine;

// 	public ref struct MemberContext

// 	[CanEditMultipleObjects]
// 	[CustomEditor(typeof(UnityEngine.Object), true)]
// 	public class NAFObjectEditor : UnityEditor.Editor
// 	{

// 		private bool injected = false;
// 		private PropertyAttribute[] _additionalDrawers = null;
// 		private PropertyAttribute[] AdditionalDrawers
// 		{
// 			get {
// 				_additionalDrawers ??= target.GetType().GetCustomAttributes<MemberDefinitionDrawerAttribute>().ToArray();
// 				return _additionalDrawers;
// 			}
// 		}
	
// 		public float ViewScalar = 1f;
// 		public static ArrayDrawer Current { get; private set; }
// 		public static bool ViewScaled => Current.ViewScalar < 1f && EditorGUIUtility.currentViewWidth > 520 && Current.ViewScalar > 0.05f;

// 		private void OnEnable()
// 		{
// 			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
// 			ChangesCache.OnClear += Repaint;
// 		}

// 		private void OnDisable()
// 		{
// 			AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
// 			ChangesCache.OnClear -= Repaint;
// 		}

// 		private void OnAfterAssemblyReload()
// 		{
// 			injected = false;
// 			_additionalDrawers = null;
// 		}
	
// 		public override void OnInspectorGUI()
// 		{
// 			if (!injected)
// 			{
// 				InjectArrayProperties(serializedObject, AdditionalDrawers);
// 				injected = true;
// 			}

// 			if (target == null || serializedObject == null)
// 				return;

// 			// Update the inspector to reflect the left padding
// 			Current = this;

// 			if (ViewScaled)
// 			{
// 				EditorGUILayout.BeginVertical(GUILayout.Width(EditorGUIUtility.currentViewWidth * ViewScalar));
// 				EditorGUIUtility.labelWidth = Mathf.Max(EditorGUIUtility.currentViewWidth * 0.45f * ViewScalar - 40f, 120f);
// 				base.OnInspectorGUI();
// 				EditorGUIUtility.labelWidth = 0f;
// 				GUILayout.EndVertical();
// 			}
// 			else {
// 				base.OnInspectorGUI();
// 			}

// 			Current = null;
// 		}

// 		public static void InjectArrayProperties(SerializedObject serializedObject, PropertyAttribute[] additionalDrawers = null)
// 		{
// 			var iterator = serializedObject.GetIterator();

// 			bool hasAdditionalDrawers = additionalDrawers != null && additionalDrawers.Length > 0;

// 			// Iterate over all properties, and inject property handlers for any 
// 			while (iterator.NextVisible(true))
// 			{
// 				if (iterator.IsElementOfArray())
// 				{
// 					if (!iterator.propertyPath.EndsWith(".Array.data[0]"))
// 						// Skip to the end of the array.
// 						do { if (!iterator.NextVisible(false)) return; }
// 						while(iterator.IsElementOfArray());
// 					else continue;
// 				}

// 				bool isArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String;
// 				if (hasAdditionalDrawers || isArray)
// 					InjectPropertyHandler(iterator, isArray, additionalDrawers);
// 			}
// 		}

// 		private static Func<object> scriptAttributeUtility_propertyHandlerCache;
// 		private static Action<object, SerializedProperty, object> propertyCache_SetHandler;
// 		private static Func<SerializedProperty, FieldInfo> scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty;
// 		private static Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type> propertyHandler_HandleAttribute;
// 		private static Type propertyHandlerType;

// 		public static void InjectPropertyHandler([NotNull] SerializedProperty property, bool isArray = false, PropertyAttribute[] additionalDrawers = null)
// 		{
// 			if (propertyHandlerType == null)
// 				CompileReflectionTargets();

// 			property = property.Copy();

// 			object propertyCache = scriptAttributeUtility_propertyHandlerCache();
// 			FieldInfo fieldInfo = scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(property);

// 			object propertyHandler = Activator.CreateInstance(propertyHandlerType);
// 			var propertyAttributes = GetFieldAttributes(fieldInfo, additionalDrawers);

// 			if (propertyAttributes != null)
// 			{
// 				for (int i = 0; i < propertyAttributes.Count; i++)
// 				{
// 					var attribute = propertyAttributes[i];

// 					// propertyType is solely used for filtering out array properties, which we don't want to do if the attribute is an array property attribute.
// 					Type propertyType = attribute is IArrayPropertyAttribute && isArray ? null : fieldInfo?.FieldType;

// 					propertyHandler_HandleAttribute(propertyHandler, property, attribute, fieldInfo, propertyType);
// 				}
// 			}

// 			propertyCache_SetHandler(propertyCache, property, propertyHandler);
// 		}

// 		private static List<PropertyAttribute> GetFieldAttributes(FieldInfo field, PropertyAttribute[] additionalDrawers)
// 		{

// 			Comparer<PropertyAttribute> comparer = null;
// 			List<PropertyAttribute> propertyAttributeList = null;
// 			if (field != null)
// 			{
// 				var attrs = field.GetCustomAttributes<PropertyAttribute>(true);
// 				foreach (PropertyAttribute attribute in attrs)
// 				{
// 					propertyAttributeList ??= new List<PropertyAttribute>();
// 					comparer ??= Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));

// 					propertyAttributeList.Add(attribute);
// 				}
// 			}

// 			if (additionalDrawers != null)
// 			{
// 				foreach (PropertyAttribute attribute in additionalDrawers)
// 				{
// 					propertyAttributeList ??= new List<PropertyAttribute>();
// 					comparer ??= Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));

// 					propertyAttributeList.Add(attribute);
// 				}
// 			}

// 			propertyAttributeList?.Sort(comparer);
// 			return propertyAttributeList;
// 		}

// 		public static void CompileReflectionTargets()
// 		{
// 			var bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

// 			Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;

// 			// ScriptAttributeUtility
// 			Type utilityType = editorAssembly.GetType("UnityEditor.ScriptAttributeUtility");

// 			// internal static PropertyHandlerCache propertyHandlerCache
// 			PropertyInfo propertyCacheField = utilityType.GetProperty("propertyHandlerCache", bindings);
// 			scriptAttributeUtility_propertyHandlerCache = (Func<object>)Delegate.CreateDelegate(typeof(Func<object>), null, propertyCacheField.GetGetMethod(true));

// 			// internal static void SetHandler(SerializedProperty property, PropertyHandler handler)
// 			MethodInfo setHandlerMethod = propertyCacheField.PropertyType.GetMethod("SetHandler", bindings);

// 			ParameterInfo[] parameters = setHandlerMethod.GetParameters();
// 			propertyHandlerType = parameters[1].ParameterType;

// 			ParameterExpression propertyCacheParameter = Expression.Parameter(typeof(object), "propertyCache");
// 			ParameterExpression propertyParameter = Expression.Parameter(typeof(SerializedProperty), "property");
// 			ParameterExpression handlerParameter = Expression.Parameter(typeof(object), "handler");

// 			UnaryExpression propertyCacheCast = Expression.Convert(propertyCacheParameter, propertyCacheField.PropertyType);
// 			UnaryExpression handlerCast = Expression.Convert(handlerParameter, propertyHandlerType);
// 			Expression call = Expression.Call(propertyCacheCast, setHandlerMethod, propertyParameter, handlerCast);

// 			propertyCache_SetHandler = Expression.Lambda<Action<object, SerializedProperty, object>>(call, propertyCacheParameter, propertyParameter, handlerParameter).Compile();

// 			// internal static void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo, Type propertyType)
// 			MethodInfo handleAttributeMethod = propertyHandlerType.GetMethod("HandleAttribute", bindings);

// 			ParameterExpression handlerParameter2 = Expression.Parameter(typeof(object), "handler");
// 			ParameterExpression propertyParameter2 = Expression.Parameter(typeof(SerializedProperty), "property");
// 			ParameterExpression attributeParameter = Expression.Parameter(typeof(PropertyAttribute), "attribute");
// 			ParameterExpression fieldInfoParameter = Expression.Parameter(typeof(FieldInfo), "fieldInfo");
// 			ParameterExpression propertyTypeParameter = Expression.Parameter(typeof(Type), "propertyType");

// 			UnaryExpression handlerCast2 = Expression.Convert(handlerParameter2, propertyHandlerType);
// 			propertyHandler_HandleAttribute = Expression.Lambda<Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type>>(
// 				Expression.Call(handlerCast2, handleAttributeMethod, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter),
// 				handlerParameter2, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter
// 			).Compile();

// 			// internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
// 			MethodInfo getFieldInfoAndStaticTypeFromPropertyMethod = utilityType.GetMethod("GetFieldInfoAndStaticTypeFromProperty", bindings);

// 			ParameterExpression propertyParameter3 = Expression.Parameter(typeof(SerializedProperty), "property");
// 			ConstantExpression typeParameter = Expression.Constant(null, typeof(Type));

// 			call = Expression.Call(getFieldInfoAndStaticTypeFromPropertyMethod, propertyParameter3, typeParameter);

// 			scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty = Expression.Lambda<Func<SerializedProperty, FieldInfo>>(
// 				call,
// 				propertyParameter3
// 			).Compile();

// 			// UnityEngine.Debug.Log("Checking delegates: " + scriptAttributeUtility_propertyHandlerCache + " | " + propertyCache_SetHandler + " | " + scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty + " | " + propertyHandler_HandleAttribute);
// 		}
// 	}
// }