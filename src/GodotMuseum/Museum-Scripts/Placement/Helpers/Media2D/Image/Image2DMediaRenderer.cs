using System.IO;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;

public static class Image2DMediaRenderer
{
	public static void Render(Media2DDisplayInstance instance, byte[] bytes, string path)
	{
		var texture = File.Exists(path) ? LoadTexture(path) : LoadTexture(bytes);
		instance.ShowTexture(texture);
	}

	private static ImageTexture LoadTexture(string path)
	{
		var image = new Godot.Image();
		image.Load(path);
		image.GenerateMipmaps();
		return ImageTexture.CreateFromImage(image);
	}

	private static ImageTexture LoadTexture(byte[] bytes)
	{
		var image = new Godot.Image();
		image.LoadJpgFromBuffer(bytes);
		image.GenerateMipmaps();
		return ImageTexture.CreateFromImage(image);
	}
}
