using System;
using System.Collections.Generic;
using System.Linq;
using UnityEditor;
using UnityEngine;

namespace NAF.Inspector.Editor
{
	/// <summary>
	/// A helper class for drawing the axis/ticks/labels and <see cref="GraphData"/> using the <see cref="Graphics"/> class. Allows for easy and performant realtime scaling and editing.
	/// </summary>
	public class Graph
	{
		public static Color DarkLineColor => EditorGUIUtility.isProSkin ? new Color(0.11f, 0.11f, 0.11f, 0.8f) : new Color(0.38f, 0.38f, 0.38f, 0.6f);
		public static Color LightLineColor => EditorGUIUtility.isProSkin ? new Color(1.000f, 1.000f, 1.000f, 0.103f) : new Color(1, 1, 1, 1);

		public struct LineStyle
		{
			public Color color;
			public float thickness;

			public LineStyle(Color color, float thickness)
			{
				this.color = color;
				this.thickness = thickness;
			}

			public void Draw(Vector2 start, Vector2 end)
			{
				Handles.color = color;
				Handles.DrawAAPolyLine(thickness, start, end);
			}
		}

		public class Args
		{
			public static GUIStyle DefaultTickLabelStyle = new GUIStyle(EditorStyles.miniLabel) { alignment = TextAnchor.MiddleRight };
			public static LineStyle DefailtTickStyle = new LineStyle(Color.white, 1);
			public static GUIStyle DefaultAxisTitleStyle = new GUIStyle(EditorStyles.label) { alignment = TextAnchor.MiddleCenter };
			public static LineStyle DefaultAxisStyle = new LineStyle(Color.white, 1);

			public float? minX = null;
			public float? maxX = null;
			public float? minY = null;
			public float? maxY = null;
			public float? xLabelSize = null;
			public float? yLabelSize = null;
			public string xTickFormat = null;
			public string yTickFormat = null;
			public int xSignificantDigits = 3;
			public int ySignificantDigits = 3;
			public int xTickMaxDecimals = 2;
			public int yTickMaxDecimals = 2;
			public int xTickCount = 10;
			public int yTickCount = 10;
			public int xTickSize = 5;
			public int yTickSize = 5;

			public GUIStyle xTickFont = DefaultTickLabelStyle;
			public GUIStyle yTickFont = DefaultTickLabelStyle;
			public LineStyle xTickBrush = DefailtTickStyle;
			public LineStyle yTickBrush = DefailtTickStyle;

			public LineStyle xAxisStyle = DefaultAxisStyle;
			public LineStyle yAxisStyle = DefaultAxisStyle;
			public GUIStyle xAxisTitleStyle = DefaultAxisTitleStyle;
			public GUIStyle yAxisTitleStyle = DefaultAxisTitleStyle;

			public float topMargin = 0;
			public float bottomMargin = 0;
			public float leftMargin = 0;
			public float rightMargin = 0;

			public float[] xTicks;
			public float[] yTicks;
		}

		public Args args;
		public readonly HashSet<GraphData> plots = new HashSet<GraphData>();
		public float minX { get; private set; }
		public float maxX { get; private set; }
		public float minY { get; private set; }
		public float maxY { get; private set; }
		// public float xLabelSize { get; private set; }
		// public float yLabelSize { get; private set; }
		public Rect ClipRectangle { get; private set;}
		public Rect GraphArea { get; private set; }

		public Graph(Args args)
		{
			this.args = args;

			args.xTickFormat = args.xTickFormat ?? "{0:F" + args.xTickMaxDecimals + "}";
			args.yTickFormat = args.yTickFormat ?? "{0:F" + args.yTickMaxDecimals + "}";
		}

		public static float CeilingPoint(float value, int sigfigs, int decimals)
		{
			if(value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 - sigfigs);
			double working = (scale * Math.Ceiling(value / scale));

			double multiplier = Math.Pow(10, decimals);
			return (float)(Math.Ceiling(working * multiplier) / multiplier);
		}

		public static float FloorPoint(float value, int sigfigs, int decimals)
		{
			if (value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 - sigfigs);
			double working = (scale * Math.Floor(value / scale));

			double multiplier = Math.Pow(10, decimals);
			// Program.Log($"FloorPoint: {value}, working: {working}, multiplier: {multiplier}, result: {Math.Floor(working * multiplier) / multiplier}");
			return (float)(Math.Floor(working * multiplier) / multiplier);
		}

		public virtual void ReCalculate(Rect clipRectangle)
		{
			ClipRectangle = clipRectangle;
			Rect graphArea = clipRectangle;

			minX = args.minX ?? plots.Min(p => p.RawValues?.Min(v => v.x) ?? float.MaxValue);
			maxX = args.maxX ?? plots.Max(p => p.RawValues?.Max(v => v.x) ?? float.MinValue);
			minY = args.minY ?? plots.Min(p => p.RawValues?.Min(v => v.y) ?? float.MaxValue);
			maxY = args.maxY ?? plots.Max(p => p.RawValues?.Max(v => v.y) ?? float.MinValue);

			if (minX == float.MaxValue || maxX == float.MinValue || minY == float.MaxValue || maxY == float.MinValue)
			{
				args.xTicks = Enumerable.Repeat(float.NaN, args.xTickCount).ToArray();
				args.yTicks = Enumerable.Repeat(float.NaN, args.yTickCount).ToArray();
			}
			else
			{
				minX = FloorPoint(minX, args.xSignificantDigits, args.xTickMaxDecimals);
				minY = FloorPoint(minY, args.ySignificantDigits, args.yTickMaxDecimals);

				args.xTicks = new float[args.xTickCount];
				float xStep = (maxX - minX) / (args.xTickCount - 1);
				xStep = CeilingPoint(xStep, args.xSignificantDigits - 1, args.xTickMaxDecimals);
				for (int i = 0; i < args.xTickCount; i++)
					args.xTicks[i] = minX + xStep * i;
				maxX = args.xTicks[args.xTickCount - 1];

				args.yTicks = new float[args.yTickCount];
				float yStep = (maxY - minY) / (args.yTickCount - 1);
				yStep = CeilingPoint(yStep, args.ySignificantDigits - 1, args.yTickMaxDecimals);
				for (int i = 0; i < args.yTickCount; i++)
					args.yTicks[i] = minY + yStep * i;
				maxY = args.yTicks[args.yTickCount - 1];


				if (args.minX.HasValue && minX != args.minX.Value)
					Debug.LogWarning($"GraphArgs minX was rounded down to match xTickDecimals: {args.minX.Value} -> {minX}");
				if (args.maxX.HasValue && maxX != args.maxX.Value)
					Debug.LogWarning($"GraphArgs maxX was rounded up to match xTickDecimals step: {args.maxX.Value} -> {maxX} (step: {xStep} from {minX} using {args.xTickCount} ticks)");
				if (args.minY.HasValue && minY != args.minY.Value)
					Debug.LogWarning($"GraphArgs minY was rounded down to match yTickDecimals: {args.minY.Value} -> {minY}");
				if (args.maxY.HasValue && maxY != args.maxY.Value)
					Debug.LogWarning($"GraphArgs maxY was rounded up to match yTickDecimals step: {args.maxY.Value} -> {maxY} (step: {yStep} from {minY} using {args.yTickCount} ticks)");
			}

			Vector2 lastXTickSize  = args.xTickFont.CalcSize(TempUtility.Content(args.xTicks[args.xTickCount - 1].ToString(args.xTickFormat)));
			Vector2 lastYTickSize = Vector2.zero;

			for (int i = 0; i < args.yTickCount; i++)
				lastYTickSize = Vector2.Max(lastYTickSize, args.yTickFont.CalcSize(TempUtility.Content(args.yTicks[i].ToString(args.yTickFormat))));

			// add margin for the text that hangs off the edge
			graphArea.width -= lastXTickSize.x / 2f;
			graphArea.yMin += lastXTickSize.y / 2f;

			// add margin for ticks/labels.
			graphArea.xMin += lastXTickSize.x + args.yTickSize;
			graphArea.height -= lastXTickSize.y + args.xTickSize;

			GraphArea = graphArea;

			foreach (GraphData plot in plots)
				plot.ReCalculate(this);
		}

		public float LerpX(float value) => GraphArea.x + (GraphArea.width) * (value - minX) / (maxX - minX);
		public float LerpY(float value) => GraphArea.y + (GraphArea.height) * (value - minY) / (maxY - minY);

		public void Draw(Rect position)
		{
			bool dirty = false;
			
			if (ClipRectangle != position)
			{
				ClipRectangle = position;
				dirty = true;
			}
			else if (plots.Any(p => p.Pixels == null))
				dirty = true;

			if (dirty)
			{
				ReCalculate(ClipRectangle);
			}

			PaintBase();
			foreach (GraphData plot in plots)
				plot.Paint(this);
		}

		public void PaintBase()
		{
			// Draw the axis
			args.xAxisStyle.Draw(new Vector2(GraphArea.x, GraphArea.yMax), new Vector2(GraphArea.xMax, GraphArea.yMax));
			args.yAxisStyle.Draw(new Vector2(GraphArea.x, GraphArea.yMax), new Vector2(GraphArea.x, GraphArea.y));

			// Draw the tick marks
			for (int i = 0; i < args.xTicks.Length; i++)
			{
				// e.Graphics.DrawLine(args.xTickPen ?? Pens.Black, x, GraphArea.Bottom, x, GraphArea.Bottom + args.xTickSize);
				// e.Graphics.DrawString(string.Format(args.xTickFormat, args.xTicks[i]), args.xTickFont, args.xTickBrush, x, GraphArea.Bottom + args.xTickSize, new StringFormat() { Alignment = StringAlignment.Center, LineAlignment = StringAlignment.Near });
				float x = GraphArea.x + (GraphArea.width) * i / (args.xTickCount - 1);
				args.xTickBrush.Draw(new Vector2(x, GraphArea.yMax), new Vector2(x, GraphArea.yMax + args.xTickSize));
				GUI.Label(new Rect(x, GraphArea.yMax, 0, 0), string.Format(args.xTickFormat, args.xTicks[i]), args.xTickFont);
			}

			for (int i = 0; i < args.yTicks.Length; i++)
			{
				// float y = GraphArea.Bottom - (GraphArea.Height) * i / (args.yTickCount - 1);
				// e.Graphics.DrawLine(args.yTickPen ?? Pens.Black, GraphArea.Left, y, GraphArea.Left - args.yTickSize, y);
				// e.Graphics.DrawString(string.Format(args.yTickFormat, args.yTicks[i]), args.yTickFont, args.yTickBrush, GraphArea.Left - args.yTickSize, y, new StringFormat() { Alignment = StringAlignment.Far, LineAlignment = StringAlignment.Center });
				float y = GraphArea.y + (GraphArea.height) * i / (args.yTickCount - 1);
				args.yTickBrush.Draw(new Vector2(GraphArea.x, y), new Vector2(GraphArea.x - args.yTickSize, y));
				GUI.Label(new Rect(GraphArea.x - args.yTickSize, y, 0, 0), string.Format(args.yTickFormat, args.yTicks[i]), args.yTickFont);
			}

			// Draw the axis titles
			// GUI.Label(new Rect(GraphArea.x + GraphArea.width / 2, GraphArea.yMax + args.xTickSize + args.xTickFont.lineHeight, 0, 0), "X", args.xAxisTitleStyle);
			// GUI.Label(new Rect(GraphArea.x - args.yTickSize - args.yTickFont.lineHeight, GraphArea.y + GraphArea.height / 2, 0, 0), "Y", args.yAxisTitleStyle);
		}
	}
}