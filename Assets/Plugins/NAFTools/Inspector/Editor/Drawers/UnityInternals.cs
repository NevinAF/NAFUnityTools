
namespace NAF.Inspector.Editor
{
	using System;
	using System.Linq.Expressions;
	using System.Reflection;
	using NAF.ExpressionCompiler;
	using UnityEditor;
	using UnityEngine;

	public static class UnityInternals
	{
		public readonly static Assembly EditorAssembly = typeof(Editor).Assembly;
		public readonly static Type ScriptAttributeUtilityType = EditorAssembly.GetType("UnityEditor.ScriptAttributeUtility")!;
		private const float kIndentPerLevel = 15;

		public static float EditorGUI_indent => EditorGUI.indentLevel * kIndentPerLevel;

		// UnityEditor.ScriptAttributeUtility:
		// internal static FieldInfo GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
		private delegate FieldInfo GetFieldInfoAndStaticTypeFromPropertyDelegate(SerializedProperty property, out Type type);
		private static GetFieldInfoAndStaticTypeFromPropertyDelegate _scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty;
		public static FieldInfo ScriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(SerializedProperty property, out Type type)
		{
			if (_scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty == null)
			{
				MethodInfo method = ScriptAttributeUtilityType.GetMethod("GetFieldInfoAndStaticTypeFromProperty", BindingFlags.NonPublic | BindingFlags.Static);
				EmitUtils.BoxedMember(method, out _scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty);
			}

			return _scriptAttributeUtility_GetFieldInfoAndStaticTypeFromProperty(property, out type);
		}

		// UnityEditor.ScriptAttributeUtility:
		// internal static Type GetDrawerTypeForPropertyAndType(SerializedProperty property, Type type)
		private static Func<SerializedProperty, Type, Type> _scriptAttributeUtility_GetDrawerTypeForPropertyAndType;
		public static Type ScriptAttributeUtility_GetDrawerTypeForPropertyAndType(SerializedProperty property, Type type)
		{
			if (_scriptAttributeUtility_GetDrawerTypeForPropertyAndType == null)
			{
				MethodInfo method = ScriptAttributeUtilityType.GetMethod("GetDrawerTypeForPropertyAndType", BindingFlags.NonPublic | BindingFlags.Static);
				EmitUtils.BoxedMember(method, out _scriptAttributeUtility_GetDrawerTypeForPropertyAndType);
			}

			return _scriptAttributeUtility_GetDrawerTypeForPropertyAndType(property, type);
		}


		// CustomPropertyDrawer:
		// CustomPropertyDrawer_m_Type_AND_m_UseForChildren
		private static (FieldInfo, FieldInfo) _customPropertyDrawer_m_Type_AND_m_UseForChildren;
		public static (Type, bool) CustomPropertyDrawer_m_Type_AND_m_UseForChildren(CustomPropertyDrawer drawer)
		{
			if (_customPropertyDrawer_m_Type_AND_m_UseForChildren == default)
			{
				Type customPropertyDrawerType = typeof(CustomPropertyDrawer);
				FieldInfo m_Type = customPropertyDrawerType.GetField("m_Type", BindingFlags.NonPublic | BindingFlags.Instance);
				FieldInfo m_UseForChildren = customPropertyDrawerType.GetField("m_UseForChildren", BindingFlags.NonPublic | BindingFlags.Instance);
				_customPropertyDrawer_m_Type_AND_m_UseForChildren = (m_Type, m_UseForChildren);
			}

			return (
				(Type)_customPropertyDrawer_m_Type_AND_m_UseForChildren.Item1.GetValue(drawer),
				(bool)_customPropertyDrawer_m_Type_AND_m_UseForChildren.Item2.GetValue(drawer)
			);
		}

		// EditorGUI:
		// internal static bool DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
		private static Func<Rect, SerializedProperty, GUIContent, bool> _editorGUI_DefaultPropertyField;
		public static bool EditorGUI_DefaultPropertyField(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_editorGUI_DefaultPropertyField == null)
			{
				MethodInfo defaultPropertyFieldMethod = typeof(EditorGUI).GetMethod("DefaultPropertyField", BindingFlags.NonPublic | BindingFlags.Static);
				EmitUtils.BoxedMember(defaultPropertyFieldMethod, out _editorGUI_DefaultPropertyField);
			}

			return _editorGUI_DefaultPropertyField(position, property, label);
		}

		// EditorGUI:
		// internal static float GetSinglePropertyHeight(SerializedProperty property, GUIContent label)
		private static Func<SerializedProperty, GUIContent, float> _editorGUI_GetSinglePropertyHeight;
		public static float EditorGUI_GetSinglePropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (_editorGUI_GetSinglePropertyHeight == null)
			{
				MethodInfo getSinglePropertyHeightMethod = typeof(EditorGUI).GetMethod("GetSinglePropertyHeight", BindingFlags.NonPublic | BindingFlags.Static);
				EmitUtils.BoxedMember(getSinglePropertyHeightMethod, out _editorGUI_GetSinglePropertyHeight);
			}

			return _editorGUI_GetSinglePropertyHeight(property, label);
		}

		// SerializedProperty:
		// internal bool isValid { get; }
		private static Func<SerializedProperty, bool> _serializedProperty_isValid;
		public static bool SerializedProperty_isValid(SerializedProperty property)
		{
			if (_serializedProperty_isValid == null)
			{
				PropertyInfo isValidProperty = typeof(SerializedProperty).GetProperty("isValid", BindingFlags.NonPublic | BindingFlags.Instance);
				_serializedProperty_isValid = (Func<SerializedProperty, bool>)Delegate.CreateDelegate(typeof(Func<SerializedProperty, bool>), isValidProperty.GetMethod!);
			}

			return _serializedProperty_isValid(property);
		}

		// ScriptAttributeUtility:
		// internal static PropertyHandlerCache propertyHandlerCache { get; }
		private static Func<object> _scriptAttributeUtility_propertyHandlerCache;
		public static object ScriptAttributeUtility_propertyHandlerCache
		{
			get
			{
				if (_scriptAttributeUtility_propertyHandlerCache == null)
				{
					PropertyInfo property = ScriptAttributeUtilityType.GetProperty("propertyHandlerCache", BindingFlags.NonPublic | BindingFlags.Static);
					EmitUtils.BoxedMember(property, out _scriptAttributeUtility_propertyHandlerCache);
				}

				return _scriptAttributeUtility_propertyHandlerCache();
			}
		}

		// PropertyHandlerCache:
		// internal void SetHandler(SerializedProperty property, PropertyHandler handler)
		private static Action<object, SerializedProperty, object> _propertyHandlerCache_SetHandler;
		public static void PropertyHandlerCache_SetHandler(object cache, SerializedProperty property, object handler)
		{
			if (_propertyHandlerCache_SetHandler == null)
			{
				Type propertyHandlerCacheType = cache.GetType();
				MethodInfo setHandlerMethod = propertyHandlerCacheType.GetMethod("SetHandler", BindingFlags.NonPublic | BindingFlags.Instance);
				EmitUtils.BoxedMember(setHandlerMethod, out _propertyHandlerCache_SetHandler);
			}

			_propertyHandlerCache_SetHandler(cache, property, handler);
		}

		// Editor:
		// private readonly PropertyHandlerCache m_PropertyHandlerCache;
		private static FieldInfo _editor_m_PropertyHandlerCache;
		public static object Editor_m_PropertyHandlerCache(Editor editor)
		{
			if (_editor_m_PropertyHandlerCache == null)
			{
				Type editorType = typeof(Editor);
				_editor_m_PropertyHandlerCache = editorType.GetField("m_PropertyHandlerCache", BindingFlags.NonPublic | BindingFlags.Instance);
			}

			return _editor_m_PropertyHandlerCache.GetValue(editor);
		}

		private static Type _propertyHandlerType;
		public static Type PropertyHandlerType
		{
			get
			{
				if (_propertyHandlerType == null)
				{
					_propertyHandlerType = EditorAssembly.GetType("UnityEditor.PropertyHandler");
					if (_propertyHandlerType == null)
						throw new InvalidOperationException("Type UnityEditor.PropertyHandler not found.");
				}

				return _propertyHandlerType;
			}
		}

		// PropertyHandler:
		// public void HandleAttribute(SerializedProperty property, PropertyAttribute attribute, FieldInfo field, Type propertyType)
		private static Action<object, SerializedProperty, PropertyAttribute, FieldInfo, Type> _propertyDrawer_HandleAttribute;
		public static void PropertyDrawer_HandleAttribute(object drawer, SerializedProperty property, PropertyAttribute attribute, FieldInfo field, Type propertyType)
		{
			if (_propertyDrawer_HandleAttribute == null)
			{
				MethodInfo handleAttributeMethod = PropertyHandlerType.GetMethod("HandleAttribute", BindingFlags.Public | BindingFlags.Instance);
				EmitUtils.BoxedMember(handleAttributeMethod, out _propertyDrawer_HandleAttribute);
			}

			_propertyDrawer_HandleAttribute(drawer, property, attribute, field, propertyType);
		}

		// PropertyHandler:
		// public void HandleDrawnType(SerializedProperty property, Type drawnType, Type propertyType, FieldInfo field, PropertyAttribute attribute)
		private static Action<object, SerializedProperty, Type, Type, FieldInfo, PropertyAttribute> _propertyDrawer_HandleDrawnType;
		public static void PropertyDrawer_HandleDrawnType(object drawer, SerializedProperty property, Type drawnType, Type propertyType, FieldInfo field, PropertyAttribute attribute)
		{
			if (_propertyDrawer_HandleDrawnType == null)
			{
				MethodInfo handleDrawnTypeMethod = PropertyHandlerType.GetMethod("HandleDrawnType", BindingFlags.Public | BindingFlags.Instance) ??
					throw new InvalidOperationException("Method HandleDrawnType not found.");
				EmitUtils.BoxedMember(handleDrawnTypeMethod, out _propertyDrawer_HandleDrawnType);
			}

			_propertyDrawer_HandleDrawnType(drawer, property, drawnType, propertyType, field, attribute);
		}

		// PropertyHandler:
		// public bool OnGUI(Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
		private static Func<object, Rect, SerializedProperty, GUIContent, bool, bool> _propertyDrawer_OnGUI;
		public static bool PropertyDrawer_OnGUI(object drawer, Rect position, SerializedProperty property, GUIContent label, bool includeChildren)
		{
			if (_propertyDrawer_OnGUI == null)
			{
				MethodInfo onGUIMethod = PropertyHandlerType.GetMethod("OnGUI", BindingFlags.Public | BindingFlags.Instance);
				EmitUtils.BoxedMember(onGUIMethod, out _propertyDrawer_OnGUI);
			}

			return _propertyDrawer_OnGUI(drawer, position, property, label, includeChildren);
		}

		// PropertyHandler:
		// public float GetHeight(SerializedProperty property, GUIContent label, bool includeChildren)
		private static Func<object, SerializedProperty, GUIContent, bool, float> _propertyDrawer_GetHeight;
		public static float PropertyDrawer_GetHeight(object drawer, SerializedProperty property, GUIContent label, bool includeChildren = true)
		{
			if (_propertyDrawer_GetHeight == null)
			{
				MethodInfo getHeightMethod = PropertyHandlerType.GetMethod("GetHeight", BindingFlags.Public | BindingFlags.Instance);
				EmitUtils.BoxedMember(getHeightMethod, out _propertyDrawer_GetHeight);
			}

			return _propertyDrawer_GetHeight(drawer, property, label, includeChildren);
		}

		// AudioUtil:
		// public static bool HasAudioCallback(MonoBehaviour behaviour)
		private static Func<MonoBehaviour, bool> _audioUtil_HasAudioCallback;
		public static bool AudioUtil_HasAudioCallback(MonoBehaviour behaviour)
		{
			if (_audioUtil_HasAudioCallback == null)
			{
				Type audioUtilType = EditorAssembly.GetType("UnityEditor.AudioUtil");
				MethodInfo hasAudioCallbackMethod = audioUtilType.GetMethod("HasAudioCallback", BindingFlags.Public | BindingFlags.Static);
				EmitUtils.BoxedMember(hasAudioCallbackMethod, out _audioUtil_HasAudioCallback);
			}

			return _audioUtil_HasAudioCallback(behaviour);
		}

		// AudioUtil:
		// public static int GetCustomFilterChannelCount(MonoBehaviour behaviour)
		private static Func<MonoBehaviour, int> _audioUtil_GetCustomFilterChannelCount;
		public static int AudioUtil_GetCustomFilterChannelCount(MonoBehaviour behaviour)
		{
			if (_audioUtil_GetCustomFilterChannelCount == null)
			{
				Type audioUtilType = EditorAssembly.GetType("UnityEditor.AudioUtil");
				MethodInfo getCustomFilterChannelCountMethod = audioUtilType.GetMethod("GetCustomFilterChannelCount", BindingFlags.Public | BindingFlags.Static);
				EmitUtils.BoxedMember(getCustomFilterChannelCountMethod, out _audioUtil_GetCustomFilterChannelCount);
			}

			return _audioUtil_GetCustomFilterChannelCount(behaviour);
		}

		// Editor:
		// private AudioFilterGUI m_AudioFilterGUI;
		// AudioFilterGUI:
		// public void DrawAudioFilterGUI(MonoBehaviour behaviour)
		// Code::
		// 	if (m_AudioFilterGUI == null)
		// 	{
		// 		m_AudioFilterGUI = new AudioFilterGUI();
		// 	}
		// 	m_AudioFilterGUI.DrawAudioFilterGUI(monoBehaviour);
		private static Action<Editor, MonoBehaviour> _editor_DrawAudioFilter;
		public static void Editor_DrawAudioFilter(Editor editor, MonoBehaviour behaviour)
		{
			if (_editor_DrawAudioFilter == null)
			{
				Type editorType = typeof(Editor);
				FieldInfo m_AudioFilterGUI = editorType.GetField("m_AudioFilterGUI", BindingFlags.NonPublic | BindingFlags.Instance);
				ParameterExpression editorParameter = Expression.Parameter(editorType, "editor");
				ParameterExpression behaviourParameter = Expression.Parameter(typeof(MonoBehaviour), "behaviour");

				ConstructorInfo audioFilterGUIConstructor = m_AudioFilterGUI.FieldType.GetConstructor(Type.EmptyTypes);
				MethodInfo drawAudioFilterGUIMethod = m_AudioFilterGUI.FieldType.GetMethod("DrawAudioFilterGUI", BindingFlags.Public | BindingFlags.Instance);

				MemberExpression m_AudioFilterGUIAccess = Expression.Field(editorParameter, m_AudioFilterGUI);
				// if member is null, create new instance
				var ifThen = Expression.IfThen(
					Expression.Equal(m_AudioFilterGUIAccess, Expression.Constant(null)),
					Expression.Assign(m_AudioFilterGUIAccess, Expression.New(audioFilterGUIConstructor))
				);
				var call = Expression.Call(m_AudioFilterGUIAccess, drawAudioFilterGUIMethod, behaviourParameter);
				var block = Expression.Block(ifThen, call);

				_editor_DrawAudioFilter = Expression.Lambda<Action<Editor, MonoBehaviour>>(
					block,
					editorParameter
				).Compile();
			}

			_editor_DrawAudioFilter(editor, behaviour);
		}

		// EditorGUIUtility:
		// currentViewWidth
		private static Func<float> _editorGUIUtility_get_s_OverriddenViewWidth;	
		private static Action<float> _editorGUIUtility_set_s_OverriddenViewWidth;
		public static float EditorGUIUtility_s_OverriddenViewWidth
		{
			get
			{
				if (_editorGUIUtility_get_s_OverriddenViewWidth == null || _editorGUIUtility_set_s_OverriddenViewWidth == null)
				{
					var field = typeof(EditorGUIUtility).GetField("s_OverriddenViewWidth", BindingFlags.NonPublic | BindingFlags.Static);
					EmitUtils.BoxedMember(field, out _editorGUIUtility_get_s_OverriddenViewWidth);
					EmitUtils.BoxedMember(field, out _editorGUIUtility_set_s_OverriddenViewWidth);
				}

				return _editorGUIUtility_get_s_OverriddenViewWidth();
			}
			set
			{
				if (_editorGUIUtility_get_s_OverriddenViewWidth == null || _editorGUIUtility_set_s_OverriddenViewWidth == null)
				{
					var field = typeof(EditorGUIUtility).GetField("s_OverriddenViewWidth", BindingFlags.NonPublic | BindingFlags.Static);
					EmitUtils.BoxedMember(field, out _editorGUIUtility_get_s_OverriddenViewWidth);
					EmitUtils.BoxedMember(field, out _editorGUIUtility_set_s_OverriddenViewWidth);
				}

				_editorGUIUtility_set_s_OverriddenViewWidth(value);
			}
		}

		// EditorGUIUtility:
		// internal static Texture2D LoadIcon(string name)
		private static Func<string, Texture2D> _editorGUIUtility_LoadIcon;
		public static Texture2D EditorGUIUtility_LoadIcon(string name)
		{
			if (_editorGUIUtility_LoadIcon == null)
			{
				MethodInfo loadIconMethod = typeof(EditorGUIUtility).GetMethod("LoadIcon", BindingFlags.NonPublic | BindingFlags.Static);
				EmitUtils.BoxedMember(loadIconMethod, out _editorGUIUtility_LoadIcon);
			}

			return _editorGUIUtility_LoadIcon(name);
		}
	}
}