using System.Text.Json;
using Core;
using Logger;
using Models;

namespace Infrastructure.Vitrivr;

/// <summary>
/// Converts a Vitrivr response into search results used by the application.
/// </summary>
public static class VitrivrResponseParser
{
	private static readonly EventLogger Log = new(nameof(VitrivrResponseParser));
	private static readonly JsonSerializerOptions SerializerOptions = new() { IncludeFields = true };

	/// <summary>
	/// Reads a Vitrivr response and creates a media search result.
	/// </summary>
	/// <param name="responseText">The response text returned by Vitrivr.</param>
	/// <param name="mediaFolderPath">The local media folder used by the application.</param>
	/// <param name="mediaBaseUrl">The base web address for downloading media.</param>
	/// <returns>The parsed search result.</returns>
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

	/// <summary>
	/// Converts a Vitrivr search item into an application search item.
	/// </summary>
	/// <param name="retrievable">The Vitrivr item to change.</param>
	/// <param name="mediaFolderPath">The local media folder used by the application.</param>
	/// <param name="mediaBaseUrl">The base web address for downloading media.</param>
	/// <returns>The search item, or <see langword="null"/> if its media type is not supported.</returns>
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

	/// <summary>
	/// Changes nanoseconds to whole seconds when the value is valid.
	/// </summary>
	/// <param name="nanoseconds">The timestamp in nanoseconds.</param>
	/// <returns>The time in seconds or <see langword="null"/> if it is missing or outside the valid range.</returns>
	private static int? ToSeconds(long? nanoseconds)
	{
		var seconds = nanoseconds / 1_000_000_000;
		return seconds is >= 0 and <= int.MaxValue ? (int)seconds.Value : null;
	}

	/// <summary>
	/// Gets the file name from a Windows or Unix path.
	/// </summary>
	/// <param name="path">The media file path.</param>
	/// <returns>The file name.</returns>
	private static string ExtractFileName(string path)
	{
		return Path.GetFileName(path.Replace('\\', '/'));
	}

	/// <summary>
	/// Gets the media type from the file ending.
	/// </summary>
	/// <param name="fileName">The media file name.</param>
	/// <returns>The detected media type.</returns>
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

	/// <summary>
	/// Changes 3D model file names to use the Godot package ending and keeps other file names unchanged.
	/// </summary>
	/// <param name="sourceFileName">The original media file name.</param>
	/// <param name="mediaType">The detected media type.</param>
	/// <returns>The file name to use for the media.</returns>
	private static string GetRuntimeFileName(string sourceFileName, MediaType mediaType)
	{
		return mediaType == MediaType.Object3D ? Path.ChangeExtension(sourceFileName, ".pck") : sourceFileName;
	}

	/// <summary>
	/// Gets the folder for a supported media type.
	/// </summary>
	/// <param name="mediaType">The media type to locate.</param>
	/// <returns>The folder name for the media type.</returns>
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
