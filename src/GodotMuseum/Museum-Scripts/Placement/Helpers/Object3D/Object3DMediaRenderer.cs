using System.IO;
using System.Threading.Tasks;
using BCSVRMuseum.Museum_Scripts.Placement.Helpers.Common;
using BCSVRMuseum.Museum_Scripts.Placement.Object3D;
using Godot;

namespace BCSVRMuseum.Museum_Scripts.Placement.Helpers.Object3D;

public static class Object3DMediaRenderer
{
	private static GltfResourceLoader _loader;

	public static async Task<float?> Render(Object3DDisplayInstance instance, string path, Node3D place, Object3DDisplayFitter fitter)
	{
		EnsureLoader();
		var resourcePath = ProjectSettings.LocalizePath(Path.GetFullPath(path).Replace('\\', '/'));
		var packedScene = await ThreadedResourceLoader.Load<PackedScene>(resourcePath, instance.Item);
		var objectNode = packedScene?.Instantiate() as Node3D;
		return objectNode == null ? null : fitter.Place(instance.Item, objectNode, place);
	}

	private static void EnsureLoader()
	{
		if (_loader != null)
			return;

		_loader = new GltfResourceLoader();
		ResourceLoader.AddResourceFormatLoader(_loader, true);
	}
}
