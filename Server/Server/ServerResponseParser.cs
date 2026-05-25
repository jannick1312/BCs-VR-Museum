using System.Text.Json;

namespace Server;

public static class ServerResponseParser
{
	public static ServerResult Parse(string responseText, ServerMode mode, string currentServerUrl, string mediaFolderPath)
	{
		if (mode == ServerMode.Deployed)
			return ParseDeployed(responseText, currentServerUrl);
		return ParseStreamed(responseText, mediaFolderPath);
	}

	private static ServerResult ParseDeployed(string responseText, string currentServerUrl)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(responseText);
			JsonElement root = document.RootElement;

			root.TryGetProperty("filename", out JsonElement filenameElement);

			string? filename = filenameElement.GetString();
			string imageUrl = ServerSettings.NormalizeBaseUrl(currentServerUrl) + "media/" + filename;
			return ServerResult.FromUrl(imageUrl);
		}
		catch
		{
			return ServerResult.Fail("Could not parse response.");
		}
	}

	private static ServerResult ParseStreamed(string responseText, string mediaFolderPath)
	{
		try
		{
			using JsonDocument document = JsonDocument.Parse(responseText);
			JsonElement root = document.RootElement;

			root.TryGetProperty("retrievables", out JsonElement retrievables);

			JsonElement best = retrievables[0];
			best.TryGetProperty("descriptors", out JsonElement descriptors);
			descriptors.TryGetProperty("file.path", out JsonElement pathElement);

			string? dockerPath = pathElement.GetString();

			string? filename = Path.GetFileName(dockerPath);
			string localImagePath = Path.Combine(mediaFolderPath, filename);

			return ServerResult.FromLocalPath(localImagePath);
		}
		catch
		{
			return ServerResult.Fail("Could not parse response.");
		}
	}
}