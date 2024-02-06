namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class ValidateAttributeExample : NAFSampleBehaviour
	{
		[Description("The ValidateAttribute is used to conditionally display an inline label. By default, when the condition evaluates to false, an error icon is displayed to the right of the field. This attribute extends all functionality of 'InlineLabelAttribute' for customizing the label content, style, and alignment.")]
		[Space(20)]

		[Header("Helper Fields")]

		public bool Toggle = true;
		public GameObject Target = null;

		[Header("Basic Validate")]
		[Description("The following examples only populate the condition, which is a string expression that is interpreted as a boolean (non default/false).")]
		[Space]

		[Validate("{1} > 0")]
		public int Positive = -12;

		[Validate("{1}.Length < 24")]
		public string LessThan24Chars = "Hello World and everyone who is reading this!";

		[Validate("{1}.magnitude > float.Epsilon")]
		public Vector2 MagnitudeGreaterThanEpsilon = Vector2.zero;

		[Validate("float.IsNaN({1}.x) || float.IsNaN({1}.y)")]
		public Vector2 OneIsNaN = new Vector2(1, 2);

		[Validate("(bool){1} || !" + nameof(Toggle))]
		public GameObject NotNullWhenToggle = null;

		[Header("Validate Labels/Icons")]
		[Description("In addition to a condition, the following also have some custom the label content")]
		[Space]

		[Validate("{1}.Length > 0", Icon = EditorIcons.d_console_warnicon, Label = "Empty is Skipped!")]
		public int[] ArrayNotEmptyWarning = new int[0];

		[Validate("{1}?.gameObject == " + nameof(Target), Icon = EditorIcons.d_console_infoicon, Label = "{1}?.gameObject.name + \" is not Target!\"")]
		public Transform NotOnTarget = null;

		[Header("Validate Styles")]
		[Description("In addition to a condition, the following also have some custom the label style")]
		[Space]

		[Validate("!{1} || (bool)" + nameof(Target), Style = "EditorStyles.label")]
		public bool OnWithoutObject = true;

		[Validate("=(bool){1}?.GetComponent(typeof(" + nameof(NAFSampleBehaviour) + "))", Style = nameof(CustomStyleFunc), Label = "Custom Style!")]
		public GameObject WithNAFSampleBehaviour = null;

		private GUIStyle CustomStyleFunc => new GUIStyle("miniLabel") { normal = { textColor = UnityEngine.Random.ColorHSV() }, fontStyle = FontStyle.Italic };

		[Header("Required Attribute")]
		[Description("The RequiredAttribute is the same as a ValidateAttribute with the condition always set to '{1}'. This will always evaluate to true iff the field is not the default value (null, 0, false, etc).")]
		[Space]

		[Required]
		public GameObject RequiredObject = null;

		[Required]
		public LayerMask RequiredMask = default;
	}
}