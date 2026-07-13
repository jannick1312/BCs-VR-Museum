namespace BCSVRMuseum;

public class RuntimeSearchSettings
{
	private string InitialIp { get; }
	public string MediaFolderPath { get; }

	public string CurrentIp { get; private set; }

	public RuntimeSearchSettings(string defaultIp, string mediaFolderPath)
	{
		InitialIp = CleanIp(defaultIp);
		MediaFolderPath = mediaFolderPath;
		CurrentIp = InitialIp;
	}

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