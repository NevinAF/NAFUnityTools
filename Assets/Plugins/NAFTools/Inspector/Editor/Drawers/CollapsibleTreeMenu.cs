#nullable enable
namespace NAF.Inspector.Editor
{
	using System;
	using System.Collections.Generic;
	using UnityEditor;
	using UnityEngine;

	public class TreeMenu : EditorWindow
	{
		public sealed class Item
		{
			public GUIContent content;
			public GenericMenu.MenuFunction? func;

			public GenericMenu.MenuFunction2? func2;
			public object? userData;

			public bool expanded = true;
			public bool selected = false;
			public List<Item>? children;

			public Item(GUIContent content)
				{ this.content = content; }
			public Item(GUIContent content, GenericMenu.MenuFunction func)
				{ this.content = content; this.func = func; }
			public Item(GUIContent content, GenericMenu.MenuFunction2 func, object userData)
				{ this.content = content; this.func2 = func; this.userData = userData; }

			public Item AddItem(GUIContent content) => AddItem(new Item(content));
			public Item AddItem(GUIContent content, GenericMenu.MenuFunction func) => AddItem(new Item(content, func));
			public Item AddItem(GUIContent content, GenericMenu.MenuFunction2 func, object userData) => AddItem(new Item(content, func, userData));

			public Item AddItem(Item child)
			{
				children ??= new List<Item>();
				children.Add(child);

				return child;
			}

			public bool Clickable => func != null || func2 != null;
	
			public void Click()
			{
				func?.Invoke();
				func2?.Invoke(userData);
			}

			public static Item? FromTransformHierarchy(Transform transform, Func<Transform, Item> converter)
			{
				Item item = converter(transform);
				if (item == null) return null;

				if (transform.childCount > 0)
				{
					for (int i = 0; i < transform.childCount; i++)
					{
						var child = FromTransformHierarchy(transform.GetChild(i), converter);
						if (child != null)
							item.AddItem(child);
					}
				}

				return item;
			}
		}

		private List<Item>? options;
		private Item? hoveredItem;

		private Vector2 CalcSizeWithoutCycles()
		{
			if (options == null) return Vector2.zero;

			using var _iconSize = IconSizeScope.Small;

			int depth = 0;
			float longest = 0;

			void Recurse(Item item, HashSet<Item> visited)
			{
				if (options == null) return;

				visited.Add(item);

				EditorStyles.label.CalcMinMaxWidth(item.content, out float minWidth, out float _);
				minWidth += 20 * depth;
				if (minWidth > longest) longest = minWidth;

				if (item.children != null)
				{
					for (int i = 0; i < item.children.Count; i++)
					{
						Item child = item.children[i];
						if (visited.Contains(child))
						{
							item.children.Remove(child);
							UnityEngine.Debug.LogWarning("Removed cyclic menu item: " + child.content.text);
						}
						else
						{
							depth++;
							Recurse(child, visited);
							depth--;
						}
					}
				}
			}

			var visited = new HashSet<Item>();
			int count = 0;

			for (int i = 0; i < options.Count; i++)
			{
				visited.Clear();
				Recurse(options[i], visited);
				count += visited.Count;
			}

			return new Vector2(
				Mathf.Clamp(longest + 20, 100, 450),
				count * 17 + 3
			);
		}

		public static TreeMenu New()
		{
			TreeMenu window = ScriptableObject.CreateInstance<TreeMenu>();
			window.wantsMouseMove = true;
			return window;
		}

		public void AddItem(Item item)
		{
			options ??= new List<Item>();
			options.Add(item);
		}

		public void Show(Rect? button = null)
		{
			if (button.HasValue == false)
			{
				Vector2 position;
				if (Event.current != null)
					position = GUIUtility.GUIToScreenPoint(Event.current.mousePosition);
				else position = new Vector2(Screen.width / 2, Screen.height / 2);

				button = new Rect(position, new Vector2(10, 10));
			}

			if (options == null)
			{
				Close();
				return;
			}

			ShowAsDropDown(button.Value, CalcSizeWithoutCycles());
		}

		private void OnGUI()
		{
			if (options == null)
			{
				Close();
				return;
			}

			using var _iconSize = IconSizeScope.Small;

			// Add a small inner border
			var box = new Rect(0, 0, position.width, position.height);
			Color color = EditorGUIUtility.isProSkin ? new Color(0.8f, 0.8f, 0.8f, 0.3f) : new Color(0.6f, 0.6f, 0.6f, 0.3f);

			EditorGUI.DrawRect(new Rect(box.x, box.y, box.width, 1), color); // Top
			EditorGUI.DrawRect(new Rect(box.x, box.yMax - 1, box.width, 1), color); // Bottom
			EditorGUI.DrawRect(new Rect(box.x, box.y, 1, box.height), color); // Left
			EditorGUI.DrawRect(new Rect(box.xMax - 1, box.y, 1, box.height), color); // Right

			var previousHoveredItem = hoveredItem;
			if (Event.current.type == EventType.MouseMove)
				hoveredItem = null;

			foreach (Item item in options)
			{
				DrawItem(item);
			}

			if (hoveredItem != null && hoveredItem.Clickable && Event.current.type == EventType.MouseDown)
			{
				hoveredItem.selected = true;
				hoveredItem.Click();
				Event.current.Use();
				Repaint();
			}

			if (hoveredItem != previousHoveredItem)
				Repaint();
		}

		void OnInspectorUpdate()
		{
			if (options == null)
			{
				Close();
			}
		}

		private void DrawItem(Item item)
		{
			Texture _image = item.content.image;
			item.content.image = null;

			Rect position = GUILayoutUtility.GetRect(item.content, EditorStyles.label);
			position.width = 2000;

			item.content.image = _image;

			Rect backgroundPosition = position;
			backgroundPosition.y -= 1;
			backgroundPosition.height += 2;

			if (item.selected)
			{
				Color selectColor = EditorGUIUtility.isProSkin ? new Color(0.2f, 0.4f, 0.8f, 0.2f) : new Color(0.2f, 0.4f, 0.8f, 0.4f);
				EditorGUI.DrawRect(backgroundPosition, selectColor);
			}

			if (item.Clickable && backgroundPosition.Contains(Event.current.mousePosition))
			{
				if (hoveredItem != item)
				{
					hoveredItem = item;
				}

				if (item.Clickable)
				{
					Color hoverColor = EditorGUIUtility.isProSkin ? new Color(0.5f, 0.5f, 0.5f, 0.2f) : new Color(0.8f, 0.8f, 0.8f, 0.4f);
					EditorGUIUtility.AddCursorRect(backgroundPosition, MouseCursor.Link);
					EditorGUI.DrawRect(backgroundPosition, hoverColor);
				}
			}


			if (item.children != null)
			{
				item.expanded = EditorGUI.Foldout(position, item.expanded, item.content, !item.Clickable);
			}
			else
			{
				EditorGUI.LabelField(position, item.content);
			}

			if (item.Clickable)
			{
				Rect togglePosition = position;
				togglePosition.width = togglePosition.height;
				togglePosition.x = this.position.width - togglePosition.width - 2;
				bool newSelected;

				using (IndentScope.Zero)
					newSelected = EditorGUI.Toggle(togglePosition, item.selected);

				if (newSelected != item.selected)
				{
					item.selected = newSelected;
					item.Click();
					Event.current.Use();
					Repaint();
				}
			}

			if (item.expanded && item.children != null)
			{
				EditorGUI.indentLevel += 1;
				foreach (Item child in item.children)
				{
					DrawItem(child);
				}
				EditorGUI.indentLevel -= 1;
			}
		}
	}

#nullable restore
}