using Application.Abstractions;
using Infrastructure.Media;
using Infrastructure.Vitrivr;

namespace Application.Factory;

public static class MuseumApplicationFactory
{
    public static IMuseumApplication CreateVitrivrApplication(string currentIp, string mediaFolderPath)
    {
        var vitrivrSettings = new VitrivrSettings(currentIp, mediaFolderPath);

        return new MuseumApplication(
            new VitrivrSearchService(vitrivrSettings),
            new MediaLoader(),
            new VitrivrServerHealthService(vitrivrSettings));
    }
}