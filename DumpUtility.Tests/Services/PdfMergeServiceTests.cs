using DumpUtility.Helpers;
using DumpUtility.Services;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;
using Xunit;

namespace DumpUtility.Tests.Services;

public class PdfMergeServiceTests : IDisposable
{
    private readonly string _tmpDir;
    private readonly PdfMergeService _svc;
    private readonly string _logPath;

    // Minimal 1×1 white BMP (58 bytes) — no external image library needed.
    private static readonly byte[] MinimalBmpBytes = {
        0x42, 0x4D, 0x3A, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x36, 0x00, 0x00, 0x00,
        0x28, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00, 0x00, 0x00, 0x01, 0x00,
        0x18, 0x00, 0x00, 0x00, 0x00, 0x00, 0x04, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00, 0x00,
        0xFF, 0xFF, 0xFF, 0x00
    };

    public PdfMergeServiceTests()
    {
        _tmpDir  = Path.Combine(Path.GetTempPath(), $"PdfMergeTests_{Guid.NewGuid():N}");
        Directory.CreateDirectory(_tmpDir);
        _logPath = Path.Combine(_tmpDir, "test.log");
        _svc     = new PdfMergeService(new LogHelper(_logPath));
    }

    public void Dispose() => Directory.Delete(_tmpDir, recursive: true);

    // ── fixture helpers ────────────────────────────────────────────────────────

    private string MakePdf(int pageCount = 1, string? name = null)
    {
        string path = Path.Combine(_tmpDir, name ?? $"{Guid.NewGuid():N}.pdf");
        using var doc = new PdfDocument();
        for (int i = 0; i < pageCount; i++) doc.AddPage();
        doc.Save(path);
        return path;
    }

    private string MakeBmp(string? name = null)
    {
        string path = Path.Combine(_tmpDir, name ?? $"{Guid.NewGuid():N}.bmp");
        File.WriteAllBytes(path, MinimalBmpBytes);
        return path;
    }

    private string MakeCorrupt(string ext = ".pdf", string? name = null)
    {
        string path = Path.Combine(_tmpDir, name ?? $"{Guid.NewGuid():N}{ext}");
        File.WriteAllText(path, "this is not a valid file");
        return path;
    }

    private int PageCount(string pdfPath)
    {
        using var doc = PdfReader.Open(pdfPath, PdfDocumentOpenMode.Import);
        return doc.PageCount;
    }

    // ── IsEligible ─────────────────────────────────────────────────────────────

    [Theory]
    [InlineData(".pdf",  true)]
    [InlineData(".jpg",  true)]
    [InlineData(".jpeg", true)]
    [InlineData(".jfif", true)]
    [InlineData(".png",  true)]
    [InlineData(".bmp",  true)]
    [InlineData(".tiff", true)]
    [InlineData(".tif",  true)]
    [InlineData(".webp", true)]
    [InlineData(".docx", false)]
    [InlineData(".xlsx", false)]
    [InlineData(".txt",  false)]
    [InlineData("",      false)]
    public void IsEligible_CorrectForAllExtensions(string ext, bool expected)
    {
        Assert.Equal(expected, PdfMergeService.IsEligible($"file{ext}"));
    }

    [Theory]
    [InlineData(".PDF")]
    [InlineData(".JPG")]
    [InlineData(".BMP")]
    public void IsEligible_CaseInsensitive(string ext)
    {
        Assert.True(PdfMergeService.IsEligible($"file{ext}"));
    }

    // ── Merge ──────────────────────────────────────────────────────────────────

    [Fact]
    public void SinglePdf_ProducesExactPageCount()
    {
        string src    = MakePdf(1);
        string output = Path.Combine(_tmpDir, "out.pdf");

        _svc.Merge([src], output);

        Assert.Equal(1, PageCount(output));
    }

    [Fact]
    public void MultiPdf_PageCountsSum()
    {
        string src1   = MakePdf(2);
        string src2   = MakePdf(3);
        string output = Path.Combine(_tmpDir, "out.pdf");

        _svc.Merge([src1, src2], output);

        Assert.Equal(5, PageCount(output));
    }

    [Fact]
    public void SingleImage_ProducesOnePage()
    {
        string src    = MakeBmp();
        string output = Path.Combine(_tmpDir, "out.pdf");

        _svc.Merge([src], output);

        Assert.Equal(1, PageCount(output));
    }

    [Fact]
    public void PdfAndImage_ProducesSummedPages()
    {
        string pdf    = MakePdf(2);
        string img    = MakeBmp();
        string output = Path.Combine(_tmpDir, "out.pdf");

        _svc.Merge([pdf, img], output);

        Assert.Equal(3, PageCount(output));
    }

    [Fact]
    public void CorruptFile_IsSkippedMergeSucceeds()
    {
        string valid   = MakePdf(1);
        string corrupt = MakeCorrupt(".pdf");
        string output  = Path.Combine(_tmpDir, "out.pdf");

        _svc.Merge([valid, corrupt], output);

        Assert.Equal(1, PageCount(output));
        Assert.Contains("WARN", File.ReadAllText(_logPath));
    }

    [Fact]
    public void AllFilesFail_Throws()
    {
        string c1     = MakeCorrupt(".pdf", "c1.pdf");
        string c2     = MakeCorrupt(".pdf", "c2.pdf");
        string output = Path.Combine(_tmpDir, "out.pdf");

        Assert.Throws<InvalidOperationException>(() => _svc.Merge([c1, c2], output));
    }
}
