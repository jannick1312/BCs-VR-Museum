namespace Infrastructure.Configuration;

public class AppSettings
{
    public bool Deployed { get; init; } = true;
    public string DefaultDeployedIp { get; init; } = "192.168.1.21";
    public string DefaultStreamedIp { get; init; } = "10.34.64.208";
    public string MediaFolderPath { get; init; } = @"C:\Users\dbis-\Desktop\BCs\media";
    public string LogDirectoryPath { get; init; } = @"C:\Users\dbis-\Desktop\logs\";
    public string FallbackLogDirectoryPath { get; init; } = "/sdcard/Android/data/VR.Museum/files/logs";
}
