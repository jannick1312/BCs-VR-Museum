using Core;

namespace Application;

public interface IMediaLoader
{
	Task<MediaContent> LoadAsync(SearchResultItem item);
}
