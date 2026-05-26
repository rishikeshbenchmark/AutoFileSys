namespace DumpUtility.Helpers;

public class LogHelper
{
    private readonly string _logFilePath;
    private readonly object _lock = new();

    public LogHelper(string logFilePath)
    {
        _logFilePath = logFilePath;
        Directory.CreateDirectory(Path.GetDirectoryName(logFilePath)!);
    }

    public void Info(string message)  => Write("INFO ", message);
    public void Warn(string message)  => Write("WARN ", message);
    public void Error(string message) => Write("ERROR", message);

    private void Write(string level, string message)
    {
        string entry = $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] [{level}]  {message}";
        lock (_lock)
        {
            File.AppendAllText(_logFilePath, entry + Environment.NewLine);
        }
    }
}
