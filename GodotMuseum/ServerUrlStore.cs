using Godot;
using Server;
namespace BCSVRMuseum;

public partial class ServerUrlStore : Node
{
    [Export] public bool Deployed = true;

    [Export] public string DefaultDeployedIp = "192.168.1.21";
    [Export] public string DefaultStreamedIp = "10.34.64.208";

    private ServerSettings _settings;

    public string CurrentIp => _settings.CurrentIp;
    public string QueryUrl => _settings.QueryUrl;
    public string MediaBaseUrl => _settings.MediaBaseUrl;
    private ServerMode Mode => _settings.Mode;

    public override void _Ready()
    {
        _settings = new ServerSettings(Deployed, DefaultDeployedIp, DefaultStreamedIp);
    }

    public void SetServerIp(string ip)
    {
        _settings.SetCurrentIp(ip);
    }

    public void RevertServerUrl()
    {
        _settings = new ServerSettings(Deployed, DefaultDeployedIp, DefaultStreamedIp);
    }

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;
        _settings.SetDeployed(deployed);
    }
}