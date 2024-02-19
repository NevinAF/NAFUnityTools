/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using System.Threading.Tasks;
	using Codice.CM.Common;
	using NAF.ExpressionCompiler;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	#nullable enable

	public abstract class NAFPropertyDrawer
	{
		[Flags]
		private enum State : byte
		{
			Enabled = 1,
			Invalidated = 2,
			UsesUpdate = 4,
		}

		private static readonly Dictionary<Type, bool> _typeHasUpdate = new Dictionary<Type, bool>();
		private bool GetHasUpdate()
		{
			Type type = GetType();
			if (!_typeHasUpdate.TryGetValue(type, out bool hasUpdate))
			{
				hasUpdate = type.GetMethod("OnUpdate", BindingFlags.Instance | BindingFlags.NonPublic).DeclaringType == type;
				_typeHasUpdate.Add(type, hasUpdate);
			}

			return hasUpdate;
		}

		private ObjectPool<NAFPropertyDrawer>? m_sourcePool;
		private PropertyTree? m_tree;
		private PropertyAttribute? m_attribute;
		private Task enablingTask = Task.CompletedTask;
		private State _state;
		private int _drawCall;
		private float _cachedHeight;

		public bool LastHeightValid => _cachedHeight >= 0f && _drawCall == ArrayDrawer.Current!.LayoutDrawID && !Invalidated;
		public float LastHeight {
			get => _cachedHeight;
			private set {
				_cachedHeight = value;
				_drawCall = ArrayDrawer.Current!.LayoutDrawID;
			}
		}

		public PropertyTree Tree => m_tree ?? throw new InvalidOperationException("PropertyTree is not set.");
		public PropertyAttribute? Attribute => m_attribute;
		public FieldInfo? FieldInfo => Tree.FieldInfo;

		private bool Enabled {
			get => (_state & State.Enabled) != 0;
			set => _state = value ? _state | State.Enabled : _state & ~State.Enabled;
		}

		private bool Invalidated {
			get => (_state & State.Invalidated) != 0;
			set => _state = value ? _state | State.Invalidated : _state & ~State.Invalidated;
		}

		private bool UsesUpdate {
			get => (_state & State.UsesUpdate) != 0;
			set => _state = value ? _state | State.UsesUpdate : _state & ~State.UsesUpdate;
		}

		protected NAFPropertyDrawer()
		{
			UsesUpdate = GetHasUpdate();
			Enabled = false;
		}

		protected virtual Task OnEnable(in SerializedProperty property) => Task.CompletedTask;
		protected virtual void OnUpdate(SerializedProperty property) { }
		protected virtual void OnDisable() { }
		protected virtual void OnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = Tree.InPlaceGetHeight(property, label);
			Tree.OnGUI(position, property, label);
		}
		protected virtual void LoadingGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			position.height = EditorGUIUtility.singleLineHeight;
			EditorGUI.LabelField(position, TempUtility.Content("Loading...", EditorIcons.Loading));
			property.NextVisible(false);
		}
		
		protected virtual float OnGetHeight(SerializedProperty property, GUIContent label)
		{
			return Tree.IterateGetHeight(property, label);
		}

		protected virtual float GetLoadingHeight(SerializedProperty property, GUIContent label)
		{
			return Tree.IterateGetHeight(property, label);
		}


		private void Enable(in SerializedProperty property)
		{
			if (Enabled == true)
				return;

			if (enablingTask.IsCompleted == false)
			{
				UnityEngine.Debug.LogError("PropertyDrawer was not reset properly. Somehow trying to enable while a disable task is running.");
				return;
			}

			Enabled = true;
			Invalidated = true;
			_cachedHeight = float.MinValue;
			enablingTask = OnEnable(property);
			enablingTask.ContinueWith(RequestRepaintTask);
		}

		private void RequestRepaintTask(Task t)
		{
			// Make sure that this wasn't disabled before the task completed,
			// and that this wasn't already repainted (thus no longer invalid).
			if (!Enabled || !Invalidated)
				return;

			EditorApplication.delayCall += () =>
			{
				if (Enabled && Invalidated)
					Tree.Repaint();
			};
		}

		public void Return()
		{
			// Prevent double returns...
			if (Enabled == false)
				return; // TODO: Warn about double returns?

			Enabled = false;
			// Make sure that the task is extended to include the disable process so we can insure that the drawer does not get re-enabled before it is fully disabled.
			enablingTask = enablingTask.ContinueWith(_ => {
				try {
					OnDisable();
					m_sourcePool?.Return(this);
				}
				catch (Exception e)
				{
					UnityEngine.Debug.LogException(e);
				}
			});
		}

		public bool Invalidate()
		{
			if (!Invalidated && UsesUpdate)
			{
				Invalidated = true;
				return true;
			}

			return false;
		}

		public void DoGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (enablingTask.IsCompleted)
			{
				if (enablingTask.IsFaulted)
				{
					property.NextVisible(false);
					ErrorGUI(position, label, enablingTask.Exception);
					return;
				}
			}

			int depth = property.depth;
			string path = property.propertyPath;

			try {
				if (enablingTask.IsCompleted == false || Invalidated)
					LoadingGUI(position, property, label);
				else OnGUI(position, property, label);
			}
			catch (Exception e)
			{
				FixProperty(property, depth, path);
				ErrorGUI(position, label, e);
			}
		}

		[MethodImpl(MethodImplOptions.AggressiveInlining)]
		private void FixProperty(SerializedProperty property, int depth, string path)
		{
			// If throw exception while iterating through properties, exit to get to the end of the original property.
			if (UnityInternals.SerializedProperty_isValid(property))
			{
				if (depth > property.depth) 
				{
					while (property.NextVisible(false) && depth > property.depth);
				}
				// If the property was not moved, move to the next property.
				else if (path == property.propertyPath)
				{
					property.NextVisible(false);
				}
			}
		}

		public float DoGetHeight(SerializedProperty property, GUIContent label)
		{
			if (enablingTask.IsCompleted)
			{
				if (enablingTask.IsFaulted)
				{
					property.NextVisible(false);
					return LastHeight = ErrorHeight();
				}

				if (Invalidated)
				{
					if (UsesUpdate)
						OnUpdate(property);
					Invalidated = false;
				}
			}

			int depth = property.depth;
			string path = property.propertyPath;

			try {
				if (enablingTask.IsCompleted == false)
					return LastHeight = GetLoadingHeight(property, label);
				else return LastHeight = OnGetHeight(property, label);
			}
			catch (Exception)
			{
				FixProperty(property, depth, path);
				return LastHeight = ErrorHeight();
			}
		}

		public static NAFPropertyDrawer? Get(PropertyTree tree, in SerializedProperty property, Type? drawerForType, PropertyAttribute? attribute)
		{
			if (drawerForType == null)
				return null;

			var sourcePool = PropertyCache.GetDrawerPoolForType(drawerForType);
			if (sourcePool == null)
				return null;

			NAFPropertyDrawer drawer = sourcePool.Get();
			drawer.m_tree = tree;
			drawer.m_attribute = attribute;
			drawer.m_sourcePool = sourcePool;
			drawer.Enable(property);
			return drawer;
		}

		public static Font? _monospace;
		public static Font MonoSpace => _monospace ??= (Font)EditorGUIUtility.Load("Fonts/RobotoMono/RobotoMono-Regular.ttf");

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
	}
}