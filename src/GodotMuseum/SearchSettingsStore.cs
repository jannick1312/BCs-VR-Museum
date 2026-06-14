using Core;
using Godot;
using Infrastructure.Configuration;
using Infrastructure.Logging;

namespace BCSVRMuseum;

public partial class SearchSettingsStore : Node
{
    private readonly EventLogger _logger = new(nameof(SearchSettingsStore));
    private RuntimeSearchSettings _runtimeSettings;

    public bool Deployed => _runtimeSettings.Deployed;
    public string CurrentIp => _runtimeSettings.CurrentIp;
    public string MediaFolderPath => _runtimeSettings.MediaFolderPath;

    public override void _Ready()
    {
        const string settingsPath = "res://appsettings.json";

        using var file = FileAccess.Open(settingsPath, FileAccess.ModeFlags.Read );

        var json = file.GetAsText();
        var appSettings = AppSettingsLoader.LoadFromJson(json);

        var logDirectoryPath = ResolvePath(appSettings.LogDirectoryPath);
        EventLogger.Configure(logDirectoryPath);
        GD.Print($"Logs are written to: " + logDirectoryPath + "/app.log");
        _logger.Info($"Settings loaded.");

        _runtimeSettings = new RuntimeSearchSettings(appSettings.Deployed, appSettings.DefaultDeployedIp, appSettings.DefaultStreamedIp, appSettings.MediaFolderPath);
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
}