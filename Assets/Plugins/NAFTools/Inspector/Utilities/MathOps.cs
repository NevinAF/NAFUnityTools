namespace NAF.Inspector
{
	using System;

	public static class MathOps
	{
		public static float RoundPoint(float value, int sigfigs) => RoundPoint(value, sigfigs, sigfigs);
		public static float RoundPoint(float value, int sigfigs, int decimals)
		{
			if (value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 - sigfigs);
			double working = (scale * Math.Round(value / scale));

			double multiplier = Math.Pow(10, decimals);
			return (float)(Math.Round(working * multiplier) / multiplier);
		}

		public static float FloorPoint(float value, int sigfigs) => FloorPoint(value, sigfigs, sigfigs);
		public static float FloorPoint(float value, int sigfigs, int decimals)
		{
			if (value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 - sigfigs);
			double working = (scale * Math.Floor(value / scale));

			double multiplier = Math.Pow(10, decimals);
			return (float)(Math.Floor(working * multiplier) / multiplier);
		}

		public static float CeilPoint(float value, int sigfigs) => CeilPoint(value, sigfigs, sigfigs);
		public static float CeilPoint(float value, int sigfigs, int decimals)
		{
			if (value == 0)
				return 0;

			double scale = Math.Pow(10, Math.Floor(Math.Log10(Math.Abs(value))) + 1 - sigfigs);
			double working = (scale * Math.Ceiling(value / scale));

			double multiplier = Math.Pow(10, decimals);
			return (float)(Math.Floor(working * multiplier) / multiplier);
		}
	}
}