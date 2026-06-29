using Godot;
using Logger;

namespace BCSVRMuseum;

public partial class SearchSettingsStore : Node
{
    [Export] public bool DefaultDeployed = true;
    [Export] public string DefaultDeployedIp = "192.168.1.140";
    [Export] public string DefaultStreamedIp = "10.34.64.208";
    [Export] public string MediaFolderPath = @"C:\Users\dbis-\Desktop\BCs\media";
    [Export] public string LogDirectoryPath = @"C:\Users\dbis-\Desktop\logs\";
    [Export] public string FallbackLogDirectoryPath = "/sdcard/Android/data/VR.Museum/files/logs";

    private readonly EventLogger _logger = new(nameof(SearchSettingsStore));
    private RuntimeSearchSettings _runtimeSettings;

    public bool Deployed => _runtimeSettings.Deployed;
    public string CurrentIp => _runtimeSettings.CurrentIp;
    public string CurrentMediaFolderPath => _runtimeSettings.MediaFolderPath;

    public override void _Ready()
    {
        var logDirectoryPath = ResolveLogDirectoryPath(LogDirectoryPath, FallbackLogDirectoryPath);
        EventLogger.Configure(logDirectoryPath);
        GD.Print("JSON-Logs are written to: " + logDirectoryPath + "/app.log");
        GD.Print("Readable logs are written to:" + logDirectoryPath + "/app-readable.log");
        _logger.Info("Settings loaded.");

        _runtimeSettings = new RuntimeSearchSettings(DefaultDeployed, DefaultDeployedIp, DefaultStreamedIp, MediaFolderPath);
    }

    public void SetServerIp(string ip)
    {
        _runtimeSettings.SetCurrentIp(ip);
        _logger.Info($"Server IP changed. CurrentIp={CurrentIp}");
    }

    public void RevertServerUrl()
    {
        _runtimeSettings.RevertCurrentIp();
        _logger.Info($"Server IP reverted. CurrentIp={CurrentIp}");
    }

    public void SetDeployed(bool deployed)
    {
        _runtimeSettings.SetDeployed(deployed);
        _logger.Info($"Deployment mode changed. CurrentIp={CurrentIp}");
    }

    private static string ResolvePath(string path)
    {
        if (path.StartsWith("user://") || path.StartsWith("res://"))
            return ProjectSettings.GlobalizePath(path);

        return path;
    }

    private static string ResolveLogDirectoryPath(string primaryPath, string fallbackPath)
    {
        var resolvedPrimaryPath = ResolvePath(primaryPath);

        if (System.IO.Directory.Exists(resolvedPrimaryPath))
            return resolvedPrimaryPath;

        var resolvedFallbackPath = ResolvePath(fallbackPath);
        GD.Print($"Log directory does not exist: {resolvedPrimaryPath}. Falling back to: {resolvedFallbackPath}");

        return resolvedFallbackPath;
    }
}