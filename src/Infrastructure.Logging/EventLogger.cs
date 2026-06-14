using System.Text.Json;

namespace Infrastructure.Logging;

public sealed class EventLogger(string source)
{
    private static readonly Lock WriteLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string DirectoryPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs");
    private static DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    public void Info(string text)
    {
        Log(LogLevel.Info, text);
    }

    public void Warning(string text)
    {
        Log(LogLevel.Warning, text);
    }

    public void Error(string text, Exception? exception = null)
    {
        Log(LogLevel.Error, exception is null ? text : $"{text}: {exception.Message}");
    }

    private void Log(LogLevel level, string text)
    {
        var timestamp = FormatElapsedTime();
        var entry = new{level = level.ToString(), timestamp, source, text};
        WriteLogLines(entry, FormatReadableLine(level, timestamp, text));
    }

    private static string FormatElapsedTime()
    {
        var elapsed = DateTimeOffset.UtcNow - StartTime;

        return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";
    }

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