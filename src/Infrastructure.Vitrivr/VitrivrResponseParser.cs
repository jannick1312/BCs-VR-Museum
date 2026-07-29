using System.Text.Json;
using Core;
using Logger;
using Models;

namespace Infrastructure.Vitrivr;

public static class VitrivrResponseParser
{
	private static readonly EventLogger Log = new(nameof(VitrivrResponseParser));
	private static readonly JsonSerializerOptions SerializerOptions = new() { IncludeFields = true };

	public static SearchResult Parse(string responseText, string mediaFolderPath, string mediaBaseUrl)
	{
		try
		{
			var response = JsonSerializer.Deserialize<Root>(responseText, SerializerOptions) ?? throw new JsonException("Vitrivr returned an empty JSON response.");
			var items = new List<SearchResultItem>();
			var seenLocalPaths = new List<string>();

			foreach (var retrievable in response.Retrievables)
			{
				var item = MapRetrievable(retrievable, mediaFolderPath, mediaBaseUrl);
				if (item == null)
					continue;

				if (seenLocalPaths.Contains(item.LocalPath))
				{
					Log.Info($"Skipping duplicate media file '{item.Name}'.");
					continue;
				}

				seenLocalPaths.Add(item.LocalPath);
				items.Add(item);
			}

			Log.Info($"Parsed Vitrivr response. Items={items.Count}");
			return SearchResult.FromItems(items);
		}
		catch (Exception exception)
		{
			Log.Error("Failed to parse Vitrivr response", exception);
			return SearchResult.Failure(exception.Message);
		}
	}

	private static SearchResultItem? MapRetrievable(Retrievable retrievable, string mediaFolderPath, string mediaBaseUrl)
	{
		var parent = retrievable.Relationship?.PartOf;
		var sourcePath = retrievable.Descriptors?.FilePath ?? parent?.Descriptors?.FilePath;
		var vector = retrievable.Descriptors?.ClipVector.Where(value => value.HasValue).Select(value => value!.Value).ToArray() ?? [];

		var fileName = ExtractFileName(sourcePath!);
		var mediaType = DetectMediaType(fileName);

		if (mediaType == MediaType.Unknown)
		{
			Log.Info($"Skipping unsupported media file '{fileName}'. Supported source formats are .jpg, .ogv, .glb and .pck.");
			return null;
		}

		fileName = GetRuntimeFileName(fileName, mediaType);
		var mediaFolderName = GetMediaFolderName(mediaType);
		var localPath = Path.Combine(mediaFolderPath, mediaFolderName, fileName);
		var remoteUrl = mediaBaseUrl.TrimEnd('/') + "/" + mediaFolderName + "/" + Uri.EscapeDataString(fileName);
		var startTimeSeconds = ToSeconds(retrievable.Descriptors?.TimeStart);
		var metadata = new MediaMetadata(parent?.Id ?? retrievable.Id);

		return new SearchResultItem(vector, mediaType, localPath, remoteUrl, startTimeSeconds, metadata);
	}

	private static int? ToSeconds(long? nanoseconds)
	{
		var seconds = nanoseconds / 1_000_000_000;
		return seconds is >= 0 and <= int.MaxValue ? (int)seconds.Value : null;
	}

	private static string ExtractFileName(string path)
	{
		return Path.GetFileName(path.Replace('\\', '/'));
	}

	private static MediaType DetectMediaType(string fileName)
	{
		return Path.GetExtension(fileName).ToLowerInvariant() switch
		{
			".jpg" => MediaType.Image,
			".ogv" => MediaType.Video,
			".glb" or ".pck" => MediaType.Object3D,
			_ => MediaType.Unknown
		};
	}

	private static string GetRuntimeFileName(string sourceFileName, MediaType mediaType)
	{
		return mediaType == MediaType.Object3D ? Path.ChangeExtension(sourceFileName, ".pck") : sourceFileName;
	}

	private static string GetMediaFolderName(MediaType mediaType)
	{
		return mediaType switch
		{
			MediaType.Image => "images",
			MediaType.Video => "videos",
			MediaType.Object3D => "3dPck",
			_ => throw new InvalidOperationException($"Unsupported media type: {mediaType}")
		};
	}
}
