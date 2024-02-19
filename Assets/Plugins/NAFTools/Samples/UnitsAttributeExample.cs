namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class UnitsAttributeExample : NAFSampleBehaviour
	{
		[Description("The UnitsAttribute adds a suffix dropdown to a number field in the inspector. The suffix can be used to visually convert the number to a different unit of measurement. The value is always stored as the base unit defined by the attribute. Used to specify the unit of measurement for a field without needing to suffix the field, and this offers more flexibility than a raw suffix label."), Space(20)]
		// __<>__ prevents drawing definitions, this field prevents attributes being shown on the following field.
		[Space, HideIf(true)] public byte __Title__ = 0;


		[Units(UnitsAttribute.Time.Seconds)]
		public float TimeInSeconds = 12.34f;

		[Units(UnitsAttribute.Distance.Meters)]
		[Tooltip("Note that the value is automatically rounded to the nearest float number matching a whole number of meters.")]
		public int DistanceInMeters = 314;

		[Units(UnitsAttribute.Mass.Tons)]
		[Tooltip("Note that the value is automatically rounded to the nearest float number matching a whole number of tons.")]
		public byte MassInTons = 9;

		[Units(UnitsAttribute.Angle.Degrees)]
		public Vector2 AngleInDegrees = new Vector2(45, 90);

		[Units(UnitsAttribute.Velocity.MetersPerSec)]
		public Vector3 VelocityInMetersPerSec = new Vector3(1.2f, 2.3f, 3.4f);

		[Units(UnitsAttribute.Acceleration.MilesPerHr2)]
		public Vector4 AccelerationInMilesPerHr2 = new Vector4(9.8f, 7.6f, 5.4f, 3.2f);

		[Units(UnitsAttribute.Force.Newtons)]
		public double ForceInNewtons = 1234.5678;

		[Units(UnitsAttribute.Area.SqMeters)]
		[Tooltip("Note that the value is automatically rounded to the nearest float number matching a whole number of sq meters.")]
		public Vector3Int AreaInSqMeters = new Vector3Int(42, 36, 24);

		[Units(UnitsAttribute.Volume.CubicYards)]
		public float VolumeInCubicYards = 1234.5678f;
	}
}