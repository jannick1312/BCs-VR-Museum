using Application;
using Core;
using Logger;

namespace Infrastructure.Media;

public class MediaLoader : IMediaLoader
{
	private readonly HttpClient _httpClient = new() { Timeout = TimeSpan.FromSeconds(10) };
	private readonly EventLogger _logger = new(nameof(MediaLoader));

	public async Task<MediaContent> LoadAsync(SearchResultItem item)
	{
		try
		{
			_logger.Info($"Media loading started. Name='{item.Name}', LocalPath='{item.LocalPath}', RemoteUrl='{item.RemoteUrl}'.");

			if (File.Exists(item.LocalPath))
			{
				_logger.Info($"Media loaded from local path. LocalPath='{item.LocalPath}'.");
				return MediaContent.FromPath(item.LocalPath);
			}

			_logger.Info("Local media not found, remote loading started.");
			var remoteBytes = await _httpClient.GetByteArrayAsync(item.RemoteUrl);
			_logger.Info($"Media loaded from remote URL. RemoteUrl='{item.RemoteUrl}'.");
			return MediaContent.FromBytes(remoteBytes);
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
