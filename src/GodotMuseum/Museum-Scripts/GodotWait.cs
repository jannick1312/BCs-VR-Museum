using System;
using System.Threading.Tasks;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts;

/// <summary>
/// Waits for Godot objects that are not ready yet.
/// </summary>
public static class GodotWait
{
	private static readonly EventLogger Log = new(nameof(GodotWait));

	/// <summary>
	/// Checks for a value once per frame until it is ready.
	/// </summary>
	/// <typeparam name="T">The reference type being awaited.</typeparam>
	/// <param name="node">The node used to wait for the next frame.</param>
	/// <param name="getValue">The function used to get the value.</param>
	/// <param name="description">A description included in timeout messages.</param>
	/// <param name="maxFrames">The maximum number of frames to wait.</param>
	/// <returns>A task containing the first non-null value returned by <paramref name="getValue"/>.</returns>
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
