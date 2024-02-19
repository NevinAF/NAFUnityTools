#nullable enable

namespace NAF.Inspector.Editor
{
	using System;
	using System.Threading.Tasks;
	using UnityEditor;
	using UnityEngine;

	#nullable enable

	public static class AttributeEvaluator
	{
		public static Task Load(IConditionalAttribute conditional, in UnityEditor.SerializedProperty property)
		{
			if (conditional.Condition == null) return Task.CompletedTask;

			var grabber = PropertyTargets.Load(property);
			return grabber.ContinueWith(t =>
			{
				var result = t.Result;

				PropertyEvaluationCache<string>.Load(result.ParentType, result.ValueType, conditional.Condition);
			});
		}


		public static bool Conditional(IConditionalAttribute conditional, SerializedProperty property, bool forceFetch = false)
		{
			if (conditional.Condition == null) return !conditional.Invert;

			System.Span<bool> results = PropertyEvaluationCache<bool>.ResolveAll(property, conditional.Condition, forceFetch);

			bool result = results.Length > 0 && TempUtility.AllEqual(results) && results[0];
			return conditional.Invert ? !result : result;
		}

		public static Task Load(IContentAttribute content, in UnityEditor.SerializedProperty property)
		{
			var grabber = PropertyTargets.Load(property);
			return grabber.ContinueWith(t =>
			{
				var result = t.Result;

				PropertyEvaluationCache<string>.Load(result.ParentType, result.ValueType, content.Label);
				PropertyEvaluationCache<string>.Load(result.ParentType, result.ValueType, content.Tooltip);
				PropertyEvaluationCache<string>.Load(result.ParentType, result.ValueType, content.Icon);
				PropertyEvaluationCache<object>.Load(result.ParentType, result.ValueType, content.Style);
			});
		}

		public static void PopulateContent(IContentAttribute content, UnityEditor.SerializedProperty property, ref GUIContent? guiContent)
		{
			const string DifferingTooltip = "Multi edit active, multiple tooltip values found.";
			const string DifferingLabel = "Multi edit active, multiple label values found.";
			const string DifferingIcon = "Multi edit active, multiple icon values found.";

			Span<string?> results;

			results = PropertyEvaluationCache<string>.ResolveAll(property, content.Label);
			bool sameLabels = results.Length > 0 && TempUtility.AllEqual(results);
			string? label = sameLabels ? results[0] : "--*";

			results = PropertyEvaluationCache<string>.ResolveAll(property, content.Icon);
			
			bool sameIcons = results.Length > 0 && TempUtility.AllEqual(results);
			Texture? icon = sameIcons ? TempUtility.EditorTexture(results[0]) : null;

			results = PropertyEvaluationCache<string>.ResolveAll(property, content.Tooltip);
			bool sameTooltips = results.Length > 0 && TempUtility.AllEqual(results);
			string tooltip = "";
			if (!sameLabels) tooltip += "\n" + DifferingLabel;
			if (!sameIcons) tooltip += "\n" + DifferingIcon;
			if (!sameTooltips) tooltip = DifferingTooltip + tooltip;
			else if (string.IsNullOrEmpty(results[0])) tooltip = tooltip.TrimStart('\n');
			else tooltip = results[0] + tooltip;

			guiContent ??= new GUIContent();
			guiContent.text = label;
			guiContent.tooltip = tooltip;
			guiContent.image = icon;
		}

		public static GUIStyle? ResolveStyle(IContentAttribute content, SerializedProperty property, bool forceFetch = false)
		{
			Span<object?> styles = NAF.Inspector.Editor.PropertyEvaluationCache<object>.ResolveAll(property, content.Style, forceFetch);

			if (styles.Length == 0 || !TempUtility.AllEqual(styles) || styles[0] == null)
				return null;

			if (styles[0] is GUIStyle style)
				return style;

			if (styles[0] is string styleName)
				return new GUIStyle(styleName);

			throw new ArgumentException($"Cannot resolve style from type {styles[0]!.GetType()} (value: {styles[0]})");
		}
	}
}