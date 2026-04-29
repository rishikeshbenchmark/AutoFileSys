namespace DumpUtility.Models;

public class AppConfig
{
    public string BaseFolder { get; set; } = string.Empty;
    public DatabaseConfig Database { get; set; } = new();
}
