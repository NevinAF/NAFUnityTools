using System.Collections;
using System.Collections.Generic;
using NAF.Inspector;
using UnityEngine;

/// <summary>
/// This class draws a path based on the beat of the music. For this example, the "path" is handled by a trail renderer and each beat will reset the upward velocity of the trail renderer.
/// </summary>
public class Sample1 : MonoBehaviour
{
	// [Required]
	// public AudioProcessor beatDetector;

	[Tooltip("The rigidbody that will be used to move the path.")]
	[Attached]
	public Rigidbody2D physicsBody;

	[Tooltip("The sensitivity of the beat detection. Lower values are more sensitive and thus more beats will be detected.")]
	[Range(0, 1)]
	public float sensitivity = 0.5f;

	public int PropertyTest { get; set; }

	[Tooltip("The minimum time between beats. Lower values will result in more beats being detected.")]
	[Units(UnitsAttribute.Time.Seconds)]
	public float beatInterval = 0.3f;

	public bool ThisIsAFunctionAfterBeatInterval()
	{
		return true;
	}

	[Tooltip("The velocity that set when a beat is hit.")]
	[Units(UnitsAttribute.Velocity.MetersPerSec)]
	public float beatVelocity = 5f;

	private float lastBeatTime = 0f;

	private void Start()
	{
		// physicsBody.velocity = new Vector2(1f, 0f);
		// beatDetector.onBeat.AddListener(() => {
		// 	physicsBody.velocity = new Vector2(1f, beatVelocity);
		// });
	}
}
