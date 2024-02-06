// namespace NAF.Inspector.Editor
// {
// 	public class ScatterGraphData : GraphData
// 	{
// 		public enum Shape { Circle, Square, Triangle, Diamond, Cross, X }
// 		public static Brush DefaultBrush = Brushes.Black;
// 		public static Pen DefaultPen = Pens.Black;

// 		public Brush brush = DefaultBrush;
// 		public Pen pen = DefaultPen;
// 		public Shape shape = Shape.Circle;
// 		public float size = 5;

// 		public override void Paint(PaintEventArgs e, Graph graph)
// 		{
// 			switch (shape)
// 			{
// 				case Shape.Circle:
// 					for (int i = 0; i < Pixels.Length; i++)
// 						e.Graphics.FillEllipse(brush, Pixels[i].X - size / 2f, Pixels[i].Y - size / 2f, size, size);
// 					break;
// 				case Shape.Square:
// 					for (int i = 0; i < Pixels.Length; i++)
// 						e.Graphics.FillRectangle(brush, Pixels[i].X - size / 2f, Pixels[i].Y - size / 2f, size, size);
// 					break;
// 				case Shape.Triangle:
// 					for (int i = 0; i < Pixels.Length; i++)
// 					{
// 						PointF[] points = new PointF[3];
// 						points[0] = new PointF(Pixels[i].X, Pixels[i].Y - size / 2f);
// 						points[1] = new PointF(Pixels[i].X - size / 2f, Pixels[i].Y + size / 2f);
// 						points[2] = new PointF(Pixels[i].X + size / 2f, Pixels[i].Y + size / 2f);
// 						e.Graphics.FillPolygon(brush, points);
// 					}
// 					break;
// 				case Shape.Diamond:
// 					for (int i = 0; i < Pixels.Length; i++)
// 					{
// 						PointF[] points = new PointF[4];
// 						points[0] = new PointF(Pixels[i].X, Pixels[i].Y - size / 2f);
// 						points[1] = new PointF(Pixels[i].X - size / 2f, Pixels[i].Y);
// 						points[2] = new PointF(Pixels[i].X, Pixels[i].Y + size / 2f);
// 						points[3] = new PointF(Pixels[i].X + size / 2f, Pixels[i].Y);
// 						e.Graphics.FillPolygon(brush, points);
// 					}
// 					break;
// 				case Shape.Cross:
// 					for (int i = 0; i < Pixels.Length; i++)
// 					{
// 						e.Graphics.DrawLine(pen, Pixels[i].X - size / 2f, Pixels[i].Y - size / 2f, Pixels[i].X + size / 2f, Pixels[i].Y + size / 2f);
// 						e.Graphics.DrawLine(pen, Pixels[i].X - size / 2f, Pixels[i].Y + size / 2f, Pixels[i].X + size / 2f, Pixels[i].Y - size / 2f);
// 					}
// 					break;
// 				case Shape.X:
// 					for (int i = 0; i < Pixels.Length; i++)
// 					{
// 						e.Graphics.DrawLine(pen, Pixels[i].X - size / 2f, Pixels[i].Y - size / 2f, Pixels[i].X + size / 2f, Pixels[i].Y + size / 2f);
// 						e.Graphics.DrawLine(pen, Pixels[i].X - size / 2f, Pixels[i].Y + size / 2f, Pixels[i].X + size / 2f, Pixels[i].Y - size / 2f);
// 					}
// 					break;
// 			}
// 		}
// 	}
// }