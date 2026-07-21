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

public static class Object3DMediaRenderer
{
	private static readonly EventLogger Log = new(nameof(Object3DMediaRenderer));

	private static readonly HashSet<string> MountedPacks = new(StringComparer.Ordinal);
	private static readonly Lock MountLock = new();
	private static readonly Lock LoaderLock = new();
	private static GltfResourceLoader _gltfLoader;

	public static async Task<float?> Render(Object3DDisplayInstance instance, string path, Node3D place, Object3DDisplayFitter fitter)
	{
		var mediaPath = Path.GetFullPath(path);
		var extension = Path.GetExtension(mediaPath);
		PackedScene packedScene;
		if (string.Equals(extension, ".pck", StringComparison.OrdinalIgnoreCase))
		{
			packedScene = await LoadFromPack(mediaPath, instance.Item);
		}
		else if (string.Equals(extension, ".glb", StringComparison.OrdinalIgnoreCase))
		{
			packedScene = await LoadFromGltf(mediaPath, instance.Item);
		}
		else
		{
			Log.Warning($"Unsupported 3D object path. Path='{mediaPath}'.");
			return null;
		}

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

	private static async Task<PackedScene> LoadFromPack(string packPath, Node owner)
	{
		if (!File.Exists(packPath))
		{
			Log.Warning($"3D object PCK does not exist. Path='{packPath}'.");
			return null;
		}

		if (!EnsurePackMounted(packPath))
			return null;

		var resourcePath = $"res://native/{Path.GetFileNameWithoutExtension(packPath)}.scn";
		return await ThreadedResourceLoader.Load<PackedScene>(
			resourcePath,
			owner,
			ResourceLoader.CacheMode.Reuse);
	}

	private static async Task<PackedScene> LoadFromGltf(string gltfPath, Node owner)
	{
		if (!File.Exists(gltfPath))
		{
			Log.Warning($"3D object GLB does not exist. Path='{gltfPath}'.");
			return null;
		}

		EnsureGltfLoader();
		var resourcePath = ProjectSettings.LocalizePath(gltfPath.Replace('\\', '/'));
		return await ThreadedResourceLoader.Load<PackedScene>(
			resourcePath,
			owner);
	}

	private static void EnsureGltfLoader()
	{
		lock (LoaderLock)
		{
			if (_gltfLoader != null)
				return;

			_gltfLoader = new GltfResourceLoader();
			ResourceLoader.AddResourceFormatLoader(_gltfLoader, true);
		}
	}

	private static bool EnsurePackMounted(string packPath)
	{
		lock (MountLock)
		{
			if (MountedPacks.Contains(packPath))
				return true;

			if (!ProjectSettings.LoadResourcePack(packPath, false))
			{
				Log.Warning($"3D object PCK could not be mounted. Path='{packPath}'.");
				return false;
			}

			MountedPacks.Add(packPath);
			Log.Info($"Mounted 3D object PCK. Path='{packPath}'.");
			return true;
		}
	}
}
