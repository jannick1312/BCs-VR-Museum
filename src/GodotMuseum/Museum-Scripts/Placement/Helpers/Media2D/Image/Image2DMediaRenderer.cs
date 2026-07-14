using System.IO;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;

public static class Image2DMediaRenderer
{
	private static readonly EventLogger Log = new(nameof(Image2DMediaRenderer));
	private static ImageResourceLoader _loader;

	public static async Task Render(Media2DDisplayInstance instance, string path)
	{
		EnsureLoader();
		var resourcePath = ProjectSettings.LocalizePath(Path.GetFullPath(path).Replace('\\', '/'));
		var image = await ThreadedResourceLoader.Load<Godot.Image>(resourcePath, instance.Item);

		if (image == null)
		{
			Log.Info("Using black fallback texture.");
			image = Godot.Image.CreateEmpty(1, 1, false, Godot.Image.Format.Rgba8);
			image.Fill(Colors.Black);
		}

		instance.ShowTexture(ImageTexture.CreateFromImage(image));
	}

	private static void EnsureLoader()
	{
		if (_loader != null)
			return;

		_loader = new ImageResourceLoader();
		ResourceLoader.AddResourceFormatLoader(_loader, true);
	}
}
