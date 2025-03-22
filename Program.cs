namespace FileFactory
{
	internal static class Program
	{
		[STAThread]
		static void Main()
		{			
			ApplicationConfiguration.Initialize();
			Application.SetCompatibleTextRenderingDefault(false);
			Application.Run(new Form1());
		}
	}
}