using System.Threading.Tasks;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

/// <summary>
/// Loads files in the background.
/// </summary>
public static class ThreadedResourceLoader
{
	private static readonly EventLogger Log = new(nameof(ThreadedResourceLoader));

	/// <summary>
	/// Starts loading a file and waits until it is ready.
	/// </summary>
	/// <typeparam name="T">The expected resource type.</typeparam>
	/// <param name="path">The resource path to load.</param>
	/// <param name="owner">The node used to wait for the next frame.</param>
	/// <param name="cacheMode">How Godot should cache the file.</param>
	/// <returns>A task containing the loaded resource or <see langword="null"/> when loading fails.</returns>
	public static async Task<T> Load<T>(string path, Node owner, ResourceLoader.CacheMode cacheMode = ResourceLoader.CacheMode.Ignore) where T : Resource
	{
		var requestError = ResourceLoader.LoadThreadedRequest(path, "", false, cacheMode);
		if (requestError != Error.Ok && requestError != Error.Busy)
		{
			Log.Warning($"Threaded resource request failed. Path='{path}', Error={requestError}.");
			return null;
		}

		while (true)
		{
			var status = ResourceLoader.LoadThreadedGetStatus(path);

			if (status == ResourceLoader.ThreadLoadStatus.Loaded)
			{
				var resource = ResourceLoader.LoadThreadedGet(path) as T;
				if (resource == null)
					Log.Warning($"Threaded resource has unexpected type. Path='{path}', Expected='{typeof(T).Name}'.");
				return resource;
			}

			if (status != ResourceLoader.ThreadLoadStatus.InProgress)
			{
				Log.Warning($"Threaded resource loading failed. Path='{path}', Status={status}.");
				return null;
			}

			await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
		}
	}
}
