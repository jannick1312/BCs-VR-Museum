namespace BCSVRMuseum;

public class RuntimeSearchSettings
{
	public RuntimeSearchSettings(string defaultIp, string mediaFolderPath)
	{
		InitialIp = CleanIp(defaultIp);
		MediaFolderPath = mediaFolderPath;
		CurrentIp = InitialIp;
	}

	private string InitialIp { get; }
	public string MediaFolderPath { get; }
	public string CurrentIp { get; private set; }

	public void SetCurrentIp(string ip)
	{
		CurrentIp = CleanIp(ip);
	}

	public void RevertCurrentIp()
	{
		CurrentIp = InitialIp;
	}

	private static string CleanIp(string input)
	{
		return input.Trim();
	}
}
