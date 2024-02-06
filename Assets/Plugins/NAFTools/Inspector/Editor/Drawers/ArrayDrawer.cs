
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Diagnostics.CodeAnalysis;
	using System.Linq;
	using System.Linq.Expressions;
	using System.Reflection;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;
	using NAF.ExpressionCompiler;
	using System.Threading.Tasks;
	using System.Threading;

	// // Draws all array properties with a custom drawer, everything else is drawn normally
	[CanEditMultipleObjects]
	// [CustomEditor(typeof(UnityEngine.Object), true)]
	public class ArrayDrawer : UnityEditor.Editor
	{
		private bool injected = false;
		protected virtual PropertyAttribute[] GetAdditionalDrawers() => null;

		public List<INAFDrawer> customDrawers = new List<INAFDrawer>();
	
		public static ArrayDrawer Current { get; private set; }

		private bool loading = true;

		private void OnEnable()
		{
			AssemblyReloadEvents.afterAssemblyReload += OnAfterAssemblyReload;
			ChangesCache.OnClear += InvalidateDrawers;
			injected = false;
		}

		private void OnDisable()
		{
			AssemblyReloadEvents.afterAssemblyReload -= OnAfterAssemblyReload;
			ChangesCache.OnClear -= InvalidateDrawers;
			ClearDrawers();
			cts?.Cancel();
		}

		private void ClearDrawers()
		{
			foreach (var drawer in customDrawers)
				drawer.OnDisable();
			customDrawers.Clear();
		}

		// Cancel token
		private CancellationTokenSource cts;

		private void OnAfterAssemblyReload()
		{
			injected = false;
		}

		private void Injectish()
		{
			loading = true;
			if (cts != null) cts.Cancel();
			else cts = new CancellationTokenSource();

			ClearDrawers();
			Task task = InjectArrayProperties(serializedObject, GetAdditionalDrawers());
			InvalidateDrawers();

			task.ContinueWith(t =>
			{
				if (t.IsFaulted) Debug.LogError(t.Exception);
				else if (!t.IsCanceled)
				{
					loading = false;
					Repaint();
				}
			}, cts.Token);
		}

		private bool _drawersUpdated = false;

		private void InvalidateDrawers()
		{
			_drawersUpdated = false;
		}

		private void UpdateDrawers()
		{
			foreach (var drawer in customDrawers)
				drawer.Update(serializedObject);
			_drawersUpdated = true;
		}
	
		public override void OnInspectorGUI()
		{
			if (target == null || serializedObject == null)
				return;

			if (!injected)
			{
				injected = true;
				Injectish();
			}

			if (loading)
			{
				EditorGUILayout.LabelField("Loading...");
				return;
			}

			if (!_drawersUpdated)
				UpdateDrawers();

			// Update the inspector to reflect the left padding
			Current = this;
			base.OnInspectorGUI();
			Current = null;
		}

		public Task InjectArrayProperties(SerializedObject serializedObject, PropertyAttribute[] additionalDrawers = null)
		{
			var iterator = serializedObject.GetIterator();

			bool hasAdditionalDrawers = additionalDrawers != null && additionalDrawers.Length > 0;
			List<Task> tasks = new List<Task>();

			// Iterate over all properties, and inject property handlers for any 
			while (iterator.NextVisible(true))
			{
				if (iterator.IsElementOfArray())
				{
					if (!iterator.propertyPath.EndsWith(".Array.data[0]"))
						// Skip to the end of the array.
						do { if (!iterator.NextVisible(false)) break; }
						while(iterator.IsElementOfArray());
					else continue;
				}

				bool isArray = iterator.isArray && iterator.propertyType != SerializedPropertyType.String;
				if (hasAdditionalDrawers || isArray)
					tasks.Add(InjectPropertyHandler(iterator, isArray, additionalDrawers));
			}

			return Task.WhenAll(tasks);
		}

		private static Func<UnityEditor.Editor, object> editor_propertyHandlerCache;
		private static Action<object, SerializedProperty, object> propertyCache_SetHandler;
		private static Func<SerializedProperty, FieldInfo> scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty;
		private static Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type> propertyHandler_HandleAttribute;
		private static Func<object, List<PropertyDrawer>> propertyHandler_PropertyDrawers;
		private static Func<object, List<DecoratorDrawer>> propertyHandler_DecoratorDrawers;
		private static Type propertyHandlerType;

		public Task InjectPropertyHandler([NotNull] SerializedProperty property, bool isArray = false, PropertyAttribute[] additionalDrawers = null)
		{
			if (propertyHandlerType == null)
				CompileReflectionTargets();

			int precount = customDrawers.Count;

			property = property.Copy();

			object propertyCache = editor_propertyHandlerCache(this);
			FieldInfo fieldInfo = scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(property);

			object propertyHandler = Activator.CreateInstance(propertyHandlerType);
			var propertyAttributes = GetFieldAttributes(fieldInfo, additionalDrawers);


			if (propertyAttributes != null)
			{
				for (int i = 0; i < propertyAttributes.Count; i++)
				{
					var attribute = propertyAttributes[i];

					// propertyType is solely used for filtering out array properties, which we don't want to do if the attribute is an array property attribute.
					Type propertyType = fieldInfo?.FieldType;

					if (propertyType != null && propertyType.IsArrayOrList() && attribute is IArrayPropertyAttribute arrayAttribute)
					{
						if (property.IsElementOfArray()) {
							if (!arrayAttribute.DrawOnElements) continue;
						}
						else {
							if (!arrayAttribute.DrawOnArray) continue;
							propertyType = null;
						}
					}

					propertyHandler_HandleAttribute(propertyHandler, property, attribute, fieldInfo, propertyType);
				}
			}

			List<PropertyDrawer> drawers = propertyHandler_PropertyDrawers(propertyHandler);
			List<DecoratorDrawer> decorators = propertyHandler_DecoratorDrawers(propertyHandler);

			if (drawers != null)
			{
				foreach (var drawer in drawers)
				{
					if (drawer is INAFDrawer customDrawer)
					{
						customDrawer.Initialize(drawer.attribute, fieldInfo);
						customDrawers.Add(customDrawer);
					}
				}
			}

			if (decorators != null)
			{
				foreach (var decorator in decorators)
				{
					if (decorator is INAFDrawer customDrawer)
					{
						customDrawer.Initialize(decorator.attribute, fieldInfo);
						customDrawers.Add(customDrawer);
					}
				}
			}

			propertyCache_SetHandler(propertyCache, property, propertyHandler);

			int addedCount = customDrawers.Count - precount;
			if (addedCount == 0)
				return Task.CompletedTask;

			Task[] tasks = new Task[addedCount];
			for (int i = 0; i < addedCount; i++)
				tasks[i] = customDrawers[precount + i].OnEnable(property);


			return Task.WhenAll(tasks);
		}

		private static List<PropertyAttribute> GetFieldAttributes(FieldInfo field, PropertyAttribute[] additionalDrawers)
		{

			Comparer<PropertyAttribute> comparer = null;
			List<PropertyAttribute> propertyAttributeList = null;
			if (field != null)
			{
				var attrs = field.GetCustomAttributes<PropertyAttribute>(true);
				foreach (PropertyAttribute attribute in attrs)
				{
					propertyAttributeList ??= new List<PropertyAttribute>();
					comparer ??= Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));

					propertyAttributeList.Add(attribute);
				}
			}

			if (additionalDrawers != null)
			{
				foreach (PropertyAttribute attribute in additionalDrawers)
				{
					propertyAttributeList ??= new List<PropertyAttribute>();
					comparer ??= Comparer<PropertyAttribute>.Create((p1, p2) => p1.order.CompareTo(p2.order));

					propertyAttributeList.Add(attribute);
				}
			}

			propertyAttributeList?.Sort(comparer);
			return propertyAttributeList;
		}

		public static void CompileReflectionTargets()
		{
			var bindings = BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Instance | BindingFlags.Static;

			Assembly editorAssembly = typeof(UnityEditor.Editor).Assembly;

			// ScriptAttributeUtility
			Type utilityType = editorAssembly.GetType("UnityEditor.ScriptAttributeUtility");

			// internal static PropertyHandlerCache propertyHandlerCache
			FieldInfo propertyCacheField =typeof(UnityEditor.Editor).GetField("m_PropertyHandlerCache", bindings);
			ParameterExpression editorParameter = Expression.Parameter(typeof(UnityEditor.Editor), "editor");
			Expression propertyCacheFieldAccess = Expression.Field(editorParameter, propertyCacheField);
			editor_propertyHandlerCache = Expression.Lambda<Func<UnityEditor.Editor, object>>(propertyCacheFieldAccess, editorParameter).Compile();

			// internal static void SetHandler(SerializedProperty property, PropertyHandler handler)
			MethodInfo setHandlerMethod = propertyCacheField.FieldType.GetMethod("SetHandler", bindings);

			ParameterInfo[] parameters = setHandlerMethod.GetParameters();
			propertyHandlerType = parameters[1].ParameterType;

			ParameterExpression propertyCacheParameter = Expression.Parameter(typeof(object), "propertyCache");
			ParameterExpression propertyParameter = Expression.Parameter(typeof(SerializedProperty), "property");
			ParameterExpression handlerParameter = Expression.Parameter(typeof(object), "handler");

			UnaryExpression propertyCacheCast = Expression.Convert(propertyCacheParameter, propertyCacheField.FieldType);
			UnaryExpression handlerCast = Expression.Convert(handlerParameter, propertyHandlerType);
			Expression call = Expression.Call(propertyCacheCast, setHandlerMethod, propertyParameter, handlerCast);

			propertyCache_SetHandler = Expression.Lambda<Action<object, SerializedProperty, object>>(call, propertyCacheParameter, propertyParameter, handlerParameter).Compile();

			// internal static void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo fieldInfo, Type propertyType)
			MethodInfo handleAttributeMethod = propertyHandlerType.GetMethod("HandleAttribute", bindings);

			ParameterExpression handlerParameter2 = Expression.Parameter(typeof(object), "handler");
			ParameterExpression propertyParameter2 = Expression.Parameter(typeof(SerializedProperty), "property");
			ParameterExpression attributeParameter = Expression.Parameter(typeof(PropertyAttribute), "attribute");
			ParameterExpression fieldInfoParameter = Expression.Parameter(typeof(FieldInfo), "fieldInfo");
			ParameterExpression propertyTypeParameter = Expression.Parameter(typeof(Type), "propertyType");

			UnaryExpression handlerCast2 = Expression.Convert(handlerParameter2, propertyHandlerType);
			propertyHandler_HandleAttribute = Expression.Lambda<Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type>>(
				Expression.Call(handlerCast2, handleAttributeMethod, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter),
				handlerParameter2, propertyParameter2, attributeParameter, fieldInfoParameter, propertyTypeParameter
			).Compile();

			// List<PropertyDrawer> m_PropertyDrawers;
			FieldInfo propertyDrawersField = propertyHandlerType.GetField("m_PropertyDrawers", bindings);
			ParameterExpression handlerParameter3 = Expression.Parameter(typeof(object), "handler");
			UnaryExpression handlerCast3 = Expression.Convert(handlerParameter3, propertyHandlerType);
			Expression propertyDrawersFieldAccess = Expression.Field(handlerCast3, propertyDrawersField);
			propertyHandler_PropertyDrawers = Expression.Lambda<Func<object, List<PropertyDrawer>>>(propertyDrawersFieldAccess, handlerParameter3).Compile();

			// List<DecoratorDrawer> m_DecoratorDrawers;
			FieldInfo decoratorDrawersField = propertyHandlerType.GetField("m_DecoratorDrawers", bindings);
			ParameterExpression handlerParameter4 = Expression.Parameter(typeof(object), "handler");
			UnaryExpression handlerCast4 = Expression.Convert(handlerParameter4, propertyHandlerType);
			Expression decoratorDrawersFieldAccess = Expression.Field(handlerCast4, decoratorDrawersField);
			propertyHandler_DecoratorDrawers = Expression.Lambda<Func<object, List<DecoratorDrawer>>>(decoratorDrawersFieldAccess, handlerParameter4).Compile();

			// internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
			MethodInfo getFieldInfoAndStaticTypeFromPropertyMethod = utilityType.GetMethod("GetFieldInfoAndStaticTypeFromProperty", bindings);

			ParameterExpression propertyParameter3 = Expression.Parameter(typeof(SerializedProperty), "property");
			ConstantExpression typeParameter = Expression.Constant(null, typeof(Type));

			call = Expression.Call(getFieldInfoAndStaticTypeFromPropertyMethod, propertyParameter3, typeParameter);

			scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty = Expression.Lambda<Func<SerializedProperty, FieldInfo>>(
				call,
				propertyParameter3
			).Compile();

			// UnityEngine.Debug.Log("Checking delegates: " + scriptAttributeUtility_propertyHandlerCache + " | " + propertyCache_SetHandler + " | " + scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty + " | " + propertyHandler_HandleAttribute);
		}
	}
}