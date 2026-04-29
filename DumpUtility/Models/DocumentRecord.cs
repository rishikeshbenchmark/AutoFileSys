namespace DumpUtility.Models;

public class DocumentRecord
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
