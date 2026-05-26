# DumpUtility

DumpUtility is a .NET 8 console application that organises employee HR documents into a structured folder hierarchy. You point it at a base output folder, give it a list of employee records, and it creates a timestamped run folder, copies every source file into the correct sub-folder for that employee, merges PDFs and images into a single PDF per document category, and writes a plain-text log and summary when it finishes.

The typical output looks like this:

```
C:\DumpOutput\
  Dump_Utility_26052025_1430\
    0123-Rishikesh\
      1. Documents for Onboarding\
        1.1 CV Signed\
          CV Signed_0123-Rishikesh_merged.pdf
        1.4 Photograph\
          Photograph_0123-Rishikesh_merged.pdf
      2. Education Proof\
        ...
  Log_26052025_1430.txt
  Summary_26052025_1430.txt
```

Each PDF/image group within a folder category is merged into one PDF. Non-mergeable files (Word, Excel, etc.) are copied individually with a sequenced name.

---

## Prerequisites

- .NET 8 SDK — download from https://dotnet.microsoft.com/download/dotnet/8.0
- Windows (the publish target is `win-x64`; `dotnet run` works cross-platform if needed)

Verify your installation:

```powershell
dotnet --version
# should print 8.x.x
```

---

## Getting started

### 1. Clone the repository

```powershell
git clone <repo-url>
cd AutoFileSys
```

### 2. Configure the output folder

Open `DumpUtility/config.json` and set `BaseFolder` to wherever you want the output written:

```json
{
  "BaseFolder": "C:\\DumpOutput",
  "Database": {
    "Server": "",
    "Database": "",
    "UserId": "",
    "Password": ""
  }
}
```

The folder will be created automatically if it does not exist. The `Database` section is reserved for a future SQL Server integration and is not used right now.

### 3. Run the application

```powershell
cd DumpUtility
dotnet run
```

The first run will restore NuGet packages automatically. On completion the console prints a summary:

```
DumpUtility
Output : C:\DumpOutput\Dump_Utility_26052025_1430
Records: 20

  Processed: 0123-Rishikesh (6 file(s))
  Processed: 0456-Aamir (5 file(s))

Complete.
  Employees : 2
  Folders   : 14
  Files     : 11
  Merged    : 8 group(s) from 18 source file(s)
  Skipped   : 0
  Summary   : C:\DumpOutput\Summary_26052025_1430.txt
  Log       : C:\DumpOutput\Log_26052025_1430.txt
```

---

## Running the tests

```powershell
cd DumpUtility.Tests
dotnet test
```

Tests use xUnit and do not require any external setup. All test fixtures are created in the system temp folder and cleaned up afterwards, so nothing is written to your working directory.

---

## Publishing a standalone executable

This produces a single `.exe` in `DumpUtility/publish/` that runs without a .NET installation:

```powershell
cd DumpUtility
dotnet publish -c Release -r win-x64 --self-contained true -o publish
```

Copy the entire `publish/` folder to the target machine and run `DumpUtility.exe`. The `config.json` and `dummy_data/` folder are bundled automatically.

---

## Project structure

```
DumpUtility/
  Program.cs                   Entry point; orchestrates the pipeline
  config.json                  Runtime configuration (BaseFolder, DB)
  dummy_data/
    records.json               Employee records used during development
    pdf/ img/ doc/ xls/        Source files referenced by records.json
  Services/
    ConfigService              Loads and validates config.json
    DummyDataService           Reads records from records.json
    FolderService              Creates folder tree, copies files, triggers merges
    PdfMergeService            Merges PDFs and images (jpg, png, bmp, tiff, webp, jfif) into one PDF
    ReportService              Writes the plain-text summary file
  Helpers/
    LogHelper                  Thread-safe append-only logger (INFO / WARN / ERROR)
    FileNamingHelper           Naming rules for employee folders and destination file names
  Models/
    AppConfig / DatabaseConfig Config shape
    DocumentRecord             One record: EmployeeId, EmployeeName, FolderPath, FilePath

DumpUtility.Tests/
  Helpers/                     Unit tests for FileNamingHelper
  Services/                    Unit tests for FolderService, PdfMergeService, ReportService, etc.
```

---

## How records drive the output

Each record in `records.json` maps one source file to a folder category for a specific employee:

```json
{
  "EmployeeId": 123,
  "EmployeeName": "Rishikesh",
  "FolderPath": "1. Documents for Onboarding/1.1 CV Signed",
  "FilePath": "dummy_data/pdf/rishikesh.pdf"
}
```

Multiple records with the same `EmployeeId` and `FolderPath` that point to PDF or image files are merged into a single PDF in that folder. Records pointing to other file types (`.docx`, `.xlsx`) are copied individually.

Employee folder names are zero-padded: IDs below 10,000 become four digits, so employee 123 becomes `0123-Rishikesh`. Destination file names strip the numeric prefix from the category (e.g. `"1.1 CV Signed"` becomes `CV Signed`) and follow the pattern `{category}_{employeeFolder}_{sequence}.{ext}`.

---

## Current data source

The application currently runs against the dummy data in `dummy_data/`. A SQL Server view will replace this in a future release; the view will return records in the same shape as `records.json`, so the only change required will be swapping `DummyDataService` for a database-backed implementation of `IDataService`.
