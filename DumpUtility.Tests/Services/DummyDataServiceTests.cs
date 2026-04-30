using System.Text.Json;
using DumpUtility.Models;
using DumpUtility.Services;
using Xunit;

namespace DumpUtility.Tests.Services;

public class DummyDataServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public DummyDataServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void GetRecords_ValidJson_ReturnsAllRecords()
    {
        var records = new[]
        {
            new DocumentRecord { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = @"1. Docs\1.1 CV", FilePath = @"dummy_data\pdf\rishikesh.pdf" },
            new DocumentRecord { EmployeeId = 456, EmployeeName = "Aamir",     FolderPath = @"1. Docs\1.1 CV", FilePath = @"dummy_data\pdf\aamir.pdf"     }
        };
        string jsonPath = Path.Combine(_tempDir, "records.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(records));

        var service = new DummyDataService(jsonPath);
        var result = service.GetRecords().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(123, result[0].EmployeeId);
        Assert.Equal("Aamir", result[1].EmployeeName);
    }

    [Fact]
    public void GetRecords_MissingFile_ThrowsFileNotFoundException()
    {
        var service = new DummyDataService(Path.Combine(_tempDir, "missing.json"));
        Assert.Throws<FileNotFoundException>(() => service.GetRecords().ToList());
    }

    [Fact]
    public void GetRecords_EmptyArray_ReturnsEmptyList()
    {
        string jsonPath = Path.Combine(_tempDir, "records.json");
        File.WriteAllText(jsonPath, "[]");

        var service = new DummyDataService(jsonPath);
        Assert.Empty(service.GetRecords());
    }
}
