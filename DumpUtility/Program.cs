using DumpUtility.Helpers;
using DumpUtility.Services;

string appBaseDir = AppContext.BaseDirectory;
string configPath = Path.Combine(appBaseDir, "config.json");
DateTime runTime  = DateTime.Now;
string timestamp  = runTime.ToString("ddMMyyyy_HHmm");

// ── Config ────────────────────────────────────────────────────────────────────
var configService = new ConfigService();
DumpUtility.Models.AppConfig config;

try
{
    config = configService.Load(configPath);
}
catch (Exception ex)
{
    Console.WriteLine($"[ERROR] Failed to load config.json: {ex.Message}");
    Environment.Exit(1);
    return;
}

// ── Logging ───────────────────────────────────────────────────────────────────
Directory.CreateDirectory(config.BaseFolder);
string logFilePath = Path.Combine(config.BaseFolder, $"Log_{timestamp}.txt");
var log = new LogHelper(logFilePath);

log.Info("App started");
log.Info($"Config loaded. BaseFolder: {config.BaseFolder}. DB config present: {!string.IsNullOrEmpty(config.Database?.Server)}");

try
{
    // ── Data ──────────────────────────────────────────────────────────────────
    string recordsPath = Path.Combine(appBaseDir, "dummy_data", "records.json");
    IDataService dataService = new DummyDataService(recordsPath);
    var records = dataService.GetRecords().ToList();
    log.Info($"Loaded {records.Count} record(s) from data service.");

    // ── Run folder ────────────────────────────────────────────────────────────
    string runFolderName = $"Dump_Utility_{timestamp}";
    string runFolderPath = Path.Combine(config.BaseFolder, runFolderName);

    Console.WriteLine();
    Console.WriteLine("DumpUtility");
    Console.WriteLine($"Output : {runFolderPath}");
    Console.WriteLine($"Records: {records.Count}");
    Console.WriteLine();

    // ── Folder + file operations ──────────────────────────────────────────────
    var folderService = new FolderService(log);
    var result = folderService.Execute(runFolderPath, records, appBaseDir);

    // ── Summary ───────────────────────────────────────────────────────────────
    string summaryPath = Path.Combine(config.BaseFolder, $"Summary_{timestamp}.txt");
    var reportService = new ReportService();
    reportService.WriteSummary(summaryPath, runTime, result);
    log.Info($"Summary written: {summaryPath}");

    // ── Done ──────────────────────────────────────────────────────────────────
    var duration = DateTime.Now - runTime;
    log.Info($"App finished. Duration: {duration:hh\\:mm\\:ss}. Files written: {result.FilesWritten}. Skipped: {result.SkippedFiles.Count}");

    Console.WriteLine();
    Console.WriteLine("Complete.");
    Console.WriteLine($"  Employees : {result.EmployeePaths.Count}");
    Console.WriteLine($"  Folders   : {result.FoldersCreated}");
    Console.WriteLine($"  Files     : {result.FilesWritten}");
    Console.WriteLine($"  Skipped   : {result.SkippedFiles.Count}");
    Console.WriteLine($"  Summary   : {summaryPath}");
    Console.WriteLine($"  Log       : {logFilePath}");
}
catch (Exception ex)
{
    log.Error($"Fatal error: {ex.Message}");
    Console.WriteLine($"[ERROR] {ex.Message}");
    Environment.Exit(1);
}
