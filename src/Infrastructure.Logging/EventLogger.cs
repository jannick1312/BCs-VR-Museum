using System.Text.Json;

namespace Infrastructure.Logging;

public sealed class EventLogger(string source)
{
    private static readonly Lock WriteLock = new();
    private static readonly JsonSerializerOptions JsonOptions = new(JsonSerializerDefaults.Web);

    private static string DirectoryPath { get; set; } = Path.Combine(AppContext.BaseDirectory, "logs");
    private static DateTimeOffset StartTime { get; set; } = DateTimeOffset.UtcNow;

    public static void Configure(string directoryPath)
    {
        DirectoryPath = directoryPath;
        StartTime = DateTimeOffset.UtcNow;
        Directory.CreateDirectory(DirectoryPath);
        File.WriteAllText(Path.Combine(DirectoryPath, "app.log"), "");
    }

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
        var entry = new{level = level.ToString(), timestamp = FormatElapsedTime(), source, text};
        WriteJsonLine(Path.Combine(DirectoryPath, "app.log"), entry);
    }

    private static string FormatElapsedTime()
    {
        var elapsed = DateTimeOffset.UtcNow - StartTime;

        return $"{(int)elapsed.TotalHours:00}:{elapsed.Minutes:00}:{elapsed.Seconds:00}.{elapsed.Milliseconds:000}";
    }

    private static void WriteJsonLine(string filePath, object entry)
    {
        var line = JsonSerializer.Serialize(entry, JsonOptions);
        lock (WriteLock){Directory.CreateDirectory(DirectoryPath);File.AppendAllText(filePath, line + Environment.NewLine);}
    }
}