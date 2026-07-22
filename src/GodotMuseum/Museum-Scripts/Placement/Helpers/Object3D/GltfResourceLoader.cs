using System;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

public partial class GltfResourceLoader : ResourceFormatLoader
{
	private static readonly EventLogger Log = new(nameof(GltfResourceLoader));

	public override string[] _GetRecognizedExtensions()
	{
		return ["glb"];
	}

	public override bool _HandlesType(StringName type)
	{
		return true;
	}

	public override string _GetResourceType(string path)
	{
		return "PackedScene";
	}

	public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
	{
		Node scene = null;
		try
		{
			var document = new GltfDocument();
			var state = new GltfState();
			var bytes = FileAccess.GetFileAsBytes(path);
			var error = document.AppendFromBuffer(bytes, "", state);
			if (error != Error.Ok)
			{
				Log.Warning($"GLB loading failed. Error={error}.");
				return Variant.CreateFrom((long)error);
			}

			scene = document.GenerateScene(state);
			if (scene == null)
			{
				Log.Warning("GLB scene generation failed..");
				return Variant.CreateFrom((long)Error.CantCreate);
			}

			var packedScene = new PackedScene();
			error = packedScene.Pack(scene);
			if (error == Error.Ok)
				return Variant.CreateFrom(packedScene);
			Log.Warning($"GLB scene packing failed. Error={error}.");
			return Variant.CreateFrom((long)error);
		}
		catch (Exception exception)
		{
			Log.Error("GLB loading failed unexpectedly.", exception);
			return Variant.CreateFrom((long)Error.Failed);
		}
		finally
		{
			scene?.Free();
		}
	}
}
