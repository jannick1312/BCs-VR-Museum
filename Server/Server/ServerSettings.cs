namespace Server;

public class ServerSettings
{
    public bool Deployed { get; private set; }

    public string DeployedServerUrl { get; private set; }
    public string StreamedServerUrl { get; private set; }

    public ServerMode Mode => Deployed
        ? ServerMode.Deployed
        : ServerMode.Streamed;

    public string CurrentServerUrl => Deployed
        ? NormalizeBaseUrl(DeployedServerUrl)
        : StreamedServerUrl.Trim();

    public ServerSettings(bool deployed,  string deployedServerUrl, string streamedServerUrl)
    {
        Deployed = deployed;
        DeployedServerUrl = deployedServerUrl;
        StreamedServerUrl = streamedServerUrl;
    }

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;
    }

    public void SetServerUrl(string newUrl)
    {
        if (string.IsNullOrWhiteSpace(newUrl))
            return;

        if (Deployed)
            DeployedServerUrl = NormalizeBaseUrl(newUrl);
        else
            StreamedServerUrl = newUrl.Trim();
    }

    public static string NormalizeBaseUrl(string url)
    {
        return url.Trim().TrimEnd('/') + "/";
    }
}