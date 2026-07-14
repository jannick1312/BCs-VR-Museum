using Core;

namespace Application;

public interface IMediaLoader
{
	void BeginBatch();
	void CommitBatch();
	void ReleasePreviousBatch();
	Task<MediaContent> LoadAsync(SearchResultItem item);
}
