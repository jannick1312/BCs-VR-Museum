using System;
using System.Threading;
using System.Threading.Tasks;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;

public static class ThreadedResourceLoader
{
	private static SemaphoreSlim _workers = new(1, 1);

	public static void ConfigureWorkers(int count)
	{
		count = Math.Max(1, count);
		_workers = new SemaphoreSlim(count, count);
	}

	public static async Task<T> Load<T>(string path, Node owner) where T : Resource
	{
		var workers = _workers;
		await workers.WaitAsync();
		try
		{
			ResourceLoader.LoadThreadedRequest(path, "", false, ResourceLoader.CacheMode.Ignore);
			while (true)
			{
				var status = ResourceLoader.LoadThreadedGetStatus(path);

				if (status == ResourceLoader.ThreadLoadStatus.Loaded)
					return ResourceLoader.LoadThreadedGet(path) as T;

				if (status != ResourceLoader.ThreadLoadStatus.InProgress)
					return null;

				await owner.ToSignal(owner.GetTree(), SceneTree.SignalName.ProcessFrame);
			}
		}
		finally
		{
			workers.Release();
		}
	}
}
