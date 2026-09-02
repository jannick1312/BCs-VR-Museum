namespace Infrastructure.Media;

/// <summary>
/// Manages folders for downloaded media.
/// </summary>
internal sealed class MediaStore
{
	private static readonly Dictionary<string, MediaStore> Stores = [];
	private string _currentDirectory;
	private string _nextDirectory;

	/// <summary>
	/// Creates a media store for a root folder.
	/// </summary>
	/// <param name="rootDirectory">The root folder for downloaded media.</param>
	private MediaStore(string rootDirectory)
	{
		_currentDirectory = DirectoryPath(rootDirectory, "current");
		_nextDirectory = DirectoryPath(rootDirectory, "next");
	}

	/// <summary>
	/// Gets the shared media store for a root folder.
	/// </summary>
	/// <param name="rootDirectory">The root folder for downloaded media.</param>
	/// <returns>The media store for the root folder.</returns>
	public static MediaStore ForRoot(string rootDirectory)
	{
		rootDirectory = Path.GetFullPath(rootDirectory);
		if (Stores.TryGetValue(rootDirectory, out var store))
			return store;
		store = new MediaStore(rootDirectory);
		Stores[rootDirectory] = store;

		return store;
	}

	/// <summary>
	/// Prepares the folder for new media downloads.
	/// </summary>
	public void BeginNext()
	{
		if (Directory.Exists(_nextDirectory))
			Directory.Delete(_nextDirectory, true);
		Directory.CreateDirectory(_nextDirectory);
	}

	/// <summary>
	/// Creates a file path for a new download.
	/// </summary>
	/// <param name="name">The source file name.</param>
	/// <returns>The path for the file.</returns>
	public string NextPath(string name)
	{
		return Path.Combine(_nextDirectory, Path.GetFileName(name));
	}

	/// <summary>
	/// Makes the new files current and keeps the previous files.
	/// </summary>
	public void CommitNext()
	{
		(_currentDirectory, _nextDirectory) = (_nextDirectory, _currentDirectory);
	}

	/// <summary>
	/// Deletes the downloaded files from the previous search.
	/// </summary>
	public void ReleasePrevious()
	{
		if (Directory.Exists(_nextDirectory))
			Directory.Delete(_nextDirectory, true);
	}

	/// <summary>
	/// Builds the path for a downloaded media folder.
	/// </summary>
	/// <param name="rootDirectory">The root folder for downloaded media.</param>
	/// <param name="name">The media folder name.</param>
	/// <returns>The full media folder path.</returns>
	private static string DirectoryPath(string rootDirectory, string name)
	{
		return Path.Combine(rootDirectory, "remote-media", name);
	}
}
