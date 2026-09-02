using System;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;

/// <summary>
/// Loads image files.
/// </summary>
public partial class ImageResourceLoader : ResourceFormatLoader
{
	private static readonly EventLogger Log = new(nameof(ImageResourceLoader));

	/// <summary>
	/// Gets the file endings supported by this loader.
	/// </summary>
	/// <returns>The supported file endings.</returns>
	public override string[] _GetRecognizedExtensions()
	{
		return ["jpg", "jpeg"];
	}

	/// <summary>
	/// Tells Godot that this loader accepts this resource type.
	/// </summary>
	/// <param name="type">The resource type.</param>
	/// <returns><see langword="true"/> for all requested types.</returns>
	public override bool _HandlesType(StringName type)
	{
		return true;
	}

	/// <summary>
	/// Checks that a path is outside the Godot project.
	/// </summary>
	/// <param name="path">The resource path to inspect.</param>
	/// <param name="type">The resource type.</param>
	/// <returns><see langword="true"/> for an external path and <see langword="false"/> otherwise.</returns>
	public override bool _RecognizePath(string path, StringName type)
	{
		return !path.StartsWith("res://", StringComparison.OrdinalIgnoreCase);
	}

	/// <summary>
	/// Gets the Godot resource type for a path.
	/// </summary>
	/// <param name="path">The resource path.</param>
	/// <returns>The Image resource type name.</returns>
	public override string _GetResourceType(string path)
	{
		return "Image";
	}

	/// <summary>
	/// Loads an image file and creates its smaller texture versions.
	/// </summary>
	/// <param name="path">The image path to load.</param>
	/// <param name="originalPath">The original file path.</param>
	/// <param name="useSubThreads">If Godot can use more loading threads.</param>
	/// <param name="cacheMode">The cache mode.</param>
	/// <returns>The loaded image or a Godot error value.</returns>
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
			if (error == Error.Ok)
				return Variant.CreateFrom(image);
			Log.Warning($"Image mipmap generation failed. Error={error}.");
			return Variant.CreateFrom((long)error);
		}
		catch (Exception exception)
		{
			Log.Error("Image loading failed unexpectedly.", exception);
			return Variant.CreateFrom((long)Error.Failed);
		}
	}
}
