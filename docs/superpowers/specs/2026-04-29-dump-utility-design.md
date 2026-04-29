# DumpUtility — Design Spec
**Date:** 2026-04-29
**Target:** .NET 8, C# Console Application, Windows Server

---

## Overview

DumpUtility is a standalone Windows `.exe` that reads a config file and a data source, then creates a complete folder and file structure inside a base output folder — one folder tree per employee — by copying source files into the correct locations with the correct naming convention.

Phase 1 uses a local dummy JSON file as the data source. Phase 2 will swap in a SQL Server data service that queries a DB view with the same shape.

---

## Configuration (`config.json`)

Located in the same directory as the `.exe`.

```json
{
  "BaseFolder": "C:\\DumpOutput",
  "Database": {
    "Server": "SERVER_NAME",
    "DatabaseName": "DB_NAME",
    "Username": "sa",
    "Password": "your_password",
    "Port": 1433
  }
}
```

- `BaseFolder`: root path where all output is written.
- `Database`: reserved for Phase 2 SQL Server connection — not used in Phase 1.

---

## Data Model

### `DocumentRecord`
Mirrors one row from the DB view (or dummy JSON):

| Field | Type | Example |
|---|---|---|
| `EmployeeId` | `int` | `123` |
| `EmployeeName` | `string` | `"Rishikesh"` |
| `FolderPath` | `string` | `"1. Documents for Onboarding\1.1 CV Signed"` |
| `FilePath` | `string` | `"\\server\docs\cv.pdf"` |

`FolderPath` is a single column containing the full relative path (any depth). `FilePath` is the source file path on disk.

### `AppConfig`
Deserializes `config.json`. Contains `string BaseFolder` and `DatabaseConfig Database`.

### `DatabaseConfig`
Holds Server, DatabaseName, Username, Password, Port. Not used in Phase 1.

---

## Data Service Interface

```csharp
interface IDataService {
    IEnumerable<DocumentRecord> GetRecords();
}
```

**Phase 1 — `DummyDataService`:** Reads `dummy_data/records.json` (path relative to the `.exe`). Returns the deserialized list.

**Phase 2 — `SqlDataService`:** Queries the DB view using the connection from `AppConfig.Database`. Returns rows mapped to `DocumentRecord`. Swap is one line in `Program.cs`.

---

## Dummy Data

`dummy_data/` lives in the project root and is copied to the output directory on build.

```
dummy_data/
├── records.json          ← simulated view rows (DocumentRecord array)
├── doc/
│   ├── rishikesh.docx
│   ├── rishikesh2.docx
│   ├── aamir.docx
│   └── aamir2.docx
├── img/
│   ├── rishikesh.jfif
│   ├── rishikesh2.jfif
│   ├── aamir.jfif
│   └── aamir2.jfif
├── pdf/
│   ├── rishikesh.pdf
│   ├── rishikesh2.pdf
│   ├── aamir.pdf
│   └── aamir2.pdf
└── xls/
    ├── rishikesh.xls
    └── aamir.xls
```

`records.json` contains hardcoded rows that point to files inside `dummy_data/` and cover a representative set of folder paths for both employees.

---

## Output Folder Structure

All output goes inside `BaseFolder`.

```
{BaseFolder}\
  Dump_Utility_DDMMYYYY_HHMM\               ← run folder, created once per execution
    {EmployeeFolder}\                         ← one per employee
      {FolderPath}\                           ← full path from the record (any depth)
        {FileName}_{EmployeeFolder}_{N}.ext   ← copied & renamed file
```

### Naming Rules

**Run folder:** `Dump_Utility_DDMMYYYY_HHMM`
- Example: `Dump_Utility_29042026_1300`

**Employee folder:** `{EmployeeId zero-padded to 4 digits}-{EmployeeName}`
- Example: `0123-Rishikesh`, `0456-Aamir`

**File name:** `{SubcategoryName}_{EmployeeFolder}_{SequenceNumber}.{ext}`
- `SubcategoryName` = last segment of `FolderPath`, with numeric prefix stripped using pattern `^\d+(\.\d+)*\s+`
- `"1.1 CV Signed"` → `"CV Signed"`
- `"9.1.1 Mid-Year"` → `"Mid-Year"`
- If the last segment has no numeric prefix, use it as-is
- Sequence number starts at `1`, increments per unique `(employee, folderPath)` combination
- Extension is taken from the source `FilePath`
- `EmployeeId` is zero-padded to 4 digits; if ID has 5+ digits, no padding is applied

**Full example:**
```
C:\DumpOutput\
  Dump_Utility_29042026_1300\
    0123-Rishikesh\
      1. Documents for Onboarding\
        1.1 CV Signed\
          CV Signed_0123-Rishikesh_1.pdf
          CV Signed_0123-Rishikesh_2.docx
    0456-Aamir\
      1. Documents for Onboarding\
        1.1 CV Signed\
          CV Signed_0456-Aamir_1.pdf
```

**Sanitization:** `FolderPath` is split on `\` into individual segments first. Each segment (and the file name) has `/ : * ? " < > |` stripped. The backslash is the path separator and is never stripped from the path itself.

---

## Project Structure

```
DumpUtility/
├── DumpUtility.csproj
├── Program.cs                    ← orchestrates all steps
├── config.json
├── dummy_data/
│   ├── records.json
│   ├── doc/ img/ pdf/ xls/
├── Models/
│   ├── AppConfig.cs
│   ├── DatabaseConfig.cs
│   └── DocumentRecord.cs
├── Services/
│   ├── IDataService.cs
│   ├── DummyDataService.cs
│   ├── ConfigService.cs
│   ├── FolderService.cs
│   └── ReportService.cs
└── Helpers/
    ├── FileNamingHelper.cs
    └── LogHelper.cs
```

---

## Execution Flow

```
Step 1  Read config.json via ConfigService
Step 2  Load records via IDataService.GetRecords()
Step 3  Create run folder: Dump_Utility_DDMMYYYY_HHMM inside BaseFolder
Step 4  Group records by EmployeeId
Step 5  For each employee:
          a. Create employee folder
          b. For each record:
               - Directory.CreateDirectory(full FolderPath)  [idempotent]
               - Copy source file, rename per convention
               - Increment sequence counter for (employee, folderPath)
               - Log each operation
          c. Print progress line to console
Step 6  Write Summary_DDMMYYYY_HHMM.txt inside run folder
Step 7  Write Log_DDMMYYYY_HHMM.txt inside BaseFolder
Step 8  Print completion message to console
```

---

## Error Handling

- Each file copy is wrapped in `try/catch`
- Failed files are logged with full exception message and added to a skipped list
- The app continues processing remaining files — one failure does not abort the run
- Missing `config.json` or invalid `BaseFolder` → log error + exit with non-zero code
- Missing source file (FilePath does not exist) → log as skipped, continue

---

## Summary File

`Summary_DDMMYYYY_HHMM.txt` inside the run folder:

```
DumpUtility Run Summary
=======================
Run Timestamp   : 2026-04-29 13:00:00
Employees       : 2
Folders Created : 24
Files Written   : 18
Files Skipped   : 0

Employees Processed:
  0123-Rishikesh  →  C:\DumpOutput\Dump_Utility_29042026_1300\0123-Rishikesh
  0456-Aamir      →  C:\DumpOutput\Dump_Utility_29042026_1300\0456-Aamir

Skipped Files:
  (none)
```

---

## Log File

`Log_DDMMYYYY_HHMM.txt` inside `BaseFolder` (not inside the run folder):

```
[2026-04-29 13:00:00] [INFO]  App started
[2026-04-29 13:00:00] [INFO]  Config loaded. BaseFolder: C:\DumpOutput. DB config present: Yes
[2026-04-29 13:00:01] [INFO]  Created run folder: C:\DumpOutput\Dump_Utility_29042026_1300
[2026-04-29 13:00:01] [INFO]  Created folder: C:\DumpOutput\...\1.1 CV Signed
[2026-04-29 13:00:01] [INFO]  File written: dummy_data\pdf\rishikesh.pdf → CV Signed_0123-Rishikesh_1.pdf
[2026-04-29 13:00:04] [ERROR] Failed to copy file: <exception message>
[2026-04-29 13:00:05] [INFO]  App finished. Duration: 00:00:05. Files written: 18. Skipped: 0
```

---

## Technical Requirements

- .NET 8, C# Console Application
- `System.Text.Json` for JSON deserialization — no external NuGet packages
- Publish as self-contained single `.exe`: `dotnet publish -r win-x64 --self-contained`
- All file I/O via `System.IO`
- `IDataService` interface ensures DB swap requires no changes to `FolderService`, `ReportService`, or `Program.cs`
- `LogHelper` is a shared singleton used by all services
- `Directory.CreateDirectory` used throughout (idempotent, handles any depth)
- Console output: one progress line per employee during processing
