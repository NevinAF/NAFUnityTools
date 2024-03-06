namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class SNullableStructExample : NAFSampleBehaviour
	{
		[Description("Blah.."), Space(20)]
		// __<>__ prevents drawing definitions, this field prevents attributes being shown on the following field.
		[Space, HideIf(true)] public byte __Title__ = 0;


		public SNullable<float> TimeInSeconds = 12.34f;

		public SNullable<int> DistanceInMeters = 314;
	}
}