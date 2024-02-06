namespace NAF.Inspector
{
	public interface IArrayPropertyAttribute
	{
		bool DrawOnArray { get; }
		bool DrawOnElements { get; }
		bool DrawOnField { get; }
	}
}