using System;
using System.Collections.Generic;
using System.IO;
using System.Threading;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

/// <summary>
/// Loads packed 3D model scenes and adds them to museum displays.
/// </summary>
public static class Object3DMediaRenderer
{
	private static readonly EventLogger Log = new(nameof(Object3DMediaRenderer));

	private static readonly HashSet<string> MountedPacks = new(StringComparer.Ordinal);
	private static readonly SemaphoreSlim PackLoadSlot = new(1, 1);

	/// <summary>
	/// Loads a packed scene and fits it to a display.
	/// </summary>
	/// <param name="instance">The display instance receiving the 3D model.</param>
	/// <param name="path">The path to the Godot package file.</param>
	/// <param name="place">The museum placement area.</param>
	/// <param name="fitter">The fitter used to position the display and 3D model.</param>
	/// <returns>A task containing the applied 3D model scale or <see langword="null"/> when loading fails.</returns>
	public static async Task<float?> Render(Object3DDisplayInstance instance, string path, Node3D place, Object3DDisplayFitter fitter)
	{
		var mediaPath = Path.GetFullPath(path);
		var extension = Path.GetExtension(mediaPath);
		if (!string.Equals(extension, ".pck", StringComparison.OrdinalIgnoreCase))
		{
			Log.Warning($"Unsupported 3D object path. Path='{mediaPath}'.");
			return null;
		}

		var packedScene = await LoadFromPack(mediaPath, instance.Item);
		if (packedScene?.Instantiate() is not Node3D objectNode)
		{
			Log.Warning($"3D scene could not be instantiated. Path='{mediaPath}'.");
			return null;
		}

		var geometryBounds = Object3DDisplayFitter.Bounds(objectNode);
		var originalBounds = Object3DDisplayFitter.TransformBounds(objectNode.Transform, geometryBounds);
		instance.AttachObject(objectNode, originalBounds);

		var scale = fitter.Place(instance.Item, objectNode, place, geometryBounds);
		instance.StoreDisplayPlacement();
		return scale;
	}

	/// <summary>
	/// Opens a resource pack once and loads its scene.
	/// </summary>
	/// <param name="packPath">The full path to the Godot package file.</param>
	/// <param name="owner">The node used while the scene loads.</param>
	/// <returns>A task containing the loaded scene or <see langword="null"/> if loading fails.</returns>
	private static async Task<PackedScene> LoadFromPack(string packPath, Node owner)
	{
		await PackLoadSlot.WaitAsync();
		try
		{
			if (MountedPacks.Add(packPath))
			{
				if (!ProjectSettings.LoadResourcePack(packPath))
				{
					MountedPacks.Remove(packPath);
					Log.Warning($"3D object PCK could not be mounted. Path='{packPath}'.");
					return null;
				}

				Log.Info($"Mounted 3D object PCK. Path='{packPath}'.");
			}

			var resourcePath = $"res://native/{Path.GetFileNameWithoutExtension(packPath)}.scn";
			return await ThreadedResourceLoader.Load<PackedScene>(resourcePath, owner);
		}
		finally
		{
			PackLoadSlot.Release();
		}
	}
}
