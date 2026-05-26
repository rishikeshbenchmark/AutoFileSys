using DumpUtility.Helpers;
using PdfSharp.Drawing;
using PdfSharp.Pdf;
using PdfSharp.Pdf.IO;

namespace DumpUtility.Services;

public class PdfMergeService
{
    private static readonly HashSet<string> EligibleExtensions = new(StringComparer.OrdinalIgnoreCase)
    {
        ".pdf", ".jpg", ".jpeg", ".jfif", ".png", ".bmp", ".tiff", ".tif", ".webp"
    };

    private readonly LogHelper _log;

    public PdfMergeService(LogHelper log) => _log = log;

    public static bool IsEligible(string filePath) =>
        EligibleExtensions.Contains(Path.GetExtension(filePath));

    public void Merge(IEnumerable<string> sourcePaths, string outputPath)
    {
        using var output = new PdfDocument();

        foreach (string sourcePath in sourcePaths)
        {
            string ext = Path.GetExtension(sourcePath);
            try
            {
                if (ext.Equals(".pdf", StringComparison.OrdinalIgnoreCase))
                    ImportPdfPages(sourcePath, output);
                else
                    AppendImagePage(sourcePath, output);
            }
            catch (Exception ex)
            {
                _log.Warn($"Skipping '{Path.GetFileName(sourcePath)}' during merge: {ex.Message}");
            }
        }

        if (output.PageCount == 0)
            throw new InvalidOperationException(
                $"Merge produced zero pages; all source files failed for output '{outputPath}'.");

        output.Save(outputPath);
    }

    private static void ImportPdfPages(string sourcePath, PdfDocument output)
    {
        using PdfDocument input = PdfReader.Open(sourcePath, PdfDocumentOpenMode.Import);
        foreach (PdfPage page in input.Pages)
            output.AddPage(page);
    }

    private static void AppendImagePage(string sourcePath, PdfDocument output)
    {
        using XImage image = XImage.FromFile(sourcePath);

        bool landscape = image.PixelWidth > image.PixelHeight;
        PdfPage page   = output.AddPage();
        page.Width     = XUnit.FromMillimeter(landscape ? 297 : 210);
        page.Height    = XUnit.FromMillimeter(landscape ? 210 : 297);

        using XGraphics gfx = XGraphics.FromPdfPage(page);

        double scaleX = page.Width.Point  / image.PixelWidth;
        double scaleY = page.Height.Point / image.PixelHeight;
        double scale  = Math.Min(scaleX, scaleY);
        double w      = image.PixelWidth  * scale;
        double h      = image.PixelHeight * scale;
        double x      = (page.Width.Point  - w) / 2;
        double y      = (page.Height.Point - h) / 2;

        gfx.DrawImage(image, x, y, w, h);
    }
}
