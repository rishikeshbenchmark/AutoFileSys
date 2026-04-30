using DumpUtility.Helpers;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class FolderResult
{
    public int FoldersCreated { get; set; }
    public int FilesWritten { get; set; }
    public List<(string FilePath, string Error)> SkippedFiles { get; } = [];
    public Dictionary<string, string> EmployeePaths { get; } = [];
}

public class FolderService
{
    private readonly LogHelper _log;

    public FolderService(LogHelper log) => _log = log;

    public FolderResult Execute(string runFolderPath, IEnumerable<DocumentRecord> records, string appBaseDir)
    {
        var result = new FolderResult();

        EnsureFolder(runFolderPath, result);

        var byEmployee = records
            .GroupBy(r => r.EmployeeId)
            .OrderBy(g => g.Key);

        foreach (var group in byEmployee)
        {
            var first = group.First();
            string employeeFolder = FileNamingHelper.GetEmployeeFolder(first.EmployeeId, first.EmployeeName);
            string employeePath   = Path.Combine(runFolderPath, employeeFolder);

            EnsureFolder(employeePath, result);
            result.EmployeePaths[employeeFolder] = employeePath;

            var sequences = new Dictionary<string, int>(StringComparer.OrdinalIgnoreCase);
            int filesBeforeEmployee = result.FilesWritten;

            foreach (var record in group.OrderBy(r => r.FolderPath))
            {
                string fullFolderPath = BuildFullPath(employeePath, record.FolderPath, result);

                sequences.TryGetValue(record.FolderPath, out int seq);
                seq++;
                sequences[record.FolderPath] = seq;

                string subcategoryName = FileNamingHelper.GetSubcategoryName(record.FolderPath);
                string extension       = Path.GetExtension(record.FilePath);
                string destFileName    = FileNamingHelper.BuildFileName(subcategoryName, employeeFolder, seq, extension);
                string destPath        = Path.Combine(fullFolderPath, destFileName);

                string sourcePath = Path.IsPathRooted(record.FilePath)
                    ? record.FilePath
                    : Path.Combine(appBaseDir, record.FilePath);

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
                    _log.Error($"Failed to copy file '{record.FilePath}': {ex.Message}");
                }
            }

            int filesForEmployee = result.FilesWritten - filesBeforeEmployee;
            Console.WriteLine($"  Processed: {employeeFolder} ({filesForEmployee} file(s))");
        }

        return result;
    }

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
