using Godot;
using Server;

public partial class ServerUrlStore : Node
{
    [Export] public bool Deployed = true;

    [Export] public string DefaultDeployedServerUrl = "http://192.168.1.21:5050/";
    [Export] public string DefaultStreamedServerUrl = "http://10.34.64.208:7070/api/sandbox/query";

    private ServerSettings _settings;

    public string CurrentServerUrl => _settings.CurrentServerUrl;
    public ServerMode Mode => _settings.Mode;

    public override void _Ready()
    {
        _settings = new ServerSettings(Deployed, DefaultDeployedServerUrl,  DefaultStreamedServerUrl);
    }

    public void SetServerUrl(string newUrl)
    {
        _settings.SetServerUrl(newUrl);
    }

    public void RevertServerUrl()
    {
        _settings = new ServerSettings(Deployed, DefaultDeployedServerUrl, DefaultStreamedServerUrl);
    }

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;
        _settings.SetDeployed(deployed);
    }
}