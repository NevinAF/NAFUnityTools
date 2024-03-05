namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class SliderAttributeExample : NAFSampleBehaviour
	{
		[Description("Blah.."), Space(20)]
		// __<>__ prevents drawing definitions, this field prevents attributes being shown on the following field.
		[Space, HideIf(true)] public byte __Title__ = 0;


		[Slider(0, 100)]
		public float TimeInSeconds = 12.34f;

		[Slider(nameof(TimeInSeconds), 200)]
		public int DistanceInMeters = 314;

		[Slider(nameof(TimeInSeconds), nameof(DistanceInMeters))]
		public byte MassInTons = 9;

		[Slider(0, 360)]
		public Vector2 FloatAngleInDegrees = new Vector2(45, 90);

		
		[Slider(0, 360)]
		public Vector2Int IntAngleInDegrees = new Vector2Int(45, 90);

		[Slider(0, 360)]
		public Vector3 Angle3Float = new Vector3(45, 90, 180);
	}
}