namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class ReadonlyAttributeExample : NAFSampleBehaviour
	{
		[Description("The ReadonlyAttribute is a property drawer that makes a field read-only in the inspector. The attribute can be enabled/disabled based on a string expression as long as the expression evaluates to a boolean.")]
		[Space(20)]

		[Header("Helper Fields")]

		public int number = 0;
		public Vector2 vector2 = Vector2.zero;
		public GameObject gameobject = null;
		public string text = "Normal Text!";

		[Header("Unconditional Readonly")]
		[Description("When used without any condition, the field is always read-only. The 'DrawOnElements' option can be used to draw the attribute on each element of an array rather than the array itself.")]
		[Space]

		[Readonly]
		public int ReadonlyConstantField = 1234;

		[field: Readonly]
		[field: SerializeField]
		public int HalfNumberProperty { get; set; }

		[Readonly]
		public Transform GameObjectTransform;

		[Readonly]
		public float LengthVector2;

		[Readonly]
		public string GameObjectName;

		[Readonly(DrawOnElements = true)]
		public string[] ReadonlyElements = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };

		private void OnValidate()
		{
			UnityEngine.Debug.Log("OnValidate!", this);

			if (gameobject != null)
			{
				GameObjectTransform = gameobject.transform;
				GameObjectName = gameobject.name;
			}
			else {
				GameObjectTransform = null;
				GameObjectName = null;
			}

			LengthVector2 = vector2.magnitude;
			HalfNumberProperty = number / 3;
		}


		[Header("Disable If")]
		[Description("The DisableIf attribute is a property drawer that disables a field in the inspector based on a string expression as long as the expression evaluates to a boolean.")]
		[Space]

		[DisableIf("=" + nameof(gameobject))]
		[Tooltip("This condition is a field and thus evaluates to true when not default/null, and false otherwise.")]
		public float Gameobject_Is_Set = 3.14f;

		[DisableIf("=" + nameof(vector2) + ".x > " + nameof(vector2) + ".y")]
		[Tooltip("This condition is a predicate expression and thus evaluates to true when the expression is true, and false otherwise.")]
		public float Vector2_X_Greater_Y = 9.8f;

		[DisableIf("=" + nameof(text) + " == 'Normal'")]
		[Tooltip("This condition is a predicate expression and thus evaluates to true when the expression is true, and false otherwise.")]
		public float Text_Is_Normal = 6.28f;

		[DisableIf("=transform.childCount % 2 == 1")]
		public string[] ChildrenCountIsOdd = new string[] { "A", "B", "C", "D", "E", "F", "G", "H" };


		[Header("Enable If")]
		[Description("The EnableIf attribute is a property drawer that enables a field in the inspector based on a string expression as long as the expression evaluates to a boolean.")]
		[Space]

		[EnableIf("=(bool)gameobject && gameobject.activeSelf")]
		[Tooltip("This condition is a predicate expression and thus evaluates to true when the expression is true, and false otherwise.")]
		public string Gameobject_Is_Active = "Enabled if the gameobject active.";

		[EnableIf("=" + "Mathf.Sqrt(" + nameof(number) + ") == (int)Mathf.Sqrt(" + nameof(number) + ")")]
		[Tooltip("This condition is a predicate expression and thus evaluates to true when the expression is true, and false otherwise.")]
		public string Number_Is_Square = "Enabled if the number is a perfect square.";

		[EnableIf("=" + nameof(vector2) + ".y % " + nameof(vector2) + ".x")]
		[Tooltip("This condition is an expression and thus evaluates to true when the expression is not default/null (0 in this case), and false otherwise.")]
		public string Vector2_X_Not_Multiple_Of_Y = "Enabled if the x of the vector is not a multiple of the y.";
	}
}