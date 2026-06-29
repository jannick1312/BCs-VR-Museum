using Core;
using Logger;
using Models;
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
            var seenLocalPaths = new List<string>();

            foreach (var retrievable in retrievables.EnumerateArray())
            {
                var item = ParseRetrievable(retrievable, mediaFolderPath, mediaBaseUrl);

                if (item == null)
                    continue;

                if (seenLocalPaths.Contains(item.LocalPath))
                {
                    Logger.Info($"Skipping duplicate media file '{item.Name}'.");
                    continue;
                }

                seenLocalPaths.Add(item.LocalPath);
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
        var sourcePath = GetSourcePath(retrievable);

        if (string.IsNullOrWhiteSpace(sourcePath))
            return null;

        var fileName = ExtractFileName(sourcePath);
        var mediaType = DetectMediaType(fileName);

        if (mediaType == MediaType.Unknown)
        {
            Logger.Info($"Skipping unsupported media file '{fileName}'. Supported formats are .jpg, .ogv and .glb.");
            return null;
        }

        var mediaFolderName = GetMediaFolderName(mediaType);
        var localPath = Path.Combine(mediaFolderPath, mediaFolderName, fileName);
        var remoteUrl = mediaBaseUrl.TrimEnd('/') + "/" + mediaFolderName + "/" + fileName;

        return new SearchResultItem(mediaType, localPath, remoteUrl);
    }

    private static string? GetSourcePath(JsonElement retrievable)
    {
        if (retrievable.TryGetProperty("descriptors", out var descriptors) &&
            descriptors.TryGetProperty("file.path", out var pathElement))
            return pathElement.GetString();

        if (retrievable.TryGetProperty("relationship", out var relationship) &&
            relationship.TryGetProperty("partOf", out var parentRetrievable) &&
            parentRetrievable.TryGetProperty("descriptors", out var parentDescriptors) &&
            parentDescriptors.TryGetProperty("file.path", out var parentPathElement))
            return parentPathElement.GetString();

        return null;
    }

    private static string ExtractFileName(string path)
    {
        var normalizedPath = path.Replace('\\', '/');
        return Path.GetFileName(normalizedPath);
    }

    private static MediaType DetectMediaType(string fileName)
    {
        var extension = Path.GetExtension(fileName).ToLowerInvariant();

        return extension switch
        {
            ".jpg" => MediaType.Image,
            ".ogv" => MediaType.Video,
            ".glb" => MediaType.Object3D,
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