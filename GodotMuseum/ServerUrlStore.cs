using Godot;

public partial class ServerUrlStore : Node
{
    [Export] public bool Deployed = true;

    [Export] public string DeployedServerUrl = "http://192.168.1.21:5050/";
    [Export] public string LocalServerUrl = "http://10.34.64.208:7070/api/sandbox/query";

    public string CurrentServerUrl { get; private set; }

    public override void _Ready()
    {
        SetDeployed(Deployed);
    }

    public void SetServerUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return;

        if (Deployed)
            DeployedServerUrl = NormalizeBaseUrl(newUrl);
        else
            LocalServerUrl = newUrl.Trim();

        CurrentServerUrl = Deployed ? DeployedServerUrl : LocalServerUrl;
    }

    public void RevertServerUrl()
    {
        CurrentServerUrl = Deployed
            ? NormalizeBaseUrl(DeployedServerUrl)
            : LocalServerUrl.Trim();
    }

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;

        CurrentServerUrl = Deployed
            ? NormalizeBaseUrl(DeployedServerUrl)
            : LocalServerUrl.Trim();
    }

    private string NormalizeBaseUrl(string url)
    {
        return url.Trim().TrimEnd('/') + "/";
    }
}