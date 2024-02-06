/**
 * This file is part of the NAF-Extension, an editor extension for Unity3d.
 *
 * @link   NAF-URL
 * @author Nevin Foster
 * @since  14.06.23
 */
#nullable enable
namespace NAF.Inspector
{
	public interface IConditionalAttribute
	{
		object? Condition { get; }
		bool Invert { get; }
	}
}