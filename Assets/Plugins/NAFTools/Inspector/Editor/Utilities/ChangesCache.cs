#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using System.Reflection;
	using System.Runtime.CompilerServices;
	using UnityEditor;
	using UnityEngine;

	public class ChangesCache
	{
		static ChangesCache()
		{
			ObjectChangeEvents.changesPublished += OnChangesPublished;
			AssemblyReloadEvents.afterAssemblyReload += Clear;
		}

		private static readonly HashSet<object> hashtable = new();
		public static bool LogChanges { get; set; } = true;

		public static event System.Action? OnClear;

		public static bool CacheMiss(object key)
		{
			if (hashtable.Contains(key))
				return false;

			hashtable.Add(key);
			return true;
		}

		public static void Clear()
		{
			hashtable.Clear();
			OnClear?.Invoke();
		}

		static void OnChangesPublished(ref ObjectChangeEventStream stream)
		{
			if (stream.length == 0)
				return;

			Clear();

			if (!LogChanges)
				return;

			for (int i = 0; i < stream.length; ++i)
			{
				var type = stream.GetEventType(i);
				switch (type)
				{
					case ObjectChangeKind.ChangeScene:
						stream.GetChangeSceneEvent(i, out var changeSceneEvent);
						Debug.Log($"{type}: {changeSceneEvent.scene}");
						break;

					case ObjectChangeKind.CreateGameObjectHierarchy:
						stream.GetCreateGameObjectHierarchyEvent(i, out var createGameObjectHierarchyEvent);
						var newGameObject = EditorUtility.InstanceIDToObject(createGameObjectHierarchyEvent.instanceId) as GameObject;
						Debug.Log($"{type}: {newGameObject} in scene {createGameObjectHierarchyEvent.scene}.");
						break;

					case ObjectChangeKind.ChangeGameObjectStructureHierarchy:
						stream.GetChangeGameObjectStructureHierarchyEvent(i, out var changeGameObjectStructureHierarchy);
						var gameObject = EditorUtility.InstanceIDToObject(changeGameObjectStructureHierarchy.instanceId) as GameObject;
						Debug.Log($"{type}: {gameObject} in scene {changeGameObjectStructureHierarchy.scene}.");
						break;

					case ObjectChangeKind.ChangeGameObjectStructure:
						stream.GetChangeGameObjectStructureEvent(i, out var changeGameObjectStructure);
						var gameObjectStructure = EditorUtility.InstanceIDToObject(changeGameObjectStructure.instanceId) as GameObject;
						Debug.Log($"{type}: {gameObjectStructure} in scene {changeGameObjectStructure.scene}.");
						break;

					case ObjectChangeKind.ChangeGameObjectParent:
						stream.GetChangeGameObjectParentEvent(i, out var changeGameObjectParent);
						var gameObjectChanged = EditorUtility.InstanceIDToObject(changeGameObjectParent.instanceId) as GameObject;
						var newParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.newParentInstanceId) as GameObject;
						var previousParentGo = EditorUtility.InstanceIDToObject(changeGameObjectParent.previousParentInstanceId) as GameObject;
						Debug.Log($"{type}: {gameObjectChanged} from {previousParentGo} to {newParentGo} from scene {changeGameObjectParent.previousScene} to scene {changeGameObjectParent.newScene}.");
						break;

					case ObjectChangeKind.ChangeGameObjectOrComponentProperties:
						stream.GetChangeGameObjectOrComponentPropertiesEvent(i, out var changeGameObjectOrComponent);
						var goOrComponent = EditorUtility.InstanceIDToObject(changeGameObjectOrComponent.instanceId);
						if (goOrComponent is GameObject go)
						{
							Debug.Log($"{type}: GameObject {go} change properties in scene {changeGameObjectOrComponent.scene}.");
						}
						else if (goOrComponent is Component component)
						{
							Debug.Log($"{type}: Component {component} change properties in scene {changeGameObjectOrComponent.scene}.");
						}
						break;

					case ObjectChangeKind.DestroyGameObjectHierarchy:
						stream.GetDestroyGameObjectHierarchyEvent(i, out var destroyGameObjectHierarchyEvent);
						// The destroyed GameObject can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.
						var destroyParentGo = EditorUtility.InstanceIDToObject(destroyGameObjectHierarchyEvent.parentInstanceId) as GameObject;
						Debug.Log($"{type}: {destroyGameObjectHierarchyEvent.instanceId} with parent {destroyParentGo} in scene {destroyGameObjectHierarchyEvent.scene}.");
						break;

					case ObjectChangeKind.CreateAssetObject:
						stream.GetCreateAssetObjectEvent(i, out var createAssetObjectEvent);
						var createdAsset = EditorUtility.InstanceIDToObject(createAssetObjectEvent.instanceId);
						var createdAssetPath = AssetDatabase.GUIDToAssetPath(createAssetObjectEvent.guid);
						Debug.Log($"{type}: {createdAsset} at {createdAssetPath} in scene {createAssetObjectEvent.scene}.");
						break;

					case ObjectChangeKind.DestroyAssetObject:
						stream.GetDestroyAssetObjectEvent(i, out var destroyAssetObjectEvent);
						// The destroyed asset can not be converted with EditorUtility.InstanceIDToObject as it has already been destroyed.
						Debug.Log($"{type}: Instance Id {destroyAssetObjectEvent.instanceId} with Guid {destroyAssetObjectEvent.guid} in scene {destroyAssetObjectEvent.scene}.");
						break;

					case ObjectChangeKind.ChangeAssetObjectProperties:
						stream.GetChangeAssetObjectPropertiesEvent(i, out var changeAssetObjectPropertiesEvent);
						var changeAsset = EditorUtility.InstanceIDToObject(changeAssetObjectPropertiesEvent.instanceId);
						var changeAssetPath = AssetDatabase.GUIDToAssetPath(changeAssetObjectPropertiesEvent.guid);
						Debug.Log($"{type}: {changeAsset} at {changeAssetPath} in scene {changeAssetObjectPropertiesEvent.scene}.");
						break;

					case ObjectChangeKind.UpdatePrefabInstances:
						stream.GetUpdatePrefabInstancesEvent(i, out var updatePrefabInstancesEvent);
						string s = "";
						s += $"{type}: scene {updatePrefabInstancesEvent.scene}. Instances ({updatePrefabInstancesEvent.instanceIds.Length}):\n";
						foreach (var prefabId in updatePrefabInstancesEvent.instanceIds)
						{
							s += EditorUtility.InstanceIDToObject(prefabId).ToString() + "\n";
						}
						Debug.Log(s);
						break;
				}
			}
		}
	}
}