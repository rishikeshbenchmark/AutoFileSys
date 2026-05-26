using System.Text.Json;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class DummyDataService : IDataService
{
    private readonly string _recordsPath;

    public DummyDataService(string recordsPath) => _recordsPath = recordsPath;

    public IEnumerable<DocumentRecord> GetRecords()
    {
        if (!File.Exists(_recordsPath))
            throw new FileNotFoundException($"records.json not found at: {_recordsPath}");

        string json = File.ReadAllText(_recordsPath);
        return JsonSerializer.Deserialize<List<DocumentRecord>>(json,
            new JsonSerializerOptions
            {
                PropertyNameCaseInsensitive = true,
                ReadCommentHandling = JsonCommentHandling.Skip,
                AllowTrailingCommas = true
            }) ?? [];
    }
}
