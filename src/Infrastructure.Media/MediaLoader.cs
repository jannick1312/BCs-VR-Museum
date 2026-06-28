using Application;
using Core;
using Infrastructure.Logging;

namespace Infrastructure.Media;

public class MediaLoader : IMediaLoader
{
    private readonly HttpClient _httpClient = new() {Timeout = TimeSpan.FromSeconds(10)};
    private readonly EventLogger _logger = new(nameof(MediaLoader));

    public async Task<MediaContent> LoadAsync(SearchResultItem item)
    {
        try
        {
            _logger.Info($"Loading media. Name='{item.Name}', LocalPath='{item.LocalPath}', RemoteUrl='{item.RemoteUrl}'");

            if (File.Exists(item.LocalPath))
            {
                _logger.Info($"Loaded media from local path. LocalPath='{item.LocalPath}'");
                return MediaContent.FromPath(item.LocalPath);
            }

            _logger.Info("Local media not found, loading remote media.");
            var remoteBytes = await _httpClient.GetByteArrayAsync(item.RemoteUrl);
            _logger.Info($"Loaded media from remote URL. RemoteUrl='{item.RemoteUrl}'");
            return MediaContent.FromBytes(remoteBytes);
        }
        catch (TaskCanceledException)
        {
            _logger.Warning("Media request timed out.");
            return MediaContent.Failure("Media request timed out.");
        }
        catch (Exception exception)
        {
            _logger.Error("Media loading failed", exception);
            return MediaContent.Failure(exception.Message);
        }
    }
}