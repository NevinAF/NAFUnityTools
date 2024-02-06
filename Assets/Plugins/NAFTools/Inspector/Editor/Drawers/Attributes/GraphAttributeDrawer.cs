// namespace NAF.Inspector.Editor
// {
// 	using System;
// 	using System.Collections.Generic;
// 	using System.Linq;
// 	using NAF.Inspector;
// 	using UnityEditor;
// 	using UnityEngine;

// 	[CustomPropertyDrawer(typeof(GraphAttribute))]
// 	public class GraphAttributeDrawer : ArrayPropertyDrawer
// 	{
// 		// private static readonly Color[] Colors = new Color[] {
// 		// 	Color.blue,
// 		// 	Color.red,
// 		// 	Color.green,
// 		// 	Color.yellow,
// 		// 	Color.cyan,
// 		// 	Color.magenta,
// 		// };

// 		private Graph graph = new Graph(new Graph.Args());
// 		private List<LineGraphData> datas = new List<LineGraphData>();

// 		private Span<IEnumerable<Vector2>> GetPoints(SerializedProperty property)
// 		{
// 			string selector = (this.attribute as GraphAttribute).Selector;
// 			return PropertyEvaluationCache<IEnumerable<Vector2>>.FetchAll(property, selector);
// 		}

// 		public override float GetElementHeight(SerializedProperty property, GUIContent label)
// 		{
// 			GetPoints(property);
// 			return EditorGUIUtility.singleLineHeight * 4;
// 		}

// 		public override void DrawElement(Rect position, SerializedProperty property, GUIContent label)
// 		{
// 			graph.plots.Clear();
// 			Span<IEnumerable<Vector2>> points = GetPoints(property);

// 			while (datas.Count < points.Length)
// 				datas.Add(new LineGraphData());

// 			for (int i = 0; i < points.Length; i++)
// 			{
// 				datas[i].RawValues = points[i].ToArray();
// 				graph.plots.Add(datas[i]);
// 			}

// 			// Draw the graph
// 			graph.Draw(position);

// 			// Draw lable top left
// 			Rect labelRect = new Rect(position.x, position.y, position.width, EditorGUIUtility.singleLineHeight);
// 			EditorGUI.LabelField(labelRect, label);
// 		}

// 		public override float GetArrayHeight(SerializedProperty property, GUIContent label)
// 		{
// 			return GetElementHeight(property, label);
// 		}

// 		public override void DrawArray(Rect position, SerializedProperty property, GUIContent label)
// 		{
// 			DrawElement(position, property, label);
// 		}
// 	}
// }