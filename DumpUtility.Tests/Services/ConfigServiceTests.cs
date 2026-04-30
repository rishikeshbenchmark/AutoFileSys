using DumpUtility.Services;
using Xunit;

namespace DumpUtility.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ConfigServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void Load_ValidConfig_ReturnsParsedValues()
    {
        string configPath = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(configPath, """
            {
              "BaseFolder": "C:\\Output",
              "Database": { "Server": "SRV", "DatabaseName": "DB", "Username": "sa", "Password": "pw", "Port": 1433 }
            }
            """);

        var service = new ConfigService();
        var config = service.Load(configPath);

        Assert.Equal("C:\\Output", config.BaseFolder);
        Assert.Equal("SRV", config.Database.Server);
        Assert.Equal(1433, config.Database.Port);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var service = new ConfigService();
        Assert.Throws<FileNotFoundException>(() => service.Load(Path.Combine(_tempDir, "missing.json")));
    }

    [Fact]
    public void Load_EmptyBaseFolder_ThrowsInvalidOperationException()
    {
        string configPath = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(configPath, """{ "BaseFolder": "", "Database": {} }""");

        var service = new ConfigService();
        Assert.Throws<InvalidOperationException>(() => service.Load(configPath));
    }
}
