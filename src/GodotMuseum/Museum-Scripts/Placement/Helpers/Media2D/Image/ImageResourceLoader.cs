using System;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;

public partial class ImageResourceLoader : ResourceFormatLoader
{
	private static readonly EventLogger Log = new(nameof(ImageResourceLoader));

	public override string[] _GetRecognizedExtensions()
	{
		return ["jpg", "jpeg"];
	}

	public override bool _HandlesType(StringName type)
	{
		return true;
	}

	public override string _GetResourceType(string path)
	{
		return "Image";
	}

	public override Variant _Load(string path, string originalPath, bool useSubThreads, int cacheMode)
	{
		try
		{
			var image = new Godot.Image();
			var error = image.Load(path);
			if (error != Error.Ok)
			{
				Log.Warning($"Image loading failed. Error={error}.");
				return Variant.CreateFrom((long)error);
			}

			error = image.GenerateMipmaps();
			if (error != Error.Ok)
			{
				Log.Warning($"Image mipmap generation failed. Error={error}.");
				return Variant.CreateFrom((long)error);
			}

			return Variant.CreateFrom(image);
		}
		catch (Exception exception)
		{
			Log.Error("Image loading failed unexpectedly.", exception);
			return Variant.CreateFrom((long)Error.Failed);
		}
	}
}
