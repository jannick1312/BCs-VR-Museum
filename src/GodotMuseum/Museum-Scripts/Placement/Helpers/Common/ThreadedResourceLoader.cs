using System.Threading.Tasks;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class ThreadedResourceLoader
{
	private static readonly EventLogger Log = new(nameof(ThreadedResourceLoader));

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
