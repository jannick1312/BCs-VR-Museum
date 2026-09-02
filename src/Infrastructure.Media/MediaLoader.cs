using Application;
using Core;
using Logger;

namespace Infrastructure.Media;

/// <summary>
/// Loads local media or downloads missing media.
/// </summary>
/// <param name="mediaRoot">The root folder for downloaded media.</param>
public class MediaLoader(string mediaRoot) : IMediaLoader
{
	private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
	private readonly EventLogger _logger = new(nameof(MediaLoader));
	private readonly MediaStore _mediaStore = MediaStore.ForRoot(mediaRoot);

	/// <summary>
	/// Prepares the folder for new media downloads.
	/// </summary>
	public void BeginBatch()
	{
		_mediaStore.BeginNext();
	}

	/// <summary>
	/// Replaces the current files and keeps the previous files.
	/// </summary>
	public void CommitBatch()
	{
		_mediaStore.CommitNext();
	}

	/// <summary>
	/// Removes the downloaded files from the previous search.
	/// </summary>
	public void ReleasePreviousBatch()
	{
		_mediaStore.ReleasePrevious();
	}

	/// <summary>
	/// Loads a search result from local storage or its web address.
	/// </summary>
	/// <param name="item">The search result describing the media to load.</param>
	/// <returns>A task containing the result of loading the media.</returns>
	public async Task<MediaContent> LoadAsync(SearchResultItem item)
	{
		try
		{
			_logger.Info($"Media loading started. Name='{item.Name}', MediaType={item.MediaType}, LocalPath='{item.LocalPath}', RemoteUrl='{item.RemoteUrl}'.");

			if (File.Exists(item.LocalPath))
			{
				_logger.Info($"Media loaded from local path. LocalPath='{item.LocalPath}'.");
				return MediaContent.FromPath(item.LocalPath);
			}

			_logger.Info("Local media not found, remote loading started.");
			var remotePath = _mediaStore.NextPath(item.Name);
			await using var remoteStream = await HttpClient.GetStreamAsync(item.RemoteUrl);
			await using var fileStream = new FileStream(remotePath, FileMode.Create, FileAccess.Write, FileShare.None, 81920, true);
			await remoteStream.CopyToAsync(fileStream);
			_logger.Info($"Media loaded from remote URL. RemoteUrl='{item.RemoteUrl}'.");
			return MediaContent.FromPath(remotePath);
		}
		catch (TaskCanceledException)
		{
			_logger.Warning($"Media loading timed out. Name='{item.Name}'");
			return MediaContent.Failure("Media request timed out.");
		}
		catch (Exception exception)
		{
			_logger.Error("Media loading failed", exception);
			return MediaContent.Failure(exception.Message);
		}
	}
}
