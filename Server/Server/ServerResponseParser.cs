using System.Text.Json;

namespace Server;

public static class ServerResponseParser
{
    public static ServerResult Parse(string responseText, string mediaFolderPath, string mediaBaseUrl)
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
            string remoteImageUrl = mediaBaseUrl.TrimEnd('/') + "/" + filename;

            return ServerResult.FromImage(filename, localImagePath, remoteImageUrl);
        }
        catch
        {
            return ServerResult.Fail("Could not parse response.");
        }
    }
}