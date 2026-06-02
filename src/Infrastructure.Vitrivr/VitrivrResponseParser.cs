using Core;
using System.Text.Json;

namespace Infrastructure.Vitrivr;

public static class VitrivrResponseParser
{
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
            return SearchResult.FromItems(items);
        }
        catch (Exception exception)
        {
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
        var localPath = Path.Combine(mediaFolderPath, fileName);
        var remoteUrl = mediaBaseUrl.TrimEnd('/') + "/" + fileName;
        var mediaType = DetectMediaType(fileName);

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
}