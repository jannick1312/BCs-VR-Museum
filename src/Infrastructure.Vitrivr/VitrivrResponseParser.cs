using Core;
using Infrastructure.Logging;
using System.Text.Json;

namespace Infrastructure.Vitrivr;

public static class VitrivrResponseParser
{
    private static readonly EventLogger Logger = new(nameof(VitrivrResponseParser));

    public static SearchResult Parse(string responseText, string mediaFolderPath, string mediaBaseUrl)
    {
        try
        {
            using var document = JsonDocument.Parse(responseText);
            var root = document.RootElement;

            root.TryGetProperty("retrievables", out var retrievables);

            var items = new List<SearchResultItem>();

            foreach (var retrievable in retrievables.EnumerateArray())
            {
                var item = ParseRetrievable(retrievable, mediaFolderPath, mediaBaseUrl);

                if (item != null)
                    items.Add(item);
            }
            Logger.Info($"Parsed Vitrivr response. Items={items.Count}");
            return SearchResult.FromItems(items);
        }
        catch (Exception exception)
        {
            Logger.Error("Failed to parse Vitrivr response", exception);
            return SearchResult.Failure(exception.Message);
        }
    }

    private static SearchResultItem? ParseRetrievable(JsonElement retrievable, string mediaFolderPath, string mediaBaseUrl)
    {
        if (!retrievable.TryGetProperty("descriptors", out var descriptors))
            return null;

        if (!descriptors.TryGetProperty("file.path", out var pathElement))
            return null;

        var sourcePath = pathElement.GetString();

        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        var fileName = Path.GetFileName(sourcePath);
        var mediaType = DetectMediaType(fileName);
        var mediaFolderName = GetMediaFolderName(mediaType);
        var localPath = Path.Combine(mediaFolderPath, mediaFolderName, fileName);
        var remoteUrl = mediaBaseUrl.TrimEnd('/') + "/" + mediaFolderName + "/" + fileName;

        return new SearchResultItem(mediaType, localPath, remoteUrl);
    }

    private static MediaType DetectMediaType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" or ".jpeg" or ".png" or ".webp" or ".bmp" => MediaType.Image,
            ".mp4" or ".mov" or ".avi" or ".mkv" or ".webm" => MediaType.Video,
            ".glb" or ".gltf" or ".obj" or ".fbx" => MediaType.Object3D,
            _ => MediaType.Unknown
        };
    }

    private static string GetMediaFolderName(MediaType mediaType)
    {
        return mediaType switch
        {
            MediaType.Image => "images",
            MediaType.Video => "videos",
            MediaType.Object3D => "3d",
            _ => throw new InvalidOperationException($"Unsupported media type: {mediaType}")
        };
    }
}