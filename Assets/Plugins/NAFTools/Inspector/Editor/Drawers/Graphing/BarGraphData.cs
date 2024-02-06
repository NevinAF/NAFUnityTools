// namespace NAF.Inspector.Editor
// {
// 	public class BarGraphData : GraphData
// 	{
// 		public static Brush DefaultBarBrush = Brushes.Black;

// 		public Brush barBrush = DefaultBarBrush;
// 		public float BarWidth { get; private set; }
// 		public RectangleF[] Rects { get; private set; }

// 		public override void ReCalculate(PaintEventArgs e, Graph graph)
// 		{
// 			base.ReCalculate(e, graph);
// 			Rects = new RectangleF[Pixels.Length];

// 			if (Pixels.Length == 0) return;

// 			BarWidth = float.MaxValue;
// 			for (int i = 1; i < Pixels.Length; i++)
// 			{
// 				float newDiff = Pixels[i].X - Pixels[i - 1].X;
// 				if (newDiff < BarWidth && newDiff > 0)
// 					BarWidth = newDiff;
// 			}

// 			for (int i = 0; i < Pixels.Length; i++)
// 			{
// 				float x = Pixels[i].X;
// 				float y = Pixels[i].Y;
// 				float width = BarWidth;
// 				float height = graph.GraphArea.Bottom - y;
// 				x -= width / 2f;
// 				if (x < graph.GraphArea.Left)
// 				{
// 					width -= graph.GraphArea.Left - x;
// 					x = graph.GraphArea.Left;
// 				}
// 				else if (x > graph.GraphArea.Right)
// 				{
// 					x = graph.GraphArea.Right;
// 					width = 0;
// 				}
// 				else if (x + width > graph.GraphArea.Right)
// 					width -= x + width - graph.GraphArea.Right;

// 				x = (float)Math.Round(x);
// 				width = (float)Math.Ceiling(width);

// 				Rects[i] = new RectangleF(x, Pixels[i].Y, width, height);
// 			}
// 		}

// 		public override void Paint(PaintEventArgs e, Graph graph)
// 		{
// 			for (int i = 0; i < Pixels.Length; i++)
// 				e.Graphics.FillRectangle(barBrush, Rects[i]);
// 		}
// 	}
// }