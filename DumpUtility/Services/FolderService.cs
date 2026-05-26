using DumpUtility.Helpers;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class FolderResult
{
    public int FoldersCreated { get; set; }
    public int FilesWritten { get; set; }
    public int MergedGroups { get; set; }
    public int FilesProcessedInMerge { get; set; }
    public List<(string FilePath, string Error)> SkippedFiles { get; } = [];
    public Dictionary<string, string> EmployeePaths { get; } = [];
}

public class FolderService
{
    private readonly LogHelper _log;
    private readonly PdfMergeService _mergeService;

    public FolderService(LogHelper log, PdfMergeService mergeService)
    {
        _log          = log;
        _mergeService = mergeService;
    }

    public FolderResult Execute(string runFolderPath, IEnumerable<DocumentRecord> records, string appBaseDir)
    {
        var result = new FolderResult();
        EnsureFolder(runFolderPath, result);

        var byEmployee = records
            .GroupBy(r => r.EmployeeId)
            .OrderBy(g => g.Key);

        foreach (var employeeGroup in byEmployee)
        {
            var first          = employeeGroup.First();
            string empFolder   = FileNamingHelper.GetEmployeeFolder(first.EmployeeId, first.EmployeeName);
            string empPath     = Path.Combine(runFolderPath, empFolder);

            EnsureFolder(empPath, result);
            result.EmployeePaths[empFolder] = empPath;

            int filesBeforeEmployee = result.FilesWritten;

            foreach (var folderGroup in employeeGroup.GroupBy(r => r.FolderPath))
            {
                string destFolder  = BuildFullPath(empPath, folderGroup.Key, result);
                string subCategory = FileNamingHelper.GetSubcategoryName(folderGroup.Key);

                var nonEligible = folderGroup.Where(r => !PdfMergeService.IsEligible(r.FilePath)).ToList();
                var eligible    = folderGroup.Where(r =>  PdfMergeService.IsEligible(r.FilePath)).ToList();

                // Pass 1: non-eligible files — copy individually with sequence names
                int seq = 0;
                foreach (var record in nonEligible)
                {
                    seq++;
                    string ext          = Path.GetExtension(record.FilePath);
                    string destFileName = FileNamingHelper.BuildFileName(subCategory, empFolder, seq, ext);
                    string destPath     = Path.Combine(destFolder, destFileName);
                    string sourcePath   = ResolveSourcePath(record.FilePath, appBaseDir);

                    try
                    {
                        if (!File.Exists(sourcePath))
                            throw new FileNotFoundException($"Source file not found: {sourcePath}");
                        File.Copy(sourcePath, destPath, overwrite: true);
                        result.FilesWritten++;
                        _log.Info($"File written: {record.FilePath} → {destFileName}");
                    }
                    catch (Exception ex)
                    {
                        result.SkippedFiles.Add((record.FilePath, ex.Message));
                        _log.Error($"Failed to copy '{record.FilePath}': {ex.Message}");
                    }
                }

                // Pass 2: eligible files — resolve paths, then merge into one PDF
                if (eligible.Count == 0)
                    continue;

                result.FilesProcessedInMerge += eligible.Count;

                var resolvedPaths = new List<string>();
                foreach (var record in eligible)
                {
                    string sourcePath = ResolveSourcePath(record.FilePath, appBaseDir);
                    if (!File.Exists(sourcePath))
                    {
                        result.SkippedFiles.Add((record.FilePath, $"Source file not found: {sourcePath}"));
                        _log.Error($"Eligible file missing, skipped from merge: {sourcePath}");
                    }
                    else
                    {
                        resolvedPaths.Add(sourcePath);
                    }
                }

                if (resolvedPaths.Count == 0)
                    continue;

                string mergedFileName = FileNamingHelper.BuildMergedFileName(subCategory, empFolder);
                string mergedPath     = Path.Combine(destFolder, mergedFileName);

                try
                {
                    _mergeService.Merge(resolvedPaths, mergedPath);
                    result.FilesWritten++;
                    result.MergedGroups++;
                    _log.Info($"Merged {resolvedPaths.Count} file(s) → {mergedFileName}");
                }
                catch (Exception ex)
                {
                    result.SkippedFiles.Add((folderGroup.Key, $"Merge failed: {ex.Message}"));
                    _log.Error($"Merge failed for '{folderGroup.Key}': {ex.Message}");
                }
            }

            int filesForEmployee = result.FilesWritten - filesBeforeEmployee;
            Console.WriteLine($"  Processed: {empFolder} ({filesForEmployee} file(s))");
        }

        return result;
    }

    private static string ResolveSourcePath(string filePath, string appBaseDir) =>
        Path.IsPathRooted(filePath) ? filePath : Path.Combine(appBaseDir, filePath);

    private void EnsureFolder(string path, FolderResult result)
    {
        if (!Directory.Exists(path))
        {
            Directory.CreateDirectory(path);
            result.FoldersCreated++;
            _log.Info($"Created folder: {path}");
        }
    }

    private string BuildFullPath(string employeePath, string folderPath, FolderResult result)
    {
        string[] segments = folderPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Select(FileNamingHelper.SanitizeSegment)
            .ToArray();

        string current = employeePath;
        foreach (string segment in segments)
        {
            current = Path.Combine(current, segment);
            EnsureFolder(current, result);
        }
        return current;
    }
}
