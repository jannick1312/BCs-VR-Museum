namespace BCSVRMuseum;

public class RuntimeSearchSettings
{
	public bool Deployed { get; private set; }
	private string DefaultDeployedIp { get; }
	private string DefaultStreamedIp { get; }
	public string MediaFolderPath { get; }

	private string DeployedIp { get; set; }
	private string StreamedIp { get; set; }

	public string CurrentIp => Deployed ? DeployedIp : StreamedIp;

	public RuntimeSearchSettings(bool deployed, string defaultDeployedIp, string defaultStreamedIp, string mediaFolderPath)
	{
		Deployed = deployed;
		DefaultDeployedIp = CleanIp(defaultDeployedIp);
		DefaultStreamedIp = CleanIp(defaultStreamedIp);
		MediaFolderPath = mediaFolderPath;

		DeployedIp = DefaultDeployedIp;
		StreamedIp = DefaultStreamedIp;
	}

	public void SetDeployed(bool deployed)
	{
		Deployed = deployed;
	}

	public void SetCurrentIp(string ip)
	{
		if (Deployed)
			DeployedIp = CleanIp(ip);
		else
			StreamedIp = CleanIp(ip);
	}

	public void RevertCurrentIp()
	{
		if (Deployed)
			DeployedIp = DefaultDeployedIp;
		else
			StreamedIp = DefaultStreamedIp;
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