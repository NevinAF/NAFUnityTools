#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Linq;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(AttachedAttribute))]
	public class AttachedAttributeDrawer : InlineLabelAttributeDrawer
	{
		#region Constant Content

		public readonly static GUIContent DropdownButtonContent = new GUIContent("", EditorGUIUtility.IconContent(EditorIcons.PreMatCube).image, "Attached Component. Click to show list of selectable options.");
		public readonly static GUIContent AttachedContent = new GUIContent("", EditorGUIUtility.IconContent(EditorIcons.PreMatCube).image, "Attached Component.");
		public readonly static Texture ScriptIcon = EditorGUIUtility.IconContent(EditorIcons.cs_Script_Icon).image;
		public readonly static Texture FolderIcon = EditorGUIUtility.IconContent(EditorIcons.PreMatCube).image;

		#endregion

		private Component[]? _options;

		public override void TryUpdate(SerializedProperty property)
		{
			Component target = property.serializedObject.targetObject as Component ??
				throw new Exception("The 'Attached' attribute can only be used on Component fields (like a MonoBehaviour)!");

			Type componentType = fieldInfo.FieldType.IsArray ? fieldInfo.FieldType.GetElementType()! : fieldInfo.FieldType;

			if (!typeof(Component).IsAssignableFrom(componentType))
			{
				throw new Exception("The 'Attached' attribute can only be used on fields with a component type! Currently it is being used on a '" + componentType.Name + "' field (" + property.propertyType + ").");
			}
			
			_options = null;

			if (property.serializedObject.isEditingMultipleObjects)
				return;

			Component[] options;

			AttachedAttribute attachedAttribute = (attribute as AttachedAttribute)!;
			if (attachedAttribute.Children)
			{
				options = target.GetComponentsInChildren(componentType);

				if (!attachedAttribute.Self)
					options = options.Where(c => c.gameObject != target.gameObject).ToArray();
			}
			else
			{
				options = target.GetComponents(componentType);

				if (!attachedAttribute.Self)
					options = options.Where(c => c != target).ToArray();
			}

			_options = options;

			if (property.isArray)
				UpdateArray(property);
			else
				UpdateElement(property);
		}

		public override float TryGetHeight(SerializedProperty property, GUIContent label)
		{
			return EditorGUI.GetPropertyHeight(property, label);
		}

		public override void TryOnGUI(Rect position, SerializedProperty property, GUIContent label)
		{
			if (property.isArray)
				DrawArray(position, property, label);
			else
				DrawElement(position, property, label);
		}

		#region Element Drawing

		private void UpdateElement(SerializedProperty property)
		{
			if (_options == null)
			{
				((AttachedAttribute)attribute).Style ??= EditorStyles.helpBox;
				base.TryUpdate(property); // Sets the missing style and content
				return;
			}
	
			if (property.objectReferenceValue == null || Array.IndexOf(_options, property.objectReferenceValue) == -1)
				property.objectReferenceValue = _options[0];
		}

		private void DrawElement(Rect position, SerializedProperty property, GUIContent label)
		{
			AttachedAttribute attachedAttribute = (attribute as AttachedAttribute)!;
			if (_options == null)
			{
				GUIContent multiContent = TempUtility.Content(DropdownButtonContent.text, DropdownButtonContent.image, null);
				multiContent.tooltip = "Multi object editing is not supported with using the 'Attached' attribute button.";

				using (DisabledScope.True)
					DrawAsLabel(attachedAttribute.Alignment, position, multiContent, EditorStyles.miniButton, property, label);
			}
			else if (_options.Length == 0)
			{
				DrawAsLabel(attachedAttribute.Alignment, position, content, style, property, label); // draw the missing content.
			}
			else
			{
				if (DrawAsButton(attachedAttribute.Alignment, position, DropdownButtonContent, EditorStyles.miniButton, property, label))
				{
					Rect buttonEst = new Rect(Event.current.mousePosition.x - 10, position.y, 10, position.height);
					ShowOptionsDropdown(property, buttonEst);
				}
			}
		}

		private void ShowOptionsDropdown(SerializedProperty property, Rect buttonRect)
		{
			TreeMenu menu = TreeMenu.New();
			Transform transform = (property.serializedObject.targetObject as Component)!.transform;
			string typeName = fieldInfo.FieldType.Name;

			menu.AddItem(TreeMenu.Item.FromTransformHierarchy(transform, Convert)!);

			buttonRect.position = GUIUtility.GUIToScreenPoint(buttonRect.position);
			menu.Show(buttonRect);

			return;

			void Clicked(object o)
			{
				property.objectReferenceValue = o as Component;
				property.serializedObject.ApplyModifiedProperties();
				menu?.Close();
			}

			static GUIContent Content(Component comp)
			{
				Type type = comp.GetType();
				return new(EditorGUIUtility.ObjectContent(comp, type));
			}

			TreeMenu.Item Convert(Transform child)
			{
				Component[] components = child.GetComponents(fieldInfo.FieldType);
				TreeMenu.Item item;
				GUIContent content;

				if (components.Length == 1)
				{
					content = Content(components[0]);
					item = new(content, Clicked, components[0])
					{
						selected = property.objectReferenceValue == components[0]
					};
				}
				else
				{
					content = new GUIContent(child.name);
					item = new(content);

					for (int i = 0; i < components.Length; i++)
					{
						Component component = components[i];
						item.AddItem(Content(component), Clicked, component);
						item.children![i].selected = property.objectReferenceValue == component;
					}
				}

				return item;
			}
		}

		#endregion

		#region Array Drawing

		private void UpdateArray(SerializedProperty property)
		{
			if (_options == null) return;
			var options = _options;

			bool invalid = options.Length != property.arraySize;
			if (!invalid)
			{
				SerializedProperty iterator = property.Copy();
				iterator.Next(true); // .Array
				iterator.Next(true); // .Array.size

				for (int i = 0; i < options.Length; i++)
				{
					iterator.Next(false);
					if (options[i] != iterator.objectReferenceValue)
					{
						invalid = true;
						break;
					}
				}
			}

			if (invalid)
			{
				property.ClearArray();
				for (int i = 0; i < options.Length; i++)
				{
					property.InsertArrayElementAtIndex(i);
				}

				SerializedProperty iterator = property.Copy();
				iterator.Next(true); // .Array
				iterator.Next(true); // .Array.size

				for (int i = 0; i < options.Length; i++)
				{
					iterator.Next(false);
					iterator.objectReferenceValue = options[i];
				}
			}
		}

		private void DrawArray(Rect position, SerializedProperty property, GUIContent label)
		{
			using (DisabledScope.True)
				EditorGUI.PropertyField(position, property, label, true);
		}

		#endregion
	}
}
#nullable restore