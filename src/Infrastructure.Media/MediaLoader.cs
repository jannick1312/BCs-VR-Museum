using Application;
using Core;
using Logger;

namespace Infrastructure.Media;

public class MediaLoader(string mediaRoot) : IMediaLoader
{
	private static readonly HttpClient HttpClient = new() { Timeout = TimeSpan.FromSeconds(30) };
	private readonly EventLogger _logger = new(nameof(MediaLoader));
	private readonly MediaStore _mediaStore = MediaStore.ForRoot(mediaRoot);

	public void BeginBatch()
	{
		_mediaStore.BeginNext();
	}

	public void CommitBatch()
	{
		_mediaStore.CommitNext();
	}

	public void ReleasePreviousBatch()
	{
		_mediaStore.ReleasePrevious();
	}

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
