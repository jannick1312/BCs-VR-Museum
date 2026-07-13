using System;
using System.IO;
using System.Text.Json;
using Godot;
using FileAccess = Godot.FileAccess;

namespace BCSVRMuseum;

public static class AppSettingsLoader
{
	private const string ProjectConfigPath = "res://config.json";
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
			return [AndroidExternalConfigPath, ProjectConfigPath];

		if (OS.HasFeature("editor"))
			return [ProjectConfigPath];

		var executableDirectory = Path.GetDirectoryName(OS.GetExecutablePath()) ?? "";
		return [Path.Combine(executableDirectory, ConfigFileName), ProjectConfigPath];
	}

	private static bool TryRead(string path, out string json)
	{
		json = "";

		if (path.StartsWith("res://", StringComparison.Ordinal) || path.StartsWith("user://", StringComparison.Ordinal))
		{
			if (!FileAccess.FileExists(path))
				return false;

			json = FileAccess.GetFileAsString(path);
			return true;
		}

		if (!File.Exists(path))
			return false;

		json = File.ReadAllText(path);
		return true;
	}
}
