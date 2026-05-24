using Godot;

public partial class ServerUrlStore : Node
{
    [Export] public string DefaultServerUrl = "http://192.168.1.21:5050/";
    [Export] public bool Deployed = false;

    public string CurrentServerUrl { get; private set; }

    public override void _Ready()
    {
        CurrentServerUrl = NormalizeBaseUrl(DefaultServerUrl);
        SetDeployed(Deployed);
    }

    public void SetServerUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return;
        CurrentServerUrl = NormalizeBaseUrl(newUrl);
    }

    public void RevertServerUrl()
    {
        CurrentServerUrl = NormalizeBaseUrl(DefaultServerUrl);
    }

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;

        if (Deployed)
            GD.Print("APP MODE: DEPLOYED");
        else
            GD.Print("APP MODE: NOT DEPLOYED");
    }

    private string NormalizeBaseUrl(string url)
    {
        return url.Trim().TrimEnd('/') + "/";
    }
}