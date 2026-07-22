namespace BCSVRMuseum;

public sealed class AppSettings
{
	public string ServerIp { get; set; } = "10.34.64.208";
	public bool Tutorial { get; init; } = true;
	public string Query { get; set; } = "default";
}
