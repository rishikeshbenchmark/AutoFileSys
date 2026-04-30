using DumpUtility.Services;
using Xunit;

namespace DumpUtility.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ReportServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FolderResult BuildResult(int folders, int files, string empFolder, string empPath,
        List<(string, string)>? skipped = null)
    {
        var r = new FolderResult { FoldersCreated = folders, FilesWritten = files };
        r.EmployeePaths[empFolder] = empPath;
        skipped?.ForEach(s => r.SkippedFiles.Add(s));
        return r;
    }

    [Fact]
    public void WriteSummary_CreatesFile()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var service = new ReportService();
        service.WriteSummary(summaryPath, DateTime.Now, BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice"));
        Assert.True(File.Exists(summaryPath));
    }

    [Fact]
    public void WriteSummary_ContainsEmployeeCount()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var result = BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice");

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("Employees       : 1", content);
        Assert.Contains("Folders Created : 10", content);
        Assert.Contains("Files Written   : 5", content);
        Assert.Contains("Files Skipped   : 0", content);
    }

    [Fact]
    public void WriteSummary_ListsEmployeePaths()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var result = BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice");

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("0123-Alice", content);
        Assert.Contains(@"C:\Out\0123-Alice", content);
    }

    [Fact]
    public void WriteSummary_ShowsSkippedFiles()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var skipped = new List<(string, string)> { ("missing.pdf", "File not found") };
        var result = BuildResult(5, 3, "0123-Alice", @"C:\Out\0123-Alice", skipped);

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("missing.pdf", content);
        Assert.Contains("File not found", content);
    }

    [Fact]
    public void WriteSummary_NoSkippedFiles_ShowsNone()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        new ReportService().WriteSummary(summaryPath, DateTime.Now,
            BuildResult(5, 3, "0123-Alice", @"C:\Out\0123-Alice"));

        string content = File.ReadAllText(summaryPath);
        Assert.Contains("(none)", content);
    }
}
