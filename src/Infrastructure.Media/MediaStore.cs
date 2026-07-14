namespace Infrastructure.Media;

internal sealed class MediaStore
{
	private static readonly Dictionary<string, MediaStore> Stores = [];
	private string _currentDirectory;
	private string _nextDirectory;

	private MediaStore(string rootDirectory)
	{
		_currentDirectory = DirectoryPath(rootDirectory, "current");
		_nextDirectory = DirectoryPath(rootDirectory, "next");
	}

	public static MediaStore ForRoot(string rootDirectory)
	{
		rootDirectory = Path.GetFullPath(rootDirectory);
		if (!Stores.TryGetValue(rootDirectory, out var store))
		{
			store = new MediaStore(rootDirectory);
			Stores[rootDirectory] = store;
		}

		return store;
	}

	public void BeginNext()
	{
		if (Directory.Exists(_nextDirectory))
			Directory.Delete(_nextDirectory, true);
		Directory.CreateDirectory(_nextDirectory);
	}

	public string NextPath(string name)
	{
		return Path.Combine(_nextDirectory, Path.GetFileName(name));
	}

	public void CommitNext()
	{
		(_currentDirectory, _nextDirectory) = (_nextDirectory, _currentDirectory);
	}

	public void ReleasePrevious()
	{
		if (Directory.Exists(_nextDirectory))
			Directory.Delete(_nextDirectory, true);
	}

	private static string DirectoryPath(string rootDirectory, string name)
	{
		return Path.Combine(rootDirectory, "remote-media", name);
	}
}
