#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Linq;
	using System.Threading.Tasks;
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
		private Type? componentType;

		protected override Task OnEnable()
		{
			if (Tree.FieldInfo == null)
				throw new Exception("The 'Attached' attribute can only be used on fields!");

			componentType = Tree.FieldInfo.FieldType.IsArray ? Tree.FieldInfo.FieldType.GetArrayOrListElementType()! : Tree.FieldInfo.FieldType;

			return Task.CompletedTask;
		}

		protected override void OnUpdate()
		{
			Component target = Tree.Property.serializedObject.targetObject as Component ??
				throw new Exception("The 'Attached' attribute can only be used on Component fields (like a MonoBehaviour)!");

			if (!typeof(Component).IsAssignableFrom(componentType))
			{
				throw new Exception("The 'Attached' attribute can only be used on fields with a component type! Currently it is being used on a '" + componentType!.Name + "' field (" + Tree.Property.propertyType + ").");
			}
			
			_options = null;

			if (Tree.Property.serializedObject.isEditingMultipleObjects)
				return;

			Component[] options;

			AttachedAttribute attachedAttribute = (AttachedAttribute)Attribute!;
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

			if (Tree.IsArrayProperty) UpdateArray();
			else UpdateElement();
		}

		protected override void OnGUI(Rect position)
		{
			if (Tree.IsArrayProperty) DrawArray(position);
			else DrawElement(position);
		}

		protected override void LoadingGUI(Rect position)
		{
			using (DisabledScope.True)
				base.LoadingGUI(position);
		}

		#region Element Drawing

		private void UpdateElement()
		{
			if (_options == null)
			{
				((AttachedAttribute)Attribute!).Style ??= EditorStyles.helpBox;
				base.OnUpdate(); // Sets the missing style and content
				return;
			}
	
			if (Tree.Property.objectReferenceValue == null || Array.IndexOf(_options, Tree.Property.objectReferenceValue) == -1)
				Tree.Property.objectReferenceValue = _options[0];
		}

		private void DrawElement(Rect position)
		{
			AttachedAttribute attachedAttribute = (AttachedAttribute)Attribute!;
			if (_options == null)
			{
				GUIContent multiContent = TempUtility.Content(DropdownButtonContent.text, DropdownButtonContent.image, null);
				multiContent.tooltip = "Multi object editing is not supported with using the 'Attached' attribute button.";

				using (DisabledScope.True)
					DrawAsLabel(attachedAttribute.Alignment, position, multiContent, EditorStyles.miniButton);
			}
			else if (_options.Length == 0)
			{
				DrawAsLabel(attachedAttribute.Alignment, position, content, style); // draw the missing content.
			}
			else
			{
				if (DrawAsButton(attachedAttribute.Alignment, position, DropdownButtonContent, EditorStyles.miniButton))
				{
					Rect buttonEst = new Rect(Event.current.mousePosition.x - 10, position.y, 10, position.height);
					ShowOptionsDropdown(buttonEst);
				}
			}
		}

		private void ShowOptionsDropdown(Rect buttonRect)
		{
			TreeMenu menu = TreeMenu.New();
			Transform transform = (Tree.Property.serializedObject.targetObject as Component)!.transform;
			string typeName = Tree.FieldInfo!.FieldType.Name;

			menu.AddItem(TreeMenu.Item.FromTransformHierarchy(transform, Convert)!);

			buttonRect.position = GUIUtility.GUIToScreenPoint(buttonRect.position);
			menu.Show(buttonRect);

			return;

			void Clicked(object o)
			{
				Tree.Property.objectReferenceValue = o as Component;
				Tree.Property.serializedObject.ApplyModifiedProperties();
				menu?.Close();
			}

			static GUIContent Content(Component comp)
			{
				Type type = comp.GetType();
				return new(EditorGUIUtility.ObjectContent(comp, type));
			}

			TreeMenu.Item Convert(Transform child)
			{
				Component[] components = child.GetComponents(Tree.FieldInfo.FieldType);
				TreeMenu.Item item;
				GUIContent content;

				if (components.Length == 1)
				{
					content = Content(components[0]);
					item = new(content, Clicked, components[0])
					{
						selected = Tree.Property.objectReferenceValue == components[0]
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
						item.children![i].selected = Tree.Property.objectReferenceValue == component;
					}
				}

				return item;
			}
		}

		#endregion

		#region Array Drawing

		private void UpdateArray()
		{
			if (_options == null)
				return;

			bool invalid = _options.Length != Tree.Property.arraySize;
			if (!invalid)
			{
				SerializedProperty iterator = Tree.Property.Copy();
				iterator.Next(true); // .Array
				iterator.Next(true); // .Array.size

				for (int i = 0; i < _options.Length; i++)
				{
					iterator.Next(false);
					if (_options[i] != iterator.objectReferenceValue)
					{
						invalid = true;
						break;
					}
				}
			}

			if (invalid)
			{
				Tree.Property.ClearArray();
				for (int i = 0; i < _options.Length; i++)
				{
					Tree.Property.InsertArrayElementAtIndex(i);
				}

				SerializedProperty iterator = Tree.Property.Copy();
				iterator.Next(true); // .Array
				iterator.Next(true); // .Array.size

				for (int i = 0; i < _options.Length; i++)
				{
					iterator.Next(false);
					iterator.objectReferenceValue = _options[i];
				}
			}
		}

		private void DrawArray(Rect position)
		{
			using (DisabledScope.True)
				base.OnGUI(position);
		}

		#endregion
	}
}
#nullable restore