using Godot;

public partial class ServerUrlStore : Node
{
	[Export]
	public string DefaultServerUrl = "http://192.168.1.140:5050/search_one";

	public string CurrentServerUrl { get; private set; }

	public override void _Ready()
	{
		CurrentServerUrl = DefaultServerUrl;
	}

	public void SetServerUrl(string newUrl)
	{
		if (string.IsNullOrWhiteSpace(newUrl))
			return;
		CurrentServerUrl = newUrl.Trim();
	}

	public void RevertServerUrl()
	{
		CurrentServerUrl = DefaultServerUrl;
	}
}
