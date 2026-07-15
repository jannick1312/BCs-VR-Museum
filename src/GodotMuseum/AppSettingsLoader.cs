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
			return JsonSerializer.Deserialize<AppSettings>(json, JsonOptions)!;
		}

		source = "built-in default";
		return new AppSettings();
	}

	private static string[] GetCandidates()
	{
		if (OS.GetName() == "Android")
			return [AndroidExternalConfigPath];

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		return [Path.Combine(executableDirectory, ConfigFileName)];
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
