using System.IO;
using Godot;
using Logger;

namespace BCSVRMuseum;

/// <summary>
/// Loads and stores the settings required for media searches.
/// </summary>
public partial class SearchSettingsStore : Node
{
	private static readonly string[] MediaSubdirectories = ["images", "videos", "3dPck"];

	private readonly EventLogger _logger = new(nameof(SearchSettingsStore));

	private string _configSource = "";
	private string _mediaFolderPath;
	private RuntimeSearchSettings _runtimeSettings;
	private string _serverIp = "";
	private bool _tutorialEnabled;
	public string ConfiguredQuery { get; private set; } = "";
	public string CurrentIp => _runtimeSettings.CurrentIp;
	public string CurrentMediaFolderPath => _runtimeSettings.MediaFolderPath;
	public MuseumEntryState EntryState { get; private set; }

	/// <summary>
	/// Sets up logging before child nodes enter the scene tree.
	/// </summary>
	public override void _EnterTree()
	{
		var logDirectoryPath = ResolveApplicationDirectory("logs");
		EventLogger.Configure(logDirectoryPath);
	}

	/// <summary>
	/// Loads the settings and prepares the local media folder.
	/// </summary>
	public override void _Ready()
	{
		LoadJsonSettings();

		_mediaFolderPath = ResolveMediaDirectory();
		CreateMediaDirectories(_mediaFolderPath);

		_runtimeSettings = new RuntimeSearchSettings(_serverIp, _mediaFolderPath);
		EntryState = new MuseumEntryState(_tutorialEnabled);

		var runtimeProfile = ResolveRuntimeProfile();
		if (runtimeProfile is not null)
			_logger.Info($"Search settings initialized. RuntimeProfile='{runtimeProfile}', ConfigSource='{_configSource}', ServerIp='{CurrentIp}', Tutorial={EntryState.TutorialEnabled}, Query='{ConfiguredQuery}'.");
	}

	/// <summary>
	/// Uses settings loaded from the config file.
	/// </summary>
	private void LoadJsonSettings()
	{
		var settings = AppSettingsLoader.Load(out var source);
		_serverIp = settings.ServerIp;
		_tutorialEnabled = settings.Tutorial;
		ConfiguredQuery = settings.Query;
		_configSource = source;
	}

	/// <summary>
	/// Changes the server address used for searches.
	/// </summary>
	/// <param name="ip">The new server address.</param>
	public void SetServerIp(string ip)
	{
		_runtimeSettings.SetCurrentIp(ip);
		_logger.Info($"Server IP changed. CurrentIp='{CurrentIp}'.");
	}

	/// <summary>
	/// Restores the server address.
	/// </summary>
	public void RevertServerUrl()
	{
		_runtimeSettings.RevertCurrentIp();
		_logger.Info($"Server IP reverted. CurrentIp='{CurrentIp}'.");
	}

	/// <summary>
	/// Creates the local media folder and its subfolders.
	/// </summary>
	/// <param name="mediaFolderPath">The local media folder used by the application.</param>
	private static void CreateMediaDirectories(string mediaFolderPath)
	{
		Directory.CreateDirectory(mediaFolderPath);
		foreach (var subdirectory in MediaSubdirectories)
			Directory.CreateDirectory(Path.Combine(mediaFolderPath, subdirectory));
	}

	/// <summary>
	/// Gets the current streaming profile.
	/// </summary>
	/// <returns>The runtime profile name, or <see langword="null"/> when none is detected.</returns>
	private static string ResolveRuntimeProfile()
	{
		if (OS.HasFeature("quest"))
			return "quest";

		if (OS.HasFeature("focus"))
			return "focus";

		return OS.HasFeature("streaming") ? "streamed" : null;
	}

	/// <summary>
	/// Gets a writable folder for the current platform.
	/// </summary>
	/// <param name="directoryName">The subdirectory name.</param>
	/// <returns>The folder path for the current platform.</returns>
	private static string ResolveApplicationDirectory(string directoryName)
	{
		if (OS.GetName() == "Android")
			return $"/sdcard/Android/data/VR.Museum/files/{directoryName}";

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		return Path.Combine(executableDirectory, directoryName);
	}

	/// <summary>
	/// Gets the local media folder for the current platform.
	/// </summary>
	/// <returns>The local media folder used by the application.</returns>
	private static string ResolveMediaDirectory()
	{
		if (OS.GetName() == "Android")
			return "/sdcard/Android/data/VR.Museum/files/media";

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		var sharedRunDirectory = Directory.GetParent(executableDirectory)?.FullName ?? executableDirectory;
		return Path.Combine(sharedRunDirectory, "media");
	}
}
