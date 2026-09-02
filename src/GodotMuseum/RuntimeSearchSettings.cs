namespace BCSVRMuseum;

/// <summary>
/// Manages the server address and local media folder used for searches.
/// </summary>
public class RuntimeSearchSettings
{
	/// <summary>
	/// Sets the search values while the application is running.
	/// </summary>
	/// <param name="defaultIp">The server address restored by a reset.</param>
	/// <param name="mediaFolderPath">The local media folder used by the application.</param>
	public RuntimeSearchSettings(string defaultIp, string mediaFolderPath)
	{
		InitialIp = CleanIp(defaultIp);
		MediaFolderPath = mediaFolderPath;
		CurrentIp = InitialIp;
	}

	private string InitialIp { get; }
	public string MediaFolderPath { get; }
	public string CurrentIp { get; private set; }

	/// <summary>
	/// Replaces the server address.
	/// </summary>
	/// <param name="ip">The new server address.</param>
	public void SetCurrentIp(string ip)
	{
		CurrentIp = CleanIp(ip);
	}

	/// <summary>
	/// Restores the server address.
	/// </summary>
	public void RevertCurrentIp()
	{
		CurrentIp = InitialIp;
	}

	/// <summary>
	/// Removes spaces before and after a server address.
	/// </summary>
	/// <param name="input">The server address to clean.</param>
	/// <returns>The trimmed server address.</returns>
	private static string CleanIp(string input)
	{
		return input.Trim();
	}
}
