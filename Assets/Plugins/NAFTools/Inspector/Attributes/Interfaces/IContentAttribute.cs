#nullable enable
namespace NAF.Inspector
{
	using System.Buffers;
	using UnityEngine;

	public interface IContentAttribute
	{
		object? Label { get; }
		object? Tooltip { get; }
		object? Icon { get; }
		object? Style { get; }
	}
}