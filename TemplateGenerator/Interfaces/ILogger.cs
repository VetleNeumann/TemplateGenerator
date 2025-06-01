using Microsoft.CodeAnalysis;
using System;
using System.Collections.Generic;

namespace TemplateGenerator
{
	public interface ILogger
	{
		bool IsEnabled(LogLevel level);

		void Log(LogLevel level, string message);

		void LogCounter(string counter);

		List<string> GetMessages();
	}

	static class LogLevelExtensions
	{
		public static void Log(this ILogger logger, Diagnostic diagnostic)
		{
			if (logger.IsEnabled(diagnostic.Severity.ToLogLevel()))
			{
				logger.Log(diagnostic.Severity.ToLogLevel(), $"[{diagnostic.Id}] {diagnostic.GetMessage()}");
			}
		}

		public static LogLevel ToLogLevel(this DiagnosticSeverity severity)
		{
			return severity switch
			{
				DiagnosticSeverity.Error => LogLevel.Error,
				DiagnosticSeverity.Warning => LogLevel.Warning,
				DiagnosticSeverity.Info => LogLevel.Information,
				DiagnosticSeverity.Hidden => LogLevel.None,
				_ => LogLevel.Trace,
			};
		}
	}
}
