using System.IO;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Media2D;
using Godot;
using Logger;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Media2D.Image;

/// <summary>
/// Loads images and adds them to wall displays.
/// </summary>
public static class Image2DMediaRenderer
{
	private static readonly EventLogger Log = new(nameof(Image2DMediaRenderer));
	private static ImageResourceLoader _loader;

	/// <summary>
	/// Loads an image and displays it.
	/// </summary>
	/// <param name="instance">The display instance receiving the image.</param>
	/// <param name="path">The image file path.</param>
	/// <returns>A task that completes when the texture is displayed.</returns>
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

	/// <summary>
	/// Adds the image file loader once.
	/// </summary>
	private static void EnsureLoader()
	{
		if (_loader != null)
			return;

		_loader = new ImageResourceLoader();
		ResourceLoader.AddResourceFormatLoader(_loader, true);
	}
}
