namespace NAF.Inspector.Editor
{
	using System;
	using System.Threading.Tasks;
	using NAF.Inspector;
	using UnityEditor;
	using UnityEngine;

	[CustomPropertyDrawer(typeof(SliderAttribute))]
	public class SliderAttributeDrawer : NAFPropertyDrawer
	{
		AttributeExprCache<double> min;
		AttributeExprCache<double> max;

		protected override async Task OnEnable()
		{
			SliderAttribute attr = (SliderAttribute)Attribute;
			var minTask = AttributeExpr<double>.AsyncCreate(Tree.Property, attr.Min);
			var maxTask = AttributeExpr<double>.AsyncCreate(Tree.Property, attr.Max);

			min = await minTask;
			max = await maxTask;
		}

		protected override void OnUpdate()
		{
			min.Refresh(Tree.Property);
			max.Refresh(Tree.Property);
		}

		protected override void OnGUI(Rect position)
		{
			if (min.Value >= max.Value)
			{
				ErrorGUI(position, Tree.PropertyLabel, new System.ArgumentException("Min value is greater than or equal to max value"));
				return;
			}

			switch (Tree.Property.propertyType)
			{
				case SerializedPropertyType.Float:
					if (Tree.Property.floatValue < min.Value || Tree.Property.floatValue > max.Value)
					{
						InlineGUI.InlineLabel(ref position, TempUtility.Content("Out of Range", EditorIcons.d_console_warnicon), EditorStyles.helpBox, true);
					}

					EditorGUI.Slider(position, Tree.Property, (float)min.Value, (float)max.Value);
					break;
				case SerializedPropertyType.Integer:
					if (Tree.Property.longValue < min.Value || Tree.Property.longValue > max.Value)
					{
						InlineGUI.InlineLabel(ref position, TempUtility.Content("Out of Range", EditorIcons.d_console_warnicon), EditorStyles.helpBox, true);
					}

					EditorGUI.IntSlider(position, Tree.Property, Mathf.CeilToInt((float)min.Value), (int)max.Value);
					break;
				case SerializedPropertyType.Vector2: {
					Vector2 value = Tree.Property.vector2Value;
					value = Vector2Slider(position, Tree.PropertyLabel, value, (float)min.Value, (float)max.Value);
					Tree.Property.vector2Value = value;
					break;
				}
				case SerializedPropertyType.Vector2Int: {
					Vector2Int value = Tree.Property.vector2IntValue;
					Vector2 v = value;
					v = Vector2Slider(position, Tree.PropertyLabel, v, Mathf.CeilToInt((float)min.Value), (int)max.Value);
					value = new Vector2Int(Mathf.RoundToInt(v.x), Mathf.RoundToInt(v.y));
					Tree.Property.vector2IntValue = value;
					break;
				}
				case SerializedPropertyType.Vector3: {
					Vector3 value = Tree.Property.vector3Value;
					value = Vector3Slider(position, Tree.PropertyLabel, value, (float)min.Value, (float)max.Value);
					Tree.Property.vector3Value = value;
					break;
				}
				case SerializedPropertyType.Vector3Int: {
					throw new System.NotImplementedException("Vector3Int slider not implemented");
				}
				case SerializedPropertyType.Vector4: {
					throw new System.NotImplementedException("Vector4 slider not implemented");
				}
				default:
					throw new System.ArgumentException("Slider attribute can only be applied to float, int, Vector2, Vector2Int, Vector3, Vector3Int, Vector4");
			}
		}

		protected override float OnGetHeight()
		{
			return EditorGUIUtility.singleLineHeight;
		}

		public static Vector2 Vector2Slider(Rect position, GUIContent label, Vector2 value, float min, float max)
		{
			if (value.x > value.y) {
				InlineGUI.InlineLabel(ref position, TempUtility.Content("Min > Max", EditorIcons.d_console_erroricon), EditorStyles.helpBox, true);
			}
			else if (value.x < min || value.y > max)
			{
				InlineGUI.InlineLabel(ref position, TempUtility.Content("Out of Range", EditorIcons.d_console_warnicon), EditorStyles.helpBox, true);
			}

			EditorGUI.LabelField(position, label);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			float x = value.x;
			float y = value.y;

			const float padding = 5;

			Rect leftFloat = position;
			Rect rightFloat = position;
			Rect slider = position;
			leftFloat.width = EditorGUIUtility.fieldWidth;
			rightFloat.width = EditorGUIUtility.fieldWidth;
			rightFloat.x = position.xMax - rightFloat.width;
			slider.xMin += leftFloat.width + padding;
			slider.xMax -= rightFloat.width + padding;

			if (slider.width > 30)
			{
				EditorGUI.MinMaxSlider(slider, ref x, ref y, min, max);

				if (x != value.x)
					x = (float)MathOps.RoundPoint(x, 4);
				if (y != value.y)
					y = (float)MathOps.RoundPoint(y, 4);
			}

			value.x = EditorGUI.FloatField(leftFloat, x);
			value.y = EditorGUI.FloatField(rightFloat, y);

			return value;
		}

		public static Vector3 Vector3Slider(Rect position, GUIContent label, Vector3 value, float min, float max)
		{
			if (value.x > value.y) {
				InlineGUI.InlineLabel(ref position, TempUtility.Content("Min > Max", EditorIcons.d_console_erroricon), EditorStyles.helpBox, true);
			}
			else if (value.x < min || value.y > max)
			{
				InlineGUI.InlineLabel(ref position, TempUtility.Content("Out of Range", EditorIcons.d_console_warnicon), EditorStyles.helpBox, true);
			}

			EditorGUI.LabelField(position, label);
			position = EditorGUI.PrefixLabel(position, GUIUtility.GetControlID(FocusType.Passive), label);

			float x = value.x;
			float y = value.y;
			float z = value.z;

			const float padding = 5;

			Rect leftFloat = position;
			Rect right1Float = position;
			Rect right2Float = position;
			Rect slider = position;
			leftFloat.width = EditorGUIUtility.fieldWidth;
			right1Float.width = EditorGUIUtility.fieldWidth;
			right2Float.width = EditorGUIUtility.fieldWidth;
			right2Float.x = position.xMax - right2Float.width;
			right1Float.x = right2Float.x - right1Float.width - padding;
			slider.xMin += leftFloat.width + padding;
			slider.xMax = right1Float.x - padding;
			slider.y += 2;

			Rect middle = slider;
			// float xpoint = (x - min) / (max - min);
			// float ypoint = (z - min) / (max - min);
			// middle.x = Mathf.Lerp(slider.x, slider.xMax, xpoint) - 3;
			// middle.width = Mathf.Lerp(slider.x, slider.xMax, ypoint) - middle.x + 3;
			middle.y -= 2;
			middle.height -= 6;

			if (slider.width > 30)
			{

				EditorGUI.MinMaxSlider(slider, ref x, ref z, min, max);
				y = GUI.HorizontalSlider(middle, y, min, max, GUIStyle.none, GUI.skin.verticalSliderThumb);

				// If both changed, change y. If one changed, clamp to y;
				

				if (x != value.x && z != value.z)
				{
					if (value.x == value.y)
					{
						y = x;
					}
					else if (value.y == value.z)
					{
						y = z;
					}
					else
					{
						float diff = value.x - x;
						y = Mathf.Clamp(value.y - diff, x, z);
					}
				}
				else if (x != value.x)
				{
					x = Mathf.Min(x, y);
				}
				else if (z != value.z)
				{
					z = Mathf.Max(z, y);
				}
				else if (y != value.y)
				{
					y = Mathf.Clamp(y, x, z);
				}

				if (x != value.x)
					x = (float)MathOps.RoundPoint(x, 4);
				if (y != value.y)
					y = (float)MathOps.RoundPoint(y, 4);
				if (z != value.z)
					z = (float)MathOps.RoundPoint(z, 4);

				
			}

			// Testing..
			



			value.x = EditorGUI.FloatField(leftFloat, x);
			value.y = EditorGUI.FloatField(right1Float, y);
			value.z = EditorGUI.FloatField(right2Float, z);

			return value;
		}
	}
}