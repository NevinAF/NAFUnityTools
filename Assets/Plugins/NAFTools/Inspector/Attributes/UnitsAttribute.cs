namespace NAF.Inspector
{
	using UnityEngine;
	using System;
	using System.Collections.Generic;
	using System.Linq;

	[AttributeUsage(AttributeTargets.Field, Inherited = true, AllowMultiple = false)]
	[System.Diagnostics.Conditional("UNITY_EDITOR")]
	public class UnitsAttribute : PropertyAttribute
	{
		public enum Time
		{
			Milliseconds, Seconds,
			Minutes, Hours, Days,
			Weeks, Months, Years,
		}

		public enum Distance
		{
			Millimeters, Centimeters, Meters, Kilometers,
			Inches, Feet, Yards, Miles,
		}

		public enum Mass
		{
			Milligrams, Grams, Kilograms,
			Ounces, Pounds, Tons,
		}

		public enum Angle
		{
			Degrees, Radians,
		}

		public enum Velocity
		{
			MetersPerSec, KilometersPerHr, MilesPerHr,
		}

		public enum Acceleration
		{
			MetersPerSec2, KilometersPerHr2, MilesPerHr2,
		}

		public enum Force
		{
			Newtons, KilogramsForce, PoundsForce,
		}

		public enum Area
		{
			SqMillimeters, SqCentimeters, SqMeters, SqKilometers,
			SqInches, SqFeet, SqYards, SqMiles,
		}

		public enum Volume
		{
			CubicMillimeters, CubicCentimeters, CubicMeters, CubicKilometers,
			CubicInches, CubicFeet, CubicYards, CubicMiles,
		}

		public static Dictionary<object, float> ConversionTable = new Dictionary<object, float>()
		{
			{ Time.Milliseconds, 0.001f },
			{ Time.Seconds, 1f },
			{ Time.Minutes, 60f },
			{ Time.Hours, 3600f },
			{ Time.Days, 86400f },
			{ Time.Weeks, 604800f },
			{ Time.Months, 2628000f },
			{ Time.Years, 31536000f },

			{ Distance.Millimeters, 0.001f },
			{ Distance.Centimeters, 0.01f },
			{ Distance.Meters, 1f },
			{ Distance.Kilometers, 1000f },
			{ Distance.Inches, 0.0254f },
			{ Distance.Feet, 0.3048f },
			{ Distance.Yards, 0.9144f },
			{ Distance.Miles, 1609.34f },

			{ Mass.Milligrams, 0.001f },
			{ Mass.Grams, 1f },
			{ Mass.Kilograms, 1000f },
			{ Mass.Ounces, 28.3495f },
			{ Mass.Pounds, 453.592f },
			{ Mass.Tons, 907185f },

			{ Angle.Degrees, 1f },
			{ Angle.Radians, 57.2958f },

			{ Velocity.MetersPerSec, 1f },
			{ Velocity.KilometersPerHr, 0.277778f },
			{ Velocity.MilesPerHr, 0.44704f },

			{ Acceleration.MetersPerSec2, 1f },
			{ Acceleration.KilometersPerHr2, 0.0000771605f },
			{ Acceleration.MilesPerHr2, 0.000124274f },

			{ Force.Newtons, 1f },
			{ Force.KilogramsForce, 9.80665f },
			{ Force.PoundsForce, 4.44822f },

			{ Area.SqMillimeters, 0.000001f },
			{ Area.SqCentimeters, 0.0001f },
			{ Area.SqMeters, 1f },
			{ Area.SqKilometers, 1000000f },
			{ Area.SqInches, 0.00064516f },
			{ Area.SqFeet, 0.092903f },
			{ Area.SqYards, 0.836127f },
			{ Area.SqMiles, 2589990f },

			{ Volume.CubicMillimeters, 0.000000001f },
			{ Volume.CubicCentimeters, 0.000001f },
			{ Volume.CubicMeters, 1f },
			{ Volume.CubicKilometers, 1000000000f },
			{ Volume.CubicInches, 0.0000163871f },
			{ Volume.CubicFeet, 0.0283168f },
			{ Volume.CubicYards, 0.764555f },
			{ Volume.CubicMiles, 4168180000f },
		};

		public Type[] types = new Type[]
		{
			typeof(Time), typeof(Distance), typeof(Mass), typeof(Angle),
			typeof(Velocity), typeof(Acceleration), typeof(Force),
			typeof(Area), typeof(Volume),
		};

		public object storeAs;
		public object value;

		public static float Convert(float value, object from, object to)
		{
			return MathOps.RoundPoint(value * ConversionTable[from] / ConversionTable[to], 5);
		}

		public UnitsAttribute(object value)
		{
			if (!types.Contains(value.GetType()))
				throw new ArgumentException("Value must be a valid unit type. Got: " + value.GetType() + " | Expected: " + string.Join(", ", types.Select(x => x.Name)) + ".");

			if (!ConversionTable.ContainsKey(value))
				throw new ArgumentException("Value must be a valid unit! Got: " + value + " | Expected: " + string.Join(", ", ConversionTable.Keys.Select(k => k.GetType() == value.GetType())) + ".");

			this.value = value;
			this.storeAs = value;
		}
	}
}