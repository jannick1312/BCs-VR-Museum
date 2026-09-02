using System.Text.Json;

namespace Logger;

/// <summary>
/// Writes detailed and easy-to-read log files.
/// </summary>
/// <param name="source">The component name included in each log entry.</param>
public sealed class EventLogger(string source)
{
	private static readonly Lock WriteLock = new();
	private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);
	private static string DirectoryPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs");
	private static DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

	/// <summary>
	/// Sets the log folder and starts a new log.
	/// </summary>
	/// <param name="directoryPath">The directory where log files are written.</param>
	public static void Configure(string directoryPath)
	{
		DirectoryPath = directoryPath;
		StartTime = DateTimeOffset.UtcNow;
		Directory.CreateDirectory(DirectoryPath);
		File.WriteAllText(Path.Combine(DirectoryPath, "app.log"), "");
		File.WriteAllText(Path.Combine(DirectoryPath, "app-readable.log"), "");
	}

	/// <summary>
	/// Writes an information message.
	/// </summary>
	/// <param name="text">The message to write.</param>
	public void Info(string text)
	{
		Log(LogLevel.Info, text);
	}

	/// <summary>
	/// Writes a warning log entry.
	/// </summary>
	/// <param name="text">The message to write.</param>
	public void Warning(string text)
	{
		Log(LogLevel.Warning, text);
	}

	/// <summary>
	/// Writes an error message with optional error details.
	/// </summary>
	/// <param name="text">The message to write.</param>
	/// <param name="exception">The exception for the error.</param>
	public void Error(string text, Exception? exception = null)
	{
		Log(LogLevel.Error, exception is null ? text : $"{text}: {exception.Message}");
	}

	/// <summary>
	/// Creates and writes a log message at the selected level.
	/// </summary>
	/// <param name="level">The message level.</param>
	/// <param name="text">The message to write.</param>
	private void Log(LogLevel level, string text)
	{
		var timestamp = FormatElapsedTime();
		var entry = new { level = level.ToString(), timestamp, source, text };
		WriteLogLines(entry, FormatReadableLine(level, timestamp, text));
	}

	/// <summary>
	/// Changes the time since logging started into text.
	/// </summary>
	/// <returns>The time used in log entries.</returns>
	private static string FormatElapsedTime()
	{
		var elapsed = DateTimeOffset.UtcNow - StartTime;

		return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";
	}

	/// <summary>
	/// Creates one line for the easy-to-read log.
	/// </summary>
	/// <param name="level">The severity of the entry.</param>
	/// <param name="timestamp">The time text for the message.</param>
	/// <param name="text">The log message.</param>
	/// <returns>The text log line.</returns>
	private string FormatReadableLine(LogLevel level, string timestamp, string text)
	{
		var prefix = level switch
		{
			LogLevel.Info => "[INFO]    ",
			LogLevel.Warning => "  [WARN]  ",
			LogLevel.Error => "    [ERR] ",
			_ => "[INFO]    "
		};

		return $"{prefix}{timestamp} {source} - {text}";
	}

	/// <summary>
	/// Writes a message to both log files.
	/// </summary>
	/// <param name="entry">The data written to the detailed log.</param>
	/// <param name="readableLine">The line written to the easy-to-read log.</param>
	private static void WriteLogLines(object entry, string readableLine)
	{
		var jsonLine = JsonSerializer.Serialize(entry, JsonOptions);
		lock (WriteLock)
		{
			Directory.CreateDirectory(DirectoryPath);
			File.AppendAllText(Path.Combine(DirectoryPath, "app.log"), jsonLine + Environment.NewLine);
			File.AppendAllText(Path.Combine(DirectoryPath, "app-readable.log"), readableLine + Environment.NewLine);
		}
	}
}



// Codex helped add log levels, timestamps, and a second log file to an earlier logger that wrote one file with only the source of each message.
