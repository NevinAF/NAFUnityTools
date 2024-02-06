namespace NAF.Samples
{
	using NAF.Inspector;
	using UnityEngine;

	public class InlineLabelAttributeExample : NAFSampleBehaviour
	{
		[Description("The InlineLabel attribute is an attribute that allows you to add a label to a field. The label can contain text, an icon, and a tooltip. The label can be styled and aligned. All values can be dynamically generated using string expressions.")]
		[Space(20)]

		[Header("Basic InlineLabel")]
		[Description("Changing the content that is displayed in the label.")]
		[Space]

		[InlineLabel(Label = "This Label!")]
		public int StringLabel = 1234;

		[InlineLabel(Label = "More Label!", Tooltip = "Amd a Tooltip!")]
		public string StringLabelWithTooltip = "Hello World";

		[InlineLabel(Icon = EditorIcons.Folder_Icon)]
		public bool IconLabel = true;

		[InlineLabel(Label = "Animation!", Icon = EditorIcons.Animation_Icon, Tooltip = "Animation Tooltip!")]
		public KeyCode FullStringContentLabel = KeyCode.A;

		[Header("Styling")]
		[Description("Changing the style of the label. The style can be an expression that results in a GUIStyle, or a string that is the name of a built-in GUIStyle.")]
		[Space]

		[InlineLabel(Label = "BoldStyle!", Style = "EditorStyles.boldLabel")]
		public float BoldLabel = 3.14f;

		[InlineLabel(Label = "HelpBoxStyle!", Style = "EditorStyles.helpBox")]
		public float HelpBoxLabel = 3.14f;

		[InlineLabel(Label = "Custom Style!", Style = nameof(CustomStyleFunc))]
		public float CustomStyleLabel = 3.14f;

		private GUIStyle CustomStyleFunc => new GUIStyle("miniLabel") { normal = { textColor = UnityEngine.Random.ColorHSV() }, fontStyle = FontStyle.Italic };


		[Header("Alignment")]
		[Description("Changes the position of the label relative to the field. The label can be aligned to the left, right, or between the label and the field. The label can also be applied to arrays but the 'between' behavior can result in unwanted behavior.")]
		[Space]

		[InlineLabel(Label = "Left", Alignment = LabelAlignment.Left)]
		public Vector3 LeftLabel = Vector3.left;

		[InlineLabel(Label = "BetweenLeft", Alignment = LabelAlignment.BetweenLeft)]
		public char BetweenLeftLabel = 't';

		[InlineLabel(Label = "BetweenRight", Alignment = LabelAlignment.BetweenRight)]
		public float BetweenRightLabel = 3.14f;

		[InlineLabel(Label = "Right", Alignment = LabelAlignment.Right)]
		public Vector2 RightLabel = Vector2.right;

		[Space]

		[InlineLabel(Label = "Left Array", Alignment = LabelAlignment.Left)]
		public byte[] LeftLabelArray = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

		[InlineLabel(Label = "BetweenLeft Array", Alignment = LabelAlignment.BetweenLeft)]
		public byte[] BetweenLeftLabelArray = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

		[InlineLabel(Label = "BetweenRight Array", Alignment = LabelAlignment.BetweenRight)]
		public byte[] BetweenRightLabelArray = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

		[InlineLabel(Label = "Right Array", Alignment = LabelAlignment.Right)]
		public byte[] RightLabelArray = new byte[] { 0, 1, 2, 3, 4, 5, 6, 7 };

		[Header("Prefix & Suffix")]
		[Description("The Prefix and Suffix attributes are an extension of the InlineLabel attribute, allowing you to create an inline label with some text. All properties of the InlineLabel attribute are available, but this provides a slightly more convenient way to add a prefix or suffix to a field.")]
		[Space]

		[Suffix("Suffix")]
		public AnimationCurve SuffixLabel = AnimationCurve.Linear(0, 0, 1, 1);

		[Prefix("Prefix")]
		public Color PrefixLabel = Color.green;

		[Header("Dynamic Examples")]
		[Description("All values of the InlineLabel attribute can be dynamically generated using string expressions. This allows you to create labels that change based on the value of the field.")]
		[Space]

		[InlineLabel(Label = "Math.Round({1}) + ' is a nicer number'")]
		public double RoundedLabel = 4.35d;

		[Prefix("{1}?.name")]
		public GameObject PrefixedValueName;

		[Suffix("Icon Swapper", Icon = "{1} ? EditorIcons.FolderEmpty_Icon : EditorIcons.Folder_On_Icon")]
		public bool IconSwap = false;

		[InlineLabel(Label = "{1} ? \"\" : \"Transform Needed!\"", Icon = "{1} ? null : EditorIcons.d_console_erroricon", Style = "EditorStyles.helpBox", Tooltip = "This field is required!")]
		[Tooltip("This is a proof of concept. The 'Validate' and 'Required' attributes are better suited for conditional labels.")]
		public Transform RequiredGameObject;
	}
}