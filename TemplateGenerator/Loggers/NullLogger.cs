using System.Collections.Generic;
using System.Text;

namespace TemplateGenerator
{
	public class NullLogger : ILogger
	{
		public static readonly ILogger Instance = new NullLogger();

		public bool IsEnabled(LogLevel level)
			=> false;

		public void Log(LogLevel level, string message) {}

		public List<string> GetMessages()
		{
			return new List<string>();
		}

		public void LogCounter(string counter) {}
	}
}
