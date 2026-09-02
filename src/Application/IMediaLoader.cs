using Core;

namespace Application;

/// <summary>
/// Loads media and manages downloaded files.
/// </summary>
public interface IMediaLoader
{
	/// <summary>
	/// Prepares the folder for new media downloads.
	/// </summary>
	void BeginBatch();

	/// <summary>
	/// Replaces the current files and keeps the previous files.
	/// </summary>
	void CommitBatch();

	/// <summary>
	/// Removes the downloaded files from the previous search.
	/// </summary>
	void ReleasePreviousBatch();

	/// <summary>
	/// Loads a search result from local storage or its web address.
	/// </summary>
	/// <param name="item">The search result describing the media to load.</param>
	/// <returns>A task containing the result of loading the media.</returns>
	Task<MediaContent> LoadAsync(SearchResultItem item);
}
