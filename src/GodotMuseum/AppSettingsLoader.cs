using System.IO;
using System.Text.Json;
using Godot;

namespace BCSVRMuseum;

public static class AppSettingsLoader
{
	private const string ConfigFileName = "config.json";
	private const string AndroidExternalConfigPath = "/sdcard/Android/data/VR.Museum/files/config.json";
	private static readonly JsonSerializerOptions JsonOptions = new() { PropertyNameCaseInsensitive = true, ReadCommentHandling = JsonCommentHandling.Skip, AllowTrailingCommas = true };

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

	private static string[] GetCandidates()
	{
		if (OS.GetName() == "Android")
			return [AndroidExternalConfigPath];

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		var sharedRunDirectory = Directory.GetParent(executableDirectory)?.FullName ?? executableDirectory;
		return [Path.Combine(sharedRunDirectory, ConfigFileName)];
	}

	private static bool TryRead(string path, out string json)
	{
		json = "";

		if (!File.Exists(path))
			return false;

		json = File.ReadAllText(path);
		return true;
	}
}
