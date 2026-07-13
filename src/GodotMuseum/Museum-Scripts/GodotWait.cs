using System;
using System.Threading.Tasks;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts;

public static class GodotWait
{
	private static readonly EventLogger Log = new(nameof(GodotWait));

	public static async Task<T> WaitFor<T>(this Node node, Func<T> getValue, string description, int maxFrames = 120) where T : class
	{
		for (var i = 0; i <= maxFrames; i++)
		{
			var value = getValue();
			if (value != null)
				return value;

			await node.ToSignal(node.GetTree(), SceneTree.SignalName.ProcessFrame);
		}

		Log.Error($"Godot node wait timed out. Description='{description}', MaxFrames={maxFrames}.");
		throw new TimeoutException($"Timed out waiting for {description}.");
	}
}
