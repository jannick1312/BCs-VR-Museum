using System.Text.Json;

namespace Server;

public static class ServerResponseParser
{
    public static ServerResult Parse(string responseText, string mediaFolderPath, string mediaBaseUrl)
    {
	    if (string.IsNullOrWhiteSpace(responseText))
		    return ServerResult.FromError("Server response is empty.");
	    
		using var document = JsonDocument.Parse(responseText);
		var root = document.RootElement;

		root.TryGetProperty("retrievables", out var retrievables);

		var best = retrievables[0];
		best.TryGetProperty("descriptors", out var descriptors);
		descriptors.TryGetProperty("file.path", out var pathElement);

		var dockerPath = pathElement.GetString();

        var filename = Path.GetFileName(dockerPath);
        var localImagePath = Path.Combine(mediaFolderPath, filename);
        var remoteImageUrl = mediaBaseUrl.TrimEnd('/') + "/" + filename;

        return ServerResult.FromImage(localImagePath, remoteImageUrl);
    }
}