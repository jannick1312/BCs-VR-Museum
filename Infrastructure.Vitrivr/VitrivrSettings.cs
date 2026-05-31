namespace Infrastructure.Vitrivr;

public class VitrivrSettings(bool deployed, string deployedIp, string streamedIp, string mediaFolderPath)
{
    private bool Deployed { get; set; } = deployed;
    private string DeployedIp { get; set; } = CleanIp(deployedIp);
    private string StreamedIp { get; set; } = CleanIp(streamedIp);

    public string MediaFolderPath { get; } = mediaFolderPath;

    public VitrivrMode Mode => Deployed
        ? VitrivrMode.Deployed
        : VitrivrMode.Streamed;

    public string CurrentIp => Deployed
        ? DeployedIp.Trim()
        : StreamedIp.Trim();

    public string QueryUrl => "http://" + CurrentIp + ":7070/api/sandbox/query";
    public string MediaBaseUrl => "http://" + CurrentIp + ":9090/";

    public void SetDeployed(bool deployed)
    {
        Deployed = deployed;
    }

    public void SetCurrentIp(string ip)
    {
        if (string.IsNullOrWhiteSpace(ip))
            return;

        if (Deployed)
            DeployedIp = CleanIp(ip);
        else
            StreamedIp = CleanIp(ip);
    }

    private static string CleanIp(string input)
    {
        var cleaned = input.Trim();

        cleaned = cleaned.Replace("http://", "");
        cleaned = cleaned.Replace("https://", "");

        if (cleaned.Contains(':'))
            cleaned = cleaned.Split(':')[0];

        if (cleaned.Contains('/'))
            cleaned = cleaned.Split('/')[0];

        return cleaned;
    }
}