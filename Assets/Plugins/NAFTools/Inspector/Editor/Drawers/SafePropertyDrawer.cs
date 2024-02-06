/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
#if UNITY_EDITOR
namespace NAF.Inspector.Editor
{
	using System;
	using System.Reflection;
	using System.Threading.Tasks;
	using NAF.ExpressionCompiler;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	public interface INAFDrawer
	{
		void Initialize(PropertyAttribute attribute, FieldInfo fieldInfo);
		Task OnEnable(SerializedProperty property);
		void Update(SerializedObject serializedObject);
		void OnDisable();
	}

	public abstract class NAFPropertyDrawer : PropertyDrawer, INAFDrawer
	{
		// private static Action<PropertyDrawer, PropertyAttribute> SetAttributeField = null;
		// private static Action<PropertyDrawer, FieldInfo> SetFieldInfo = null;

		public NAFPropertyDrawer()
		{
			UnityEngine.Debug.Log("NAFPropertyDrawer!" + this.GetType().Name);
		}

		public void Initialize(PropertyAttribute attribute, FieldInfo fieldInfo)
		{
			// (SetAttributeField ??= (Action<PropertyDrawer, PropertyAttribute>)EmitUtils.FieldSetter(typeof(PropertyDrawer), "m_Attribute"))
			// 	(this, attribute);

			// (SetFieldInfo ??= (Action<PropertyDrawer, FieldInfo>)EmitUtils.FieldSetter(typeof(PropertyDrawer), "m_FieldInfo"))
			// 	(this, fieldInfo);
		}

		private string _path;

		public Task OnEnable(SerializedProperty property)
		{
			_path = property.propertyPath;
			try {
				if (attribute is IArrayPropertyAttribute a)
				{
					if ((fieldInfo == null || !fieldInfo.FieldType.IsArrayOrList()) && !a.DrawOnField)
						throw new InvalidOperationException($"Attribute {attribute.GetType().Name} can only be used on array fields.");
					else if (!a.DrawOnElements && !a.DrawOnArray)
						throw new InvalidOperationException($"Attribute {attribute.GetType().Name} extends '{nameof(IArrayPropertyAttribute)}' but does not draw on the array nor the elements.");
				}

				Task result = TryEnable(property);
				enableException = false;
				_exception = null;
				return result;
			}
			catch (Exception e)
			{
				enableException = true;
				_exception = e;
				return Task.CompletedTask;
			}
		}

		public virtual Task TryEnable(in SerializedProperty property) { return Task.CompletedTask; }

		private bool enableException;
		private Exception _exception;


		public void Update(SerializedObject serializedObject)
		{
			if (enableException && _exception != null)
				return;

			try {
				SerializedProperty property = serializedObject.FindProperty(_path);
				TryUpdate(property);
				_exception = null;
			}
			catch (Exception e)
			{
				_exception = e;
			}
		}

		public virtual void TryUpdate(SerializedProperty property) {}

		public static Font _monospace;
		public static Font MonoSpace => _monospace ??= EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf") as Font;

		public static void ErrorGUI(Rect position, GUIContent label, Exception e)
		{
			position = EditorGUI.PrefixLabel(position, label);

			GUIContent error = TempUtility.Content(e.Message, EditorIcons.Collab_FileConflict, e.Message);
			InlineGUI.LimitText(error, 80);

			// Change the label/tooltip style to monospace
			GUIStyle style = new GUIStyle(EditorStyles.helpBox)
			{
				font = MonoSpace
			};

			using (IndentScope.Zero)
			using (DisabledScope.False)
				if (GUI.Button(position, error, style))
					UnityEngine.Debug.LogException(e);
		}

		public static float ErrorHeight()
		{
			return EditorGUIUtility.singleLineHeight * 2f;
		}

		public override float GetPropertyHeight(SerializedProperty property, GUIContent label)
		{
			if (_path == null)
				Setup(property);

			if (_exception != null)
				return ErrorHeight();

			try {
				return TryGetHeight(property, label);
			}
			catch
			{
				return ErrorHeight();
			}
		}

		private void Setup(SerializedProperty property)
		{
			try {
				ArrayDrawer.Current.customDrawers.Add(this);
				TryEnable(property).Wait();
			}
			catch (Exception e)
			{
				_exception = e;
				enableException = true;
			}
		}


		public override void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (_path == null)
				Setup(property);

			if (_exception != null)
			{
				ErrorGUI(position, label, _exception);
				return;
			}

			try {
				TryOnGUI(position, property, label);
			}
			catch (System.Exception e)
			{
				ErrorGUI(position, label, e);
			}
		}

		public abstract float TryGetHeight(SerializedProperty property, GUIContent label);
		public abstract void TryOnGUI(Rect position, SerializedProperty property, GUIContent label);

		public virtual void OnDisable() { }
	}

	public abstract class NAFDecoratorDrawer : DecoratorDrawer, INAFDrawer
	{
		private FieldInfo _fieldInfo;
		public FieldInfo fieldInfo => _fieldInfo;

		public void Initialize(PropertyAttribute attribute, FieldInfo fieldInfo)
		{
			_fieldInfo = fieldInfo;
		}

		private string _path;

		public Task OnEnable(SerializedProperty property)
		{
			_path = property.propertyPath;
			try {
				Task result = TryEnable(property);
				enableException = false;
				_exception = null;
				return result;
			}
			catch (Exception e)
			{
				enableException = true;
				_exception = e;
				return Task.CompletedTask;
			}
		}

		public virtual Task TryEnable(SerializedProperty property) { return Task.CompletedTask; }

		private bool enableException;
		private Exception _exception;

		public void Update(SerializedObject serializedObject)
		{
			if (enableException && _exception != null)
				return;

			try {
				TryUpdate(serializedObject.FindProperty(_path));
				_exception = null;
			}
			catch (Exception e)
			{
				_exception = e;
			}
		}

		public virtual void TryUpdate(SerializedProperty property) {}

		public override float GetHeight()
		{
			if (_exception != null)
				return NAFPropertyDrawer.ErrorHeight();

			try {
				return TryGetHeight();
			}
			catch
			{
				return NAFPropertyDrawer.ErrorHeight();
			}
		}

		public override void OnGUI(Rect position)
		{
			if (_exception != null)
			{
				NAFPropertyDrawer.ErrorGUI(position, GUIContent.none, _exception);
				return;
			}

			try {
				TryOnGUI(position);
			}
			catch (System.Exception e)
			{
				NAFPropertyDrawer.ErrorGUI(position, GUIContent.none, e);
			}
		}

		public abstract float TryGetHeight();
		public abstract void TryOnGUI(Rect position);

		public virtual void OnDisable() { }
	}
}
#endif