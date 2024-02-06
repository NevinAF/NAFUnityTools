using System;
using UnityEngine;

namespace NAF.Inspector.Editor
{
	/// <summary>
	/// Object that can be drawn on a graph, intended as an array of 2d vectors which can be drawn (lines, points, bars, etc.)
	/// </summary>
	public abstract class GraphData
	{
		private Vector2[] _rawValues;
		public Vector2[] RawValues
		{
			get => _rawValues;
			set
			{
				_rawValues = value;
				Pixels = null;
			}
		}

		public Vector2[] Pixels { get; private set; }

		public virtual void ReCalculate(Graph graph)
		{
			if (RawValues == null)
			{
				Pixels = new Vector2[0];
				return;
			}

			Pixels = new Vector2[RawValues.Length];
			for (int i = 0; i < RawValues.Length; i++)
			{
				Pixels[i].x = graph.LerpX(RawValues[i].x);
				Pixels[i].y = graph.LerpY(RawValues[i].y);
			}
		}

		public abstract void Paint(Graph graph);

		/// <summary> Returns the index of the closest point to the given x pixel. </summary>
		public virtual int ClosestX(float xpos)
		{
			if (Pixels == null || xpos < Pixels[0].x || xpos > Pixels[Pixels.Length - 1].x)
				return -1;

			int left = 0;
			int right = Pixels.Length - 1;
			while (left < right)
			{
				int mid = (left + right) / 2;
				if (Pixels[mid].x > xpos)
					right = mid;
				else
					left = mid + 1;
			}

			if (left == 0)
				return 0;
			else
			{
				if (xpos - Pixels[left - 1].x < Pixels[left].x - xpos)
					return left - 1;
				else
					return left;
			}
		}

		public virtual int ClosestY(float ypos)
		{
			if (Pixels == null || ypos < Pixels[0].y || ypos > Pixels[Pixels.Length - 1].y)
				return -1;

			int left = 0;
			int right = Pixels.Length - 1;
			while (left < right)
			{
				int mid = (left + right) / 2;
				if (Pixels[mid].y > ypos)
					right = mid;
				else
					left = mid + 1;
			}

			if (left == 0)
				return 0;
			else if (left == Pixels.Length - 1)
				return Pixels.Length - 1;
			else
			{
				if (ypos - Pixels[left - 1].y < Pixels[left].y - ypos)
					return left - 1;
				else
					return left;
			}
		}

		public virtual int ClosestPoint(Vector2 pos)
		{
			int x = ClosestX(pos.x);
			int y = ClosestY(pos.y);
			if (x == -1 || y == -1)
				return -1;
			else if (x == y)
				return x;
			else
			{
				if (Math.Abs(pos.x - Pixels[x].x) < Math.Abs(pos.y - Pixels[y].y))
					return x;
				else
					return y;
			}
		}
	}
}