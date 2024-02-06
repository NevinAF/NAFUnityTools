namespace NAF.ExpressionCompiler
{
	public class Program
	{
		public static void Main(string[] args)
		{
			Compiler compiler = new Compiler();
			var com = compiler.Compile("34M * (decimal)Math.PI");
			// var com2 = compiler.Compile("Debug.WriteLine((decimal)34u + \"  \" + 34m)");
			com.DynamicInvoke();
			// com2.DynamicInvoke();
		}
	}
}