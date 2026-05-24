using Godot;

public partial class ServerUrlStore : Node
{
    [Export]
    public string DefaultServerUrl = "http://192.168.1.21:5050/";

    public string CurrentServerUrl { get; private set; }

    public override void _Ready()
    {
        CurrentServerUrl = NormalizeBaseUrl(DefaultServerUrl);
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

    private string NormalizeBaseUrl(string url)
    {
        return url.Trim().TrimEnd('/') + "/";
    }
}