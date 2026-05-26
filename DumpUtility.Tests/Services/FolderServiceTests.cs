using DumpUtility.Helpers;
using DumpUtility.Models;
using DumpUtility.Services;
using Xunit;

namespace DumpUtility.Tests.Services;

public class FolderServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LogHelper _log;
    private readonly string _runFolder;
    private readonly FolderService _service;

    public FolderServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _log       = new LogHelper(Path.Combine(_tempDir, "test.log"));
        _runFolder = Path.Combine(_tempDir, "Dump_Utility_Test");
        _service   = new FolderService(_log, new PdfMergeService(_log));
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string CreateSourceFile(string relativePath)
    {
        string full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "dummy");
        return full;
    }

    [Fact]
    public void Execute_CreatesRunFolder()
    {
        _service.Execute(_runFolder, [], _tempDir);
        Assert.True(Directory.Exists(_runFolder));
    }

    [Fact]
    public void Execute_CreatesEmployeeFolderWithPaddedId()
    {
        CreateSourceFile("dummy_data/doc/test.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 7, EmployeeName = "Alice", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/doc/test.docx" }
        };

        _service.Execute(_runFolder, records, _tempDir);

        Assert.True(Directory.Exists(Path.Combine(_runFolder, "0007-Alice")));
    }

    [Fact]
    public void Execute_CopiesNonEligibleFileWithSequencedName()
    {
        CreateSourceFile("dummy_data/doc/test.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/doc/test.docx" }
        };

        _service.Execute(_runFolder, records, _tempDir);

        string expected = Path.Combine(_runFolder, "0123-Rishikesh", "1. Docs", "1.1 CV Signed", "CV Signed_0123-Rishikesh_1.docx");
        Assert.True(File.Exists(expected), $"Expected file not found: {expected}");
    }

    [Fact]
    public void Execute_MultipleNonEligibleFilesInSameFolder_SequenceIncrements()
    {
        CreateSourceFile("dummy_data/doc/a.docx");
        CreateSourceFile("dummy_data/doc/b.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/doc/a.docx" },
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/doc/b.docx" }
        };

        var result = _service.Execute(_runFolder, records, _tempDir);

        string folder = Path.Combine(_runFolder, "0123-Rishikesh", "1. Docs", "1.1 CV Signed");
        Assert.True(File.Exists(Path.Combine(folder, "CV Signed_0123-Rishikesh_1.docx")));
        Assert.True(File.Exists(Path.Combine(folder, "CV Signed_0123-Rishikesh_2.docx")));
        Assert.Equal(2, result.FilesWritten);
    }

    [Fact]
    public void Execute_ThreeLevelDeepPath_CreatesCorrectFolders()
    {
        CreateSourceFile("dummy_data/doc/test.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh",
                    FolderPath = "9. Performance Appraisal/9.2 Current Year/9.2.1 Mid-Year",
                    FilePath   = "dummy_data/doc/test.docx" }
        };

        _service.Execute(_runFolder, records, _tempDir);

        string expected = Path.Combine(_runFolder, "0123-Rishikesh",
            "9. Performance Appraisal", "9.2 Current Year", "9.2.1 Mid-Year",
            "Mid-Year_0123-Rishikesh_1.docx");
        Assert.True(File.Exists(expected), $"Expected: {expected}");
    }

    [Fact]
    public void Execute_MissingSourceFile_AddsToSkippedAndContinues()
    {
        CreateSourceFile("dummy_data/doc/exists.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/doc/missing.docx" },
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/doc/exists.docx"  }
        };

        var result = _service.Execute(_runFolder, records, _tempDir);

        Assert.Equal(1, result.FilesWritten);
        Assert.Single(result.SkippedFiles);
    }

    [Fact]
    public void Execute_Result_CountsFoldersCreated()
    {
        CreateSourceFile("dummy_data/doc/test.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/doc/test.docx" }
        };

        var result = _service.Execute(_runFolder, records, _tempDir);

        // run folder + employee folder + "1. Docs" + "1.1 CV" = 4
        Assert.Equal(4, result.FoldersCreated);
    }
}
