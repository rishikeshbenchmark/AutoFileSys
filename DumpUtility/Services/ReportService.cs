using System.Text;

namespace DumpUtility.Services;

public class ReportService
{
    public void WriteSummary(string summaryFilePath, DateTime runTime, FolderResult result)
    {
        var sb = new StringBuilder();
        sb.AppendLine("DumpUtility Run Summary");
        sb.AppendLine("=======================");
        sb.AppendLine($"Run Timestamp   : {runTime:yyyy-MM-dd HH:mm:ss}");
        sb.AppendLine($"Employees       : {result.EmployeePaths.Count}");
        sb.AppendLine($"Folders Created : {result.FoldersCreated}");
        sb.AppendLine($"Files Written   : {result.FilesWritten}");
        sb.AppendLine($"Files Skipped   : {result.SkippedFiles.Count}");
        sb.AppendLine();
        sb.AppendLine("Employees Processed:");
        foreach (var (folder, path) in result.EmployeePaths)
            sb.AppendLine($"  {folder}  →  {path}");
        sb.AppendLine();
        sb.AppendLine("Skipped Files:");
        if (result.SkippedFiles.Count == 0)
            sb.AppendLine("  (none)");
        else
            foreach (var (file, error) in result.SkippedFiles)
                sb.AppendLine($"  {file} — {error}");

        File.WriteAllText(summaryFilePath, sb.ToString());
    }
}
