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
		/// <summary>
		/// Adds a callback to the task using async/await which is more performant than using ContinueWith.
		/// </summary>
		public static async Task Callback<T>(this Task<T> task, Action<T> callback)
		{
			callback(await task);
		}

		/// <summary>
		/// Adds a callback to the task using async/await which is more performant than using ContinueWith.
		/// </summary>
		public static async Task Callback(this Task task, Action callback)
		{
			await task;
			callback();
		}

		public static Task<AttributeExpr<bool>> Conditional(IConditionalAttribute conditional, in SerializedProperty property)
		{
			if (conditional.Condition == null)
				return Task.FromResult(AttributeExpr<bool>.Constant(!conditional.Invert));

			var result = AttributeExpr<bool>.AsyncCreate(property, conditional.Condition);
			if (conditional.Invert)
				return InvertTask(result);
			return result;
		}

		private static async Task<AttributeExpr<bool>> InvertTask(Task<AttributeExpr<bool>> valueTask)
		{
			var value = await valueTask;
			if (value.IsFaulted)
				return value; // We don't need to invert the error for any reason..

			if (value.IsConstant)
				return AttributeExpr<bool>.Constant(!value.AsConstant());

			Func<object?, object?, bool> func = (object? parent, object? field) => !value.Compute(parent, field);
			return AttributeExpr<bool>.Constant(func);
		}


		public static Task<(AttributeExpr<GUIContent>, AttributeExpr<GUIStyle>)> Content(IContentAttribute content, in SerializedProperty property)
		{
			var label = AttributeExpr<string>.AsyncCreate(property, content.Label);
			var tooltip = AttributeExpr<string>.AsyncCreate(property, content.Tooltip);
			var icon = AttributeExpr<Texture>.AsyncCreate(property, content.Icon);
			var style = AttributeExpr<GUIStyle>.AsyncCreate(property, content.Style);

			return Content(label, tooltip, icon, style);
		}

		private static async Task<(AttributeExpr<GUIContent>, AttributeExpr<GUIStyle>)> Content(Task<AttributeExpr<string>> labelTask, Task<AttributeExpr<string>> tooltipTask, Task<AttributeExpr<Texture>> iconTask, Task<AttributeExpr<GUIStyle>> styleTask)
		{
			var label = await labelTask;
			var tooltip = await tooltipTask;
			var icon = await iconTask;

			object content;

			if (label.IsConstant && tooltip.IsConstant && icon.IsConstant)
				content = new GUIContent(label.AsConstant(), icon.AsConstant(), tooltip.AsConstant());
			else {
				GUIContent cache = new GUIContent();
				Func<object?, object?, GUIContent> func = (object? parent, object? field) =>
				{
					cache.text = label.Compute(parent, field);
					cache.tooltip = tooltip.Compute(parent, field);
					cache.image = icon.Compute(parent, field);
					return cache;
				};
				content = func;
			}

			var style = await styleTask;

			return (AttributeExpr<GUIContent>.Constant(content), style);
		}
	}
}