using Godot;
using Infrastructure.Configuration;
using Infrastructure.Vitrivr;

namespace BCSVRMuseum;

public partial class ServerUrlStore : Node
{
    private AppSettings _appSettings;
    private VitrivrSettings _settings;

    public bool Deployed => _settings.Mode == VitrivrMode.Deployed;

    public string CurrentIp => _settings.CurrentIp;
    private string QueryUrl => _settings.QueryUrl;
    private string MediaBaseUrl => _settings.MediaBaseUrl;
    private string MediaFolderPath => _settings.MediaFolderPath;

    public VitrivrSettings Settings => _settings;

    public override void _Ready()
    {
        const string settingsPath = "res://appsettings.json";

        _appSettings = new AppSettings();
        using var file = FileAccess.Open(settingsPath, FileAccess.ModeFlags.Read);
        var json = file.GetAsText();
       _appSettings = AppSettingsLoader.LoadFromJson(json);
       
        _settings = new VitrivrSettings(_appSettings.Deployed, _appSettings.DefaultDeployedIp, _appSettings.DefaultStreamedIp, _appSettings.MediaFolderPath);
    }

    public void SetServerIp(string ip)
    {
        _settings.SetCurrentIp(ip);
    }

    public void RevertServerUrl()
    {
        var deployed = Deployed;
        _settings = new VitrivrSettings(deployed, _appSettings.DefaultDeployedIp, _appSettings.DefaultStreamedIp, _appSettings.MediaFolderPath);
    }

    public void SetDeployed(bool deployed)
    {
        _settings.SetDeployed(deployed);
    }
}