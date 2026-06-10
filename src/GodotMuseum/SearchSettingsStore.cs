using Core;
using Godot;
using Infrastructure.Configuration;

namespace BCSVRMuseum;

public partial class SearchSettingsStore : Node
{
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

        _runtimeSettings = new RuntimeSearchSettings(appSettings.Deployed, appSettings.DefaultDeployedIp, appSettings.DefaultStreamedIp, appSettings.MediaFolderPath);
    }

    public void SetServerIp(string ip)
    {
        _runtimeSettings.SetCurrentIp(ip);
    }

    public void RevertServerUrl()
    {
        _runtimeSettings.RevertCurrentIp();
    }

    public void SetDeployed(bool deployed)
    {
        _runtimeSettings.SetDeployed(deployed);
    }
}