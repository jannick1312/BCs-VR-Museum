using System.IO;
using System.Text.Json;
using Godot;

namespace BCSVRMuseum;

/// <summary>
/// Loads and checks application settings for the current platform.
/// </summary>
public static class AppSettingsLoader
{
	private const string ConfigFileName = "config.json";
	private const string AndroidExternalConfigPath = "/sdcard/Android/data/VR.Museum/files/config.json";
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

	/// <summary>
	/// Loads settings from the config file.
	/// </summary>
	/// <param name="source">The config file path or the built-in default label.</param>
	/// <returns>The loaded application settings.</returns>
	public static AppSettings Load(out string source)
	{
		source = "";

		foreach (var candidate in GetCandidates())
		{
			if (!TryRead(candidate, out var json))
				continue;

			source = candidate;
			return Deserialize(json);
		}

		source = "built-in default";
		return new AppSettings();
	}

	/// <summary>
	/// Reads config values from text and fixes invalid values.
	/// </summary>
	/// <param name="json">The config file contents.</param>
	/// <returns>The checked application settings.</returns>
	private static AppSettings Deserialize(string json)
	{
		try
		{
			var settings = JsonSerializer.Deserialize<AppSettings>(json, JsonOptions) ?? new AppSettings();
			var defaults = new AppSettings();

			if (!Ipv4AddressValidator.IsValid(settings.ServerIp ?? ""))
				settings.ServerIp = defaults.ServerIp;

			if (string.IsNullOrWhiteSpace(settings.Query))
				settings.Query = defaults.Query;

			return settings;
		}
		catch (JsonException)
		{
			return new AppSettings();
		}
	}

	/// <summary>
	/// Gets the config file paths for the current platform.
	/// </summary>
	/// <returns>The config file paths in search order.</returns>
	private static string[] GetCandidates()
	{
		if (OS.GetName() == "Android")
			return [AndroidExternalConfigPath];

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		var sharedRunDirectory = Directory.GetParent(executableDirectory)?.FullName ?? executableDirectory;
		return [Path.Combine(sharedRunDirectory, ConfigFileName)];
	}

	/// <summary>
	/// Reads a config file when it exists.
	/// </summary>
	/// <param name="path">The config file path.</param>
	/// <param name="json">The file contents when reading succeeds.</param>
	/// <returns><see langword="true"/> if the file was read and <see langword="false"/> otherwise.</returns>
	private static bool TryRead(string path, out string json)
	{
		json = "";

		if (!File.Exists(path))
			return false;

		json = File.ReadAllText(path);
		return true;
	}
}
