namespace Infrastructure.Configuration;

public class AppSettings
{
    public bool Deployed { get; set; } = true;
    public string DefaultDeployedIp { get; set; } = "192.168.1.21";
    public string DefaultStreamedIp { get; set; } = "10.34.64.208";
    public string MediaFolderPath { get; set; } = @"C:\Users\dbis-\Desktop\BCs\media";
}