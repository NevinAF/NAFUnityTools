namespace NAF.Inspector.Editor
{
	using UnityEngine;
	using UnityEditor;

	public class LineGraphData : GraphData
	{
		public static Graph.LineStyle DefaultLineStyle = new Graph.LineStyle(Color.black, 1);

		public Graph.LineStyle linePen = DefaultLineStyle;

		public override void Paint(Graph graph)
		{
			if (Pixels.Length <= 1) return;

			Handles.color = linePen.color;
			Vector3[] points = new Vector3[Pixels.Length];
			for (int i = 0; i < Pixels.Length; i++)
				points[i] = Pixels[i];
			Handles.DrawAAPolyLine(linePen.thickness, points);
		}

		public static LineGraphData RandomStyle()
		{
			LineGraphData data = new LineGraphData();
			System.Random r = new System.Random();

			double hue = r.NextDouble() * 360;
			double saturation = r.NextDouble() * 0.5 + 0.5;
			double value = r.NextDouble() * 0.20 + 0.80;
			data.linePen = new Graph.LineStyle(Color.cyan, 1);
			return data;
		}

		public static Color ColorFromHSV(double hue, double saturation, double value)
		{
			int hi = System.Convert.ToInt32(System.Math.Floor(hue / 60)) % 6;
			double f = hue / 60 - System.Math.Floor(hue / 60);

			value = value * 255;
			int v = System.Convert.ToInt32(value);
			int p = System.Convert.ToInt32(value * (1 - saturation));
			int q = System.Convert.ToInt32(value * (1 - f * saturation));
			int t = System.Convert.ToInt32(value * (1 - (1 - f) * saturation));

			if (hi == 0)
				return new Color(v, t, p);
			else if (hi == 1)
				return new Color(q, v, p);
			else if (hi == 2)
				return new Color(p, v, t);
			else if (hi == 3)
				return new Color(p, q, v);
			else if (hi == 4)
				return new Color(t, p, v);
			else
				return new Color(v, p, q);
		}
	}

}