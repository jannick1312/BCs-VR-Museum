using Godot;
using Logger;

namespace BCSVRMuseum;

public partial class SearchSettingsStore : Node
{
	private string _mediaFolderPath;
	private string _serverIp = "";
	private string _configSource = "";

	private readonly EventLogger _logger = new(nameof(SearchSettingsStore));
	private RuntimeSearchSettings _runtimeSettings;

	public string CurrentIp => _runtimeSettings.CurrentIp;
	public string CurrentMediaFolderPath => _runtimeSettings.MediaFolderPath;

	public override void _EnterTree()
	{
		var logDirectoryPath = ResolveApplicationDirectory("logs", writable: true);
		EventLogger.Configure(logDirectoryPath);
	}

	public override void _Ready()
	{
		LoadJsonSettings();

		_mediaFolderPath = ResolveApplicationDirectory("media", writable: false);

		_runtimeSettings = new RuntimeSearchSettings(_serverIp, _mediaFolderPath);

		var runtimeProfile = ResolveRuntimeProfile();
		if (runtimeProfile is not null)
			_logger.Info($"Search settings initialized. RuntimeProfile='{runtimeProfile}', ConfigSource='{_configSource}', ServerIp='{CurrentIp}'.");
	}

	private void LoadJsonSettings()
	{
		var settings = AppSettingsLoader.Load(out var source);
		_serverIp = settings.ServerIp;
		_configSource = source;
	}

	public void SetServerIp(string ip)
	{
		_runtimeSettings.SetCurrentIp(ip);
		_logger.Info($"Server IP changed. CurrentIp='{CurrentIp}'.");
	}

	public void RevertServerUrl()
	{
		_runtimeSettings.RevertCurrentIp();
		_logger.Info($"Server IP reverted. CurrentIp='{CurrentIp}'.");
	}

	private static string ResolveRuntimeProfile()
	{
		if (OS.HasFeature("quest"))
			return "quest";

		if (OS.HasFeature("focus"))
			return "focus";

		return OS.HasFeature("streaming") ? "streamed" : null;
	}

	private static string ResolveApplicationDirectory(string directoryName, bool writable)
	{
		if (OS.GetName() == "Android")
			return $"/sdcard/Android/data/VR.Museum/files/{directoryName}";

		if (OS.HasFeature("editor"))
		{
			var godotPath = writable ? $"user://{directoryName}" : $"res://{directoryName}";
			return ProjectSettings.GlobalizePath(godotPath);
		}

		var executableDirectory = System.IO.Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		return System.IO.Path.Combine(executableDirectory, directoryName);
	}
}