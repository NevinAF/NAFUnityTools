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
	using System.Diagnostics.CodeAnalysis;
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
		private float _cachedHeight;

		public PropertyTree Tree => m_tree ?? throw new InvalidOperationException("PropertyTree is not set.");
		public PropertyAttribute? Attribute => m_attribute;

		public bool Enabled {
			get => (_state & State.Enabled) != 0;
			private set => _state = value ? _state | State.Enabled : _state & ~State.Enabled;
		}

		public bool Invalidated {
			get => (_state & State.Invalidated) != 0;
			private set => _state = value ? _state | State.Invalidated : _state & ~State.Invalidated;
		}

		public bool UsesUpdate {
			get => (_state & State.UsesUpdate) != 0;
			private set => _state = value ? _state | State.UsesUpdate : _state & ~State.UsesUpdate;
		}

		public float LastHeight => _cachedHeight;

		protected NAFPropertyDrawer()
		{
			UsesUpdate = GetHasUpdate();
			Enabled = false;
		}

		public virtual bool EndsDrawing => false;
		public virtual bool OnlyDrawWithEditor => false;
		protected virtual Task OnEnable() => Task.CompletedTask;
		protected virtual void OnUpdate() { }
		protected virtual void OnDisable() { }

		protected virtual float OnGetHeight() => Tree.GetHeight();
		protected virtual float GetLoadingHeight() => Tree.GetHeight();

		protected virtual void OnGUI(Rect position)
		{
			position.height = Tree.GetHeight();
			Tree.OnGUI(position);
		}

		protected virtual void LoadingGUI(Rect position)
		{
			// position.height = EditorGUIUtility.singleLineHeight;
			// EditorGUI.LabelField(position, TempUtility.Content("Loading...", EditorIcons.Loading));
			position.height = Tree.GetHeight();
			Tree.OnGUI(position);
		}

		private void Enable()
		{
			if (Enabled == true)
				return;

			if (enablingTask.IsCompleted == false)
			{
				UnityEngine.Debug.LogError("PropertyDrawer was not reset properly. Somehow trying to enable while a disable task is running.");
				return;
			}

			enableTime = DateTime.Now;
			Enabled = true;
			Invalidated = true;
			enablingTask = OnEnable();

			if (!enablingTask.IsCompleted)
				enablingTask = enablingTask.Callback(RequestRepaintTask);
		}

		private DateTime enableTime;

		private void RequestRepaintTask()
		{
			UnityEngine.Debug.Log($"PropertyDrawer {GetType().Name} took {DateTime.Now - enableTime} to enable.");
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

		public void DoGUI(Rect position)
		{
			if (enablingTask.IsCompleted)
			{
				if (enablingTask.IsFaulted)
				{
					ErrorGUI(position, Tree.PropertyLabel, enablingTask.Exception);
					return;
				}
				
				DoUpdate();
			}

			try {
				if (enablingTask.IsCompleted == false || Invalidated)
					LoadingGUI(position);
				else OnGUI(position);
			}
			catch (ExitGUIException) // Stops drawing all subsequent drawers
			{
				throw;
			}
			catch (Exception e)
			{
				enablingTask = Task.FromException(e);
				ErrorGUI(position, Tree.PropertyLabel, e);
			}
		}

		public float DoGetHeight()
		{
			if (enablingTask.IsCompleted)
			{
				if (enablingTask.IsFaulted)
				{
					return _cachedHeight = ErrorHeight();
				}

				DoUpdate();
			}

			try {
				if (enablingTask.IsCompleted == false)
					return _cachedHeight = GetLoadingHeight();
				else return _cachedHeight = OnGetHeight();
			}
			catch (ExitGUIException) // Stops drawing all subsequent drawers
			{
				throw;
			}
			catch (Exception e)
			{
				enablingTask = Task.FromException(e);
				return _cachedHeight = ErrorHeight();
			}
		}

		private void DoUpdate()
		{
			if (Invalidated)
			{
				if (UsesUpdate) try
				{
					OnUpdate();
				}
				catch (Exception e)
				{
					enablingTask = Task.FromException(e);
				}
				Invalidated = false;
			}
		}

		public static bool TryGet(PropertyTree tree, Type? drawerForType, PropertyAttribute? attribute, [NotNullWhen(true)] out NAFPropertyDrawer? drawer)
		{
			drawer = Get(tree, drawerForType, attribute);
			return drawer != null;
		}

		public static NAFPropertyDrawer? Get(PropertyTree tree, Type? drawerForType, PropertyAttribute? attribute)
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

			if (tree.BlocksChildren)
			{
				UnityEngine.Debug.LogWarning("PropertyTree already has a drawer that blocks further drawing, but there are more drawers trying to be drawn after it: " + drawer.GetType().Name);
				drawer.Return();
				return null;
			}

			if (tree.Editor == null && drawer.OnlyDrawWithEditor)
			{
				drawer.Return();
				return null;
			}

			drawer.Enable();
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