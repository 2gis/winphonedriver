namespace TestApp
{
	using System.Windows;

	public class AutomationApi
	{
		public static void Alert(string text)
		{
			MessageBox.Show(text);
		}

		public static void AlertEmpty()
		{
			MessageBox.Show("Empty alert");
		}
	}
}
