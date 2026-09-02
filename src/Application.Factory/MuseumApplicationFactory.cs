using Application.Abstractions;
using Infrastructure.Media;
using Infrastructure.Vitrivr;

namespace Application.Factory;

/// <summary>
/// Builds museum applications with their required services.
/// </summary>
public static class MuseumApplicationFactory
{
	/// <summary>
	/// Builds a museum application connected to a Vitrivr server.
	/// </summary>
	/// <param name="currentIp">The network address of the Vitrivr server.</param>
	/// <param name="mediaFolderPath">The local media folder used by the application.</param>
	/// <param name="mediaRoot">The root folder for downloaded media.</param>
	/// <returns>The new museum application.</returns>
	public static IMuseumApplication CreateVitrivrApplication(string currentIp, string mediaFolderPath, string mediaRoot)
	{
		var vitrivrSettings = new VitrivrSettings(currentIp, mediaFolderPath);

		return new MuseumApplication(new VitrivrSearchService(vitrivrSettings), new MediaLoader(mediaRoot), new VitrivrServerHealthService(vitrivrSettings));
	}
}
