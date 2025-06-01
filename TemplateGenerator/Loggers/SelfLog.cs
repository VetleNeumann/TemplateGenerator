using System;
using System.Diagnostics;
using System.IO;

namespace TemplateGenerator
{
	public class SelfLog
	{
		private const string FILE_NAME = "DemoSourceGenerator.log";

		public static void Write(string message)
		{
			try
			{
				var fullPath = Path.Combine(Path.GetTempPath(), FILE_NAME);
				File.AppendAllText(fullPath, $"[{DateTime.Now:O}] {message}{Environment.NewLine}");
			}
			catch (Exception ex)
			{
				Debug.WriteLine(ex);
			}
		}
	}
}
