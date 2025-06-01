using System;
using System.Collections.Generic;
using System.IO;

namespace TemplateGenerator
{
	public class Logger : ILogger
	{
		private readonly LogLevel logLevel;
		private readonly string logFilePath;

		private List<string> messages;
		private Dictionary<string, int> counters = new();

		public Logger(LogLevel logLevel, string logFilePath)
		{
			this.logLevel = logLevel;
			this.logFilePath = logFilePath;
			this.messages = new();
		}

		public bool IsEnabled(LogLevel logLevel)
		{
			return logLevel >= this.logLevel;
		}

		public void Log(LogLevel logLevel, string message)
		{
			if (!IsEnabled(logLevel))
				return;
			try
			{
				messages.Add($"[{DateTime.Now:O} | {logLevel}] {message}");
				Console.WriteLine($"[{DateTime.Now:O} | {logLevel}] {message}");
				File.AppendAllText(logFilePath, $"[{DateTime.Now:O} | {logLevel}] {message}{Environment.NewLine}");
			}
			catch (Exception ex)
			{
				SelfLog.Write(ex.ToString());
			}
		}

		public void LogCounter(string counter)
		{
			if (!counters.ContainsKey(counter))
				counters.Add(counter, 0);

			counters[counter]++;
		}

		public List<string> GetMessages()
		{
			return messages;
		}
	}

	public class ListLogger : ILogger
	{
		private readonly List<(LogLevel, string)> logMessages;

		public ListLogger(List<(LogLevel, string)> logMessages)
		{
			this.logMessages = logMessages;
		}

		public bool IsEnabled(LogLevel level)
			 => true;

		public void Log(LogLevel logLevel, string message)
		{
			logMessages.Add((logLevel, message));
		}

		public List<string> GetMessages()
		{
			var messages = new List<string>();
			foreach (var (level, message) in logMessages)
			{
				messages.Add($"[{DateTime.Now:O} | {level}] {message}");
			}

			return messages;
		}

		public void LogCounter(string counter)
		{
			Log(LogLevel.Trace, $"Counter: {counter} incremented.");
		}
	}
}
