using System.Text.RegularExpressions;

namespace DumpUtility.Helpers;

public static class FileNamingHelper
{
    private static readonly char[] InvalidChars = ['/', ':', '*', '?', '"', '<', '>', '|'];
    private static readonly Regex NumericPrefix = new(@"^\d+(\.\d+)*\s+", RegexOptions.Compiled);

    public static string GetEmployeeFolder(int employeeId, string employeeName)
    {
        string paddedId = employeeId < 10000
            ? employeeId.ToString().PadLeft(4, '0')
            : employeeId.ToString();
        return $"{paddedId}-{SanitizeSegment(employeeName)}";
    }

    public static string GetSubcategoryName(string folderPath)
    {
        string lastSegment = folderPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Last();
        return NumericPrefix.Replace(lastSegment, string.Empty);
    }

    public static string SanitizeSegment(string segment) =>
        string.Concat(segment.Select(c => InvalidChars.Contains(c) ? '_' : c));

    public static string BuildFileName(string subcategoryName, string employeeFolder, int sequence, string extension) =>
        $"{SanitizeSegment(subcategoryName)}_{SanitizeSegment(employeeFolder)}_{sequence}{extension}";
}
