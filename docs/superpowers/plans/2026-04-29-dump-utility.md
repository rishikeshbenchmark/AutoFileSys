# DumpUtility Implementation Plan

> **For agentic workers:** REQUIRED SUB-SKILL: Use superpowers:subagent-driven-development (recommended) or superpowers:executing-plans to implement this plan task-by-task. Steps use checkbox (`- [ ]`) syntax for tracking.

**Goal:** Build a self-contained Windows .NET 8 console application that reads `config.json` and a dummy JSON data source (simulating a DB view), then creates a structured folder/file tree per employee with a specific naming convention.

**Architecture:** Service-oriented design with an `IDataService` interface swappable between `DummyDataService` (Phase 1, reads `records.json`) and a future `SqlDataService` (Phase 2). `FolderService` drives the core loop. `LogHelper` and `ReportService` handle observability. No changes to `FolderService`, `ReportService`, or `Program.cs` are required to swap the data source.

**Tech Stack:** .NET 8, C#, System.Text.Json (no external packages in main app), xUnit 2.9 for tests.

---

## File Map

```
/mnt/d/rishi/AutoFileSys/
├── DumpUtility.sln
├── DumpUtility/
│   ├── DumpUtility.csproj
│   ├── Program.cs
│   ├── config.json
│   ├── dummy_data/
│   │   ├── records.json
│   │   ├── doc/    (rishikesh.docx, rishikesh2.docx, aamir.docx, aamir2.docx)
│   │   ├── img/    (rishikesh.jfif, rishikesh2.jfif, aamir.jfif, aamir2.jfif)
│   │   ├── pdf/    (rishikesh.pdf, rishikesh2.pdf, aamir.pdf, aamir2.pdf)
│   │   └── xls/    (rishikesh.xls, aamir.xls)
│   ├── Models/
│   │   ├── AppConfig.cs
│   │   ├── DatabaseConfig.cs
│   │   └── DocumentRecord.cs
│   ├── Services/
│   │   ├── IDataService.cs
│   │   ├── DummyDataService.cs
│   │   ├── ConfigService.cs
│   │   ├── FolderService.cs          (also contains FolderResult)
│   │   └── ReportService.cs
│   └── Helpers/
│       ├── FileNamingHelper.cs
│       └── LogHelper.cs
└── DumpUtility.Tests/
    ├── DumpUtility.Tests.csproj
    ├── Helpers/
    │   └── FileNamingHelperTests.cs
    └── Services/
        ├── ConfigServiceTests.cs
        ├── DummyDataServiceTests.cs
        ├── FolderServiceTests.cs
        └── ReportServiceTests.cs
```

---

## Task 1: Scaffold solution and projects

**Files:**
- Create: `DumpUtility.sln`
- Create: `DumpUtility/DumpUtility.csproj`
- Create: `DumpUtility.Tests/DumpUtility.Tests.csproj`

- [ ] **Step 1: Create the solution and projects**

Run from `/mnt/d/rishi/AutoFileSys`:
```bash
dotnet new sln -n DumpUtility
dotnet new console -n DumpUtility -o DumpUtility --framework net8.0
dotnet new xunit -n DumpUtility.Tests -o DumpUtility.Tests --framework net8.0
dotnet sln DumpUtility.sln add DumpUtility/DumpUtility.csproj
dotnet sln DumpUtility.sln add DumpUtility.Tests/DumpUtility.Tests.csproj
dotnet add DumpUtility.Tests/DumpUtility.Tests.csproj reference DumpUtility/DumpUtility.csproj
```

- [ ] **Step 2: Replace the generated DumpUtility.csproj**

Replace the contents of `DumpUtility/DumpUtility.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <OutputType>Exe</OutputType>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <RootNamespace>DumpUtility</RootNamespace>
    <AssemblyName>DumpUtility</AssemblyName>
  </PropertyGroup>
  <ItemGroup>
    <Content Include="config.json">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
    <Content Include="dummy_data\**\*">
      <CopyToOutputDirectory>PreserveNewest</CopyToOutputDirectory>
    </Content>
  </ItemGroup>
</Project>
```

- [ ] **Step 3: Replace the generated DumpUtility.Tests.csproj**

Replace the contents of `DumpUtility.Tests/DumpUtility.Tests.csproj` with:

```xml
<Project Sdk="Microsoft.NET.Sdk">
  <PropertyGroup>
    <TargetFramework>net8.0</TargetFramework>
    <Nullable>enable</Nullable>
    <ImplicitUsings>enable</ImplicitUsings>
    <IsPackable>false</IsPackable>
  </PropertyGroup>
  <ItemGroup>
    <PackageReference Include="Microsoft.NET.Test.Sdk" Version="17.10.0" />
    <PackageReference Include="xunit" Version="2.9.0" />
    <PackageReference Include="xunit.runner.visualstudio" Version="2.8.2">
      <IncludeAssets>runtime; build; native; contentfiles; analyzers; buildtransitive</IncludeAssets>
      <PrivateAssets>all</PrivateAssets>
    </PackageReference>
  </ItemGroup>
  <ItemGroup>
    <ProjectReference Include="..\DumpUtility\DumpUtility.csproj" />
  </ItemGroup>
</Project>
```

- [ ] **Step 4: Delete generated boilerplate files**

```bash
rm DumpUtility/Program.cs
rm DumpUtility.Tests/UnitTest1.cs
```

- [ ] **Step 5: Create directory structure**

```bash
mkdir -p DumpUtility/Models
mkdir -p DumpUtility/Services
mkdir -p DumpUtility/Helpers
mkdir -p DumpUtility.Tests/Helpers
mkdir -p DumpUtility.Tests/Services
```

- [ ] **Step 6: Verify solution builds (will error on missing Program.cs — create a stub)**

Create `DumpUtility/Program.cs` with just:
```csharp
// entry point — wired in Task 9
```

Then run:
```bash
dotnet build DumpUtility.sln
```
Expected: Build succeeded (0 errors).

- [ ] **Step 7: Copy existing dummy_data files into the project**

```bash
cp -r /mnt/d/rishi/AutoFileSys/dummy_data /mnt/d/rishi/AutoFileSys/DumpUtility/dummy_data
```

Verify:
```bash
find DumpUtility/dummy_data -type f | sort
```
Expected output includes: `doc/aamir.docx`, `img/rishikesh.jfif`, `pdf/aamir.pdf`, `xls/rishikesh.xls`, etc.

- [ ] **Step 8: Commit**

```bash
cd /mnt/d/rishi/AutoFileSys
git init   # only if not already a git repo; skip if already initialised
git add DumpUtility.sln DumpUtility/DumpUtility.csproj DumpUtility.Tests/DumpUtility.Tests.csproj DumpUtility/Program.cs DumpUtility/dummy_data
git commit -m "feat: scaffold DumpUtility solution with test project"
```

---

## Task 2: Models

**Files:**
- Create: `DumpUtility/Models/AppConfig.cs`
- Create: `DumpUtility/Models/DatabaseConfig.cs`
- Create: `DumpUtility/Models/DocumentRecord.cs`

No dedicated test file — model serialization is exercised in Task 5 (ConfigService) and Task 6 (DummyDataService) tests.

- [ ] **Step 1: Create `AppConfig.cs`**

```csharp
namespace DumpUtility.Models;

public class AppConfig
{
    public string BaseFolder { get; set; } = string.Empty;
    public DatabaseConfig Database { get; set; } = new();
}
```

- [ ] **Step 2: Create `DatabaseConfig.cs`**

```csharp
namespace DumpUtility.Models;

public class DatabaseConfig
{
    public string Server { get; set; } = string.Empty;
    public string DatabaseName { get; set; } = string.Empty;
    public string Username { get; set; } = string.Empty;
    public string Password { get; set; } = string.Empty;
    public int Port { get; set; } = 1433;
}
```

- [ ] **Step 3: Create `DocumentRecord.cs`**

```csharp
namespace DumpUtility.Models;

public class DocumentRecord
{
    public int EmployeeId { get; set; }
    public string EmployeeName { get; set; } = string.Empty;
    public string FolderPath { get; set; } = string.Empty;
    public string FilePath { get; set; } = string.Empty;
}
```

- [ ] **Step 4: Build to verify no compile errors**

```bash
dotnet build DumpUtility/DumpUtility.csproj
```
Expected: Build succeeded.

- [ ] **Step 5: Commit**

```bash
git add DumpUtility/Models/
git commit -m "feat: add AppConfig, DatabaseConfig, DocumentRecord models"
```

---

## Task 3: FileNamingHelper

**Files:**
- Create: `DumpUtility/Helpers/FileNamingHelper.cs`
- Create: `DumpUtility.Tests/Helpers/FileNamingHelperTests.cs`

- [ ] **Step 1: Write failing tests**

Create `DumpUtility.Tests/Helpers/FileNamingHelperTests.cs`:

```csharp
using DumpUtility.Helpers;

namespace DumpUtility.Tests.Helpers;

public class FileNamingHelperTests
{
    [Theory]
    [InlineData(123, "Rishikesh", "0123-Rishikesh")]
    [InlineData(456, "Aamir", "0456-Aamir")]
    [InlineData(9999, "Test", "9999-Test")]
    [InlineData(10000, "LargeId", "10000-LargeId")]
    [InlineData(1, "X", "0001-X")]
    public void GetEmployeeFolder_ReturnsCorrectFormat(int id, string name, string expected)
    {
        var result = FileNamingHelper.GetEmployeeFolder(id, name);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData(@"1. Documents for Onboarding\1.1 CV Signed", "CV Signed")]
    [InlineData(@"9. Performance Appraisal Form\9.2 Current Year\9.2.1 Mid-Year", "Mid-Year")]
    [InlineData(@"2. Education Proof\2.3 Graduation Degree or Provisional Certificate", "Graduation Degree or Provisional Certificate")]
    [InlineData(@"NoPrefix", "NoPrefix")]
    [InlineData(@"17. Exit\17.1 Resignation", "Resignation")]
    public void GetSubcategoryName_StripsNumericPrefix(string folderPath, string expected)
    {
        var result = FileNamingHelper.GetSubcategoryName(folderPath);
        Assert.Equal(expected, result);
    }

    [Theory]
    [InlineData("Hello:World", "Hello_World")]
    [InlineData("File*Name?", "File_Name_")]
    [InlineData("Normal Name", "Normal Name")]
    [InlineData("Doc\"Title\"", "Doc_Title_")]
    public void SanitizeSegment_ReplacesInvalidChars(string input, string expected)
    {
        var result = FileNamingHelper.SanitizeSegment(input);
        Assert.Equal(expected, result);
    }

    [Fact]
    public void BuildFileName_ReturnsCorrectFormat()
    {
        var result = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 1, ".pdf");
        Assert.Equal("CV Signed_0123-Rishikesh_1.pdf", result);
    }

    [Fact]
    public void BuildFileName_SequenceIncrements()
    {
        var first  = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 1, ".pdf");
        var second = FileNamingHelper.BuildFileName("CV Signed", "0123-Rishikesh", 2, ".docx");
        Assert.Equal("CV Signed_0123-Rishikesh_1.pdf",  first);
        Assert.Equal("CV Signed_0123-Rishikesh_2.docx", second);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~FileNamingHelperTests" -v minimal
```
Expected: Build error — `FileNamingHelper` does not exist yet.

- [ ] **Step 3: Create `FileNamingHelper.cs`**

```csharp
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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~FileNamingHelperTests" -v minimal
```
Expected: All tests pass.

- [ ] **Step 5: Commit**

```bash
git add DumpUtility/Helpers/FileNamingHelper.cs DumpUtility.Tests/Helpers/FileNamingHelperTests.cs
git commit -m "feat: add FileNamingHelper with naming convention and sanitization logic"
```

---

## Task 4: LogHelper

**Files:**
- Create: `DumpUtility/Helpers/LogHelper.cs`

No separate test file — LogHelper is exercised in FolderService and ConfigService tests via a temp log path.

- [ ] **Step 1: Create `LogHelper.cs`**

```csharp
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
```

- [ ] **Step 2: Build to verify no errors**

```bash
dotnet build DumpUtility/DumpUtility.csproj
```
Expected: Build succeeded.

- [ ] **Step 3: Commit**

```bash
git add DumpUtility/Helpers/LogHelper.cs
git commit -m "feat: add LogHelper for timestamped file logging"
```

---

## Task 5: ConfigService

**Files:**
- Create: `DumpUtility/Services/ConfigService.cs`
- Create: `DumpUtility/config.json`
- Create: `DumpUtility.Tests/Services/ConfigServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `DumpUtility.Tests/Services/ConfigServiceTests.cs`:

```csharp
using DumpUtility.Services;

namespace DumpUtility.Tests.Services;

public class ConfigServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ConfigServiceTests() => Directory.CreateDirectory(_tempDir);

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void Load_ValidConfig_ReturnsParsedValues()
    {
        string configPath = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(configPath, """
            {
              "BaseFolder": "C:\\Output",
              "Database": { "Server": "SRV", "DatabaseName": "DB", "Username": "sa", "Password": "pw", "Port": 1433 }
            }
            """);

        var service = new ConfigService();
        var config = service.Load(configPath);

        Assert.Equal("C:\\Output", config.BaseFolder);
        Assert.Equal("SRV", config.Database.Server);
        Assert.Equal(1433, config.Database.Port);
    }

    [Fact]
    public void Load_MissingFile_ThrowsFileNotFoundException()
    {
        var service = new ConfigService();
        Assert.Throws<FileNotFoundException>(() => service.Load(Path.Combine(_tempDir, "missing.json")));
    }

    [Fact]
    public void Load_EmptyBaseFolder_ThrowsInvalidOperationException()
    {
        string configPath = Path.Combine(_tempDir, "config.json");
        File.WriteAllText(configPath, """{ "BaseFolder": "", "Database": {} }""");

        var service = new ConfigService();
        Assert.Throws<InvalidOperationException>(() => service.Load(configPath));
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~ConfigServiceTests" -v minimal
```
Expected: Build error — `ConfigService` does not exist.

- [ ] **Step 3: Create `ConfigService.cs`**

```csharp
using System.Text.Json;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class ConfigService
{
    public AppConfig Load(string configPath)
    {
        if (!File.Exists(configPath))
            throw new FileNotFoundException($"config.json not found at: {configPath}");

        string json = File.ReadAllText(configPath);
        var config = JsonSerializer.Deserialize<AppConfig>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true });

        if (config is null)
            throw new InvalidOperationException("Failed to deserialize config.json.");

        if (string.IsNullOrWhiteSpace(config.BaseFolder))
            throw new InvalidOperationException("BaseFolder is required in config.json.");

        return config;
    }
}
```

- [ ] **Step 4: Create `config.json`**

Create `DumpUtility/config.json`:

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

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~ConfigServiceTests" -v minimal
```
Expected: 3 tests pass.

- [ ] **Step 6: Commit**

```bash
git add DumpUtility/Services/ConfigService.cs DumpUtility/config.json DumpUtility.Tests/Services/ConfigServiceTests.cs
git commit -m "feat: add ConfigService with config.json loading and validation"
```

---

## Task 6: IDataService, DummyDataService, and records.json

**Files:**
- Create: `DumpUtility/Services/IDataService.cs`
- Create: `DumpUtility/Services/DummyDataService.cs`
- Create: `DumpUtility/dummy_data/records.json`
- Create: `DumpUtility.Tests/Services/DummyDataServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `DumpUtility.Tests/Services/DummyDataServiceTests.cs`:

```csharp
using System.Text.Json;
using DumpUtility.Models;
using DumpUtility.Services;

namespace DumpUtility.Tests.Services;

public class DummyDataServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public DummyDataServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    [Fact]
    public void GetRecords_ValidJson_ReturnsAllRecords()
    {
        var records = new[]
        {
            new DocumentRecord { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = @"1. Docs\1.1 CV", FilePath = @"dummy_data\pdf\rishikesh.pdf" },
            new DocumentRecord { EmployeeId = 456, EmployeeName = "Aamir",     FolderPath = @"1. Docs\1.1 CV", FilePath = @"dummy_data\pdf\aamir.pdf"     }
        };
        string jsonPath = Path.Combine(_tempDir, "records.json");
        File.WriteAllText(jsonPath, JsonSerializer.Serialize(records));

        var service = new DummyDataService(jsonPath);
        var result = service.GetRecords().ToList();

        Assert.Equal(2, result.Count);
        Assert.Equal(123, result[0].EmployeeId);
        Assert.Equal("Aamir", result[1].EmployeeName);
    }

    [Fact]
    public void GetRecords_MissingFile_ThrowsFileNotFoundException()
    {
        var service = new DummyDataService(Path.Combine(_tempDir, "missing.json"));
        Assert.Throws<FileNotFoundException>(() => service.GetRecords().ToList());
    }

    [Fact]
    public void GetRecords_EmptyArray_ReturnsEmptyList()
    {
        string jsonPath = Path.Combine(_tempDir, "records.json");
        File.WriteAllText(jsonPath, "[]");

        var service = new DummyDataService(jsonPath);
        Assert.Empty(service.GetRecords());
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~DummyDataServiceTests" -v minimal
```
Expected: Build error — types do not exist yet.

- [ ] **Step 3: Create `IDataService.cs`**

```csharp
using DumpUtility.Models;

namespace DumpUtility.Services;

public interface IDataService
{
    IEnumerable<DocumentRecord> GetRecords();
}
```

- [ ] **Step 4: Create `DummyDataService.cs`**

```csharp
using System.Text.Json;
using DumpUtility.Models;

namespace DumpUtility.Services;

public class DummyDataService : IDataService
{
    private readonly string _recordsPath;

    public DummyDataService(string recordsPath) => _recordsPath = recordsPath;

    public IEnumerable<DocumentRecord> GetRecords()
    {
        if (!File.Exists(_recordsPath))
            throw new FileNotFoundException($"records.json not found at: {_recordsPath}");

        string json = File.ReadAllText(_recordsPath);
        return JsonSerializer.Deserialize<List<DocumentRecord>>(json,
            new JsonSerializerOptions { PropertyNameCaseInsensitive = true }) ?? [];
    }
}
```

- [ ] **Step 5: Run tests to verify they pass**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~DummyDataServiceTests" -v minimal
```
Expected: 3 tests pass.

- [ ] **Step 6: Create `dummy_data/records.json`**

Create `DumpUtility/dummy_data/records.json`:

```json
[
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "1. Documents for Onboarding/1.1 CV Signed",                                          "FilePath": "dummy_data/pdf/rishikesh.pdf"   },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "1. Documents for Onboarding/1.1 CV Signed",                                          "FilePath": "dummy_data/doc/rishikesh.docx"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "1. Documents for Onboarding/1.3 Offer letter - acknowledged",                        "FilePath": "dummy_data/pdf/rishikesh2.pdf"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "1. Documents for Onboarding/1.4 Photograph",                                         "FilePath": "dummy_data/img/rishikesh.jfif"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "2. Education Proof/2.1 SSC",                                                         "FilePath": "dummy_data/pdf/rishikesh.pdf"   },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "2. Education Proof/2.3 Graduation Degree or Provisional Certificate",                "FilePath": "dummy_data/pdf/rishikesh2.pdf"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "4. ID & Permanent Address/4.2 Aadhar Card",                                          "FilePath": "dummy_data/pdf/rishikesh.pdf"   },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "4. ID & Permanent Address/4.4 PAN Card",                                             "FilePath": "dummy_data/pdf/rishikesh2.pdf"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "7. JD & KRA/7.2 Current Year",                                                       "FilePath": "dummy_data/doc/rishikesh.docx"  },
  { "EmployeeId": 123, "EmployeeName": "Rishikesh", "FolderPath": "9. Performance Appraisal Form/9.2 Current Year/9.2.1 Mid-Year",                      "FilePath": "dummy_data/pdf/rishikesh.pdf"   },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "1. Documents for Onboarding/1.1 CV Signed",                                          "FilePath": "dummy_data/pdf/aamir.pdf"       },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "1. Documents for Onboarding/1.1 CV Signed",                                          "FilePath": "dummy_data/doc/aamir.docx"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "1. Documents for Onboarding/1.3 Offer letter - acknowledged",                        "FilePath": "dummy_data/pdf/aamir2.pdf"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "1. Documents for Onboarding/1.4 Photograph",                                         "FilePath": "dummy_data/img/aamir.jfif"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "2. Education Proof/2.1 SSC",                                                         "FilePath": "dummy_data/pdf/aamir.pdf"       },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "2. Education Proof/2.3 Graduation Degree or Provisional Certificate",                "FilePath": "dummy_data/pdf/aamir2.pdf"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "4. ID & Permanent Address/4.2 Aadhar Card",                                          "FilePath": "dummy_data/pdf/aamir.pdf"       },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "4. ID & Permanent Address/4.4 PAN Card",                                             "FilePath": "dummy_data/pdf/aamir2.pdf"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "7. JD & KRA/7.2 Current Year",                                                       "FilePath": "dummy_data/doc/aamir.docx"      },
  { "EmployeeId": 456, "EmployeeName": "Aamir",     "FolderPath": "9. Performance Appraisal Form/9.2 Current Year/9.2.1 Mid-Year",                      "FilePath": "dummy_data/pdf/aamir.pdf"       }
]
```

Note: `FolderPath` uses forward-slash `/` as separator — `FolderService` normalises to the OS separator at runtime.

- [ ] **Step 7: Commit**

```bash
git add DumpUtility/Services/IDataService.cs DumpUtility/Services/DummyDataService.cs
git add DumpUtility/dummy_data/records.json
git add DumpUtility.Tests/Services/DummyDataServiceTests.cs
git commit -m "feat: add IDataService, DummyDataService, and records.json dummy data"
```

---

## Task 7: FolderService

**Files:**
- Create: `DumpUtility/Services/FolderService.cs`  (also contains `FolderResult`)
- Create: `DumpUtility.Tests/Services/FolderServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `DumpUtility.Tests/Services/FolderServiceTests.cs`:

```csharp
using DumpUtility.Helpers;
using DumpUtility.Models;
using DumpUtility.Services;

namespace DumpUtility.Tests.Services;

public class FolderServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());
    private readonly LogHelper _log;
    private readonly string _runFolder;

    public FolderServiceTests()
    {
        Directory.CreateDirectory(_tempDir);
        _log = new LogHelper(Path.Combine(_tempDir, "test.log"));
        _runFolder = Path.Combine(_tempDir, "Dump_Utility_Test");
    }

    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private string CreateSourceFile(string relativePath)
    {
        string full = Path.Combine(_tempDir, relativePath);
        Directory.CreateDirectory(Path.GetDirectoryName(full)!);
        File.WriteAllText(full, "dummy");
        return full;
    }

    [Fact]
    public void Execute_CreatesRunFolder()
    {
        var records = new List<DocumentRecord>();
        var service = new FolderService(_log);
        service.Execute(_runFolder, records, _tempDir);
        Assert.True(Directory.Exists(_runFolder));
    }

    [Fact]
    public void Execute_CreatesEmployeeFolderWithPaddedId()
    {
        CreateSourceFile("dummy_data/pdf/test.pdf");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 7, EmployeeName = "Alice", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/pdf/test.pdf" }
        };

        var service = new FolderService(_log);
        service.Execute(_runFolder, records, _tempDir);

        Assert.True(Directory.Exists(Path.Combine(_runFolder, "0007-Alice")));
    }

    [Fact]
    public void Execute_CopiesFileWithCorrectName()
    {
        CreateSourceFile("dummy_data/pdf/test.pdf");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/pdf/test.pdf" }
        };

        var service = new FolderService(_log);
        service.Execute(_runFolder, records, _tempDir);

        string expected = Path.Combine(_runFolder, "0123-Rishikesh", "1. Docs", "1.1 CV Signed", "CV Signed_0123-Rishikesh_1.pdf");
        Assert.True(File.Exists(expected), $"Expected file not found: {expected}");
    }

    [Fact]
    public void Execute_MultipleFilesInSameFolder_SequenceIncrements()
    {
        CreateSourceFile("dummy_data/pdf/a.pdf");
        CreateSourceFile("dummy_data/doc/b.docx");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/pdf/a.pdf"  },
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV Signed", FilePath = "dummy_data/doc/b.docx" }
        };

        var service = new FolderService(_log);
        var result = service.Execute(_runFolder, records, _tempDir);

        string folder = Path.Combine(_runFolder, "0123-Rishikesh", "1. Docs", "1.1 CV Signed");
        Assert.True(File.Exists(Path.Combine(folder, "CV Signed_0123-Rishikesh_1.pdf")));
        Assert.True(File.Exists(Path.Combine(folder, "CV Signed_0123-Rishikesh_2.docx")));
        Assert.Equal(2, result.FilesWritten);
    }

    [Fact]
    public void Execute_ThreeLevelDeepPath_CreatesCorrectFolders()
    {
        CreateSourceFile("dummy_data/pdf/test.pdf");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh",
                    FolderPath = "9. Performance Appraisal/9.2 Current Year/9.2.1 Mid-Year",
                    FilePath   = "dummy_data/pdf/test.pdf" }
        };

        var service = new FolderService(_log);
        service.Execute(_runFolder, records, _tempDir);

        string expected = Path.Combine(_runFolder, "0123-Rishikesh",
            "9. Performance Appraisal", "9.2 Current Year", "9.2.1 Mid-Year",
            "Mid-Year_0123-Rishikesh_1.pdf");
        Assert.True(File.Exists(expected), $"Expected: {expected}");
    }

    [Fact]
    public void Execute_MissingSourceFile_AddsToSkippedAndContinues()
    {
        CreateSourceFile("dummy_data/pdf/exists.pdf");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/pdf/missing.pdf" },
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/pdf/exists.pdf"  }
        };

        var service = new FolderService(_log);
        var result = service.Execute(_runFolder, records, _tempDir);

        Assert.Equal(1, result.FilesWritten);
        Assert.Single(result.SkippedFiles);
    }

    [Fact]
    public void Execute_Result_CountsFoldersCreated()
    {
        CreateSourceFile("dummy_data/pdf/test.pdf");
        var records = new List<DocumentRecord>
        {
            new() { EmployeeId = 123, EmployeeName = "Rishikesh", FolderPath = "1. Docs/1.1 CV", FilePath = "dummy_data/pdf/test.pdf" }
        };

        var service = new FolderService(_log);
        var result = service.Execute(_runFolder, records, _tempDir);

        // run folder + employee folder + "1. Docs" + "1.1 CV" = 4
        Assert.Equal(4, result.FoldersCreated);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~FolderServiceTests" -v minimal
```
Expected: Build error — `FolderService` and `FolderResult` do not exist.

- [ ] **Step 3: Create `FolderService.cs`**

```csharp
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
                string fullFolderPath = BuildFullPath(employeePath, record.FolderPath);
                EnsureFolder(fullFolderPath, result);

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

    private static string BuildFullPath(string employeePath, string folderPath)
    {
        string[] segments = folderPath
            .Replace('/', Path.DirectorySeparatorChar)
            .Replace('\\', Path.DirectorySeparatorChar)
            .Split(Path.DirectorySeparatorChar, StringSplitOptions.RemoveEmptyEntries)
            .Select(FileNamingHelper.SanitizeSegment)
            .ToArray();

        return segments.Aggregate(employeePath, Path.Combine);
    }
}
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~FolderServiceTests" -v minimal
```
Expected: All 7 tests pass.

- [ ] **Step 5: Commit**

```bash
git add DumpUtility/Services/FolderService.cs DumpUtility.Tests/Services/FolderServiceTests.cs
git commit -m "feat: add FolderService — core folder creation and file copy loop"
```

---

## Task 8: ReportService

**Files:**
- Create: `DumpUtility/Services/ReportService.cs`
- Create: `DumpUtility.Tests/Services/ReportServiceTests.cs`

- [ ] **Step 1: Write failing tests**

Create `DumpUtility.Tests/Services/ReportServiceTests.cs`:

```csharp
using DumpUtility.Services;

namespace DumpUtility.Tests.Services;

public class ReportServiceTests : IDisposable
{
    private readonly string _tempDir = Path.Combine(Path.GetTempPath(), Guid.NewGuid().ToString());

    public ReportServiceTests() => Directory.CreateDirectory(_tempDir);
    public void Dispose() => Directory.Delete(_tempDir, recursive: true);

    private FolderResult BuildResult(int folders, int files, string empFolder, string empPath,
        List<(string, string)>? skipped = null)
    {
        var r = new FolderResult { FoldersCreated = folders, FilesWritten = files };
        r.EmployeePaths[empFolder] = empPath;
        skipped?.ForEach(s => r.SkippedFiles.Add(s));
        return r;
    }

    [Fact]
    public void WriteSummary_CreatesFile()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var service = new ReportService();
        service.WriteSummary(summaryPath, DateTime.Now, BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice"));
        Assert.True(File.Exists(summaryPath));
    }

    [Fact]
    public void WriteSummary_ContainsEmployeeCount()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var result = BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice");

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("Employees       : 1", content);
        Assert.Contains("Folders Created : 10", content);
        Assert.Contains("Files Written   : 5", content);
        Assert.Contains("Files Skipped   : 0", content);
    }

    [Fact]
    public void WriteSummary_ListsEmployeePaths()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var result = BuildResult(10, 5, "0123-Alice", @"C:\Out\0123-Alice");

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("0123-Alice", content);
        Assert.Contains(@"C:\Out\0123-Alice", content);
    }

    [Fact]
    public void WriteSummary_ShowsSkippedFiles()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        var skipped = new List<(string, string)> { ("missing.pdf", "File not found") };
        var result = BuildResult(5, 3, "0123-Alice", @"C:\Out\0123-Alice", skipped);

        new ReportService().WriteSummary(summaryPath, DateTime.Now, result);
        string content = File.ReadAllText(summaryPath);

        Assert.Contains("missing.pdf", content);
        Assert.Contains("File not found", content);
    }

    [Fact]
    public void WriteSummary_NoSkippedFiles_ShowsNone()
    {
        string summaryPath = Path.Combine(_tempDir, "Summary.txt");
        new ReportService().WriteSummary(summaryPath, DateTime.Now,
            BuildResult(5, 3, "0123-Alice", @"C:\Out\0123-Alice"));

        string content = File.ReadAllText(summaryPath);
        Assert.Contains("(none)", content);
    }
}
```

- [ ] **Step 2: Run tests to verify they fail**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~ReportServiceTests" -v minimal
```
Expected: Build error — `ReportService` does not exist.

- [ ] **Step 3: Create `ReportService.cs`**

```csharp
using System.Text;
using DumpUtility.Services;

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
```

- [ ] **Step 4: Run tests to verify they pass**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj --filter "FullyQualifiedName~ReportServiceTests" -v minimal
```
Expected: 5 tests pass.

- [ ] **Step 5: Run the full test suite**

```bash
dotnet test DumpUtility.Tests/DumpUtility.Tests.csproj -v minimal
```
Expected: All tests pass (0 failures).

- [ ] **Step 6: Commit**

```bash
git add DumpUtility/Services/ReportService.cs DumpUtility.Tests/Services/ReportServiceTests.cs
git commit -m "feat: add ReportService for summary file generation"
```

---

## Task 9: Wire Program.cs and end-to-end test

**Files:**
- Modify: `DumpUtility/Program.cs`

- [ ] **Step 1: Replace stub Program.cs with the full entry point**

```csharp
using DumpUtility.Helpers;
using DumpUtility.Services;

string appBaseDir  = AppContext.BaseDirectory;
string configPath  = Path.Combine(appBaseDir, "config.json");
DateTime runTime   = DateTime.Now;
string timestamp   = runTime.ToString("ddMMyyyy_HHmm");

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

// ── Logging (needs BaseFolder from config) ────────────────────────────────────
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
    Console.WriteLine($"DumpUtility");
    Console.WriteLine($"Output : {runFolderPath}");
    Console.WriteLine($"Records: {records.Count}");
    Console.WriteLine();

    // ── Folder + file operations ──────────────────────────────────────────────
    var folderService = new FolderService(log);
    var result = folderService.Execute(runFolderPath, records, appBaseDir);

    // ── Summary ───────────────────────────────────────────────────────────────
    string summaryPath = Path.Combine(runFolderPath, $"Summary_{timestamp}.txt");
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
```

- [ ] **Step 2: Build the main project**

```bash
dotnet build DumpUtility/DumpUtility.csproj
```
Expected: Build succeeded.

- [ ] **Step 3: Run end-to-end with dotnet run**

```bash
dotnet run --project DumpUtility/DumpUtility.csproj
```
Expected console output:
```
DumpUtility
Output : C:\DumpOutput\Dump_Utility_DDMMYYYY_HHMM
Records: 20

  Processed: 0123-Rishikesh (10 file(s))
  Processed: 0456-Aamir (10 file(s))

Complete.
  Employees : 2
  Folders   : 26
  Files     : 20
  Skipped   : 0
  Summary   : C:\DumpOutput\Dump_Utility_...\Summary_....txt
  Log       : C:\DumpOutput\Log_....txt
```

- [ ] **Step 4: Verify output folder structure**

Inspect the output directory and confirm:
- Run folder created: `C:\DumpOutput\Dump_Utility_DDMMYYYY_HHMM\`
- Employee folders: `0123-Rishikesh\` and `0456-Aamir\`
- Files follow naming convention: e.g. `CV Signed_0123-Rishikesh_1.pdf`, `CV Signed_0123-Rishikesh_2.docx`
- 3-level deep folder exists: `0123-Rishikesh\9. Performance Appraisal Form\9.2 Current Year\9.2.1 Mid-Year\`
- `Summary_....txt` exists inside run folder
- `Log_....txt` exists inside `C:\DumpOutput\`

- [ ] **Step 5: Commit**

```bash
git add DumpUtility/Program.cs
git commit -m "feat: wire Program.cs — full end-to-end execution flow"
```

---

## Task 10: Publish as self-contained .exe

- [ ] **Step 1: Publish for Windows x64**

Run from `/mnt/d/rishi/AutoFileSys`:
```bash
dotnet publish DumpUtility/DumpUtility.csproj \
  -r win-x64 \
  --self-contained true \
  -p:PublishSingleFile=true \
  -p:IncludeNativeLibrariesForSelfExtract=true \
  -c Release \
  -o DumpUtility/publish/
```

- [ ] **Step 2: Verify the output**

```bash
ls DumpUtility/publish/
```
Expected: `DumpUtility.exe` plus `dummy_data/` folder and `config.json` alongside it.

- [ ] **Step 3: Edit config.json in the publish folder to point at a real Windows path, then copy to the Windows server and run**

The `DumpUtility/publish/` directory is the deployable artifact. Copy it to the server and run `DumpUtility.exe`.

- [ ] **Step 4: Commit**

```bash
echo "DumpUtility/publish/" >> .gitignore
git add .gitignore
git commit -m "chore: ignore publish output directory"
```

---

## Self-Review

**Spec coverage check:**
- Config loading + validation ✅ Task 5
- IDataService interface + DummyDataService ✅ Task 6
- records.json with 20 representative records ✅ Task 6
- FileNamingHelper — padding, prefix strip, sanitize, build ✅ Task 3
- LogHelper — timestamped file logging ✅ Task 4
- FolderService — core loop, idempotent dirs, sequence tracking ✅ Task 7
- Error handling — skipped files list, app continues ✅ Task 7
- ReportService — summary file ✅ Task 8
- Log file in BaseFolder ✅ Task 4 + Task 9
- Summary in run folder ✅ Task 8 + Task 9
- Console progress per employee ✅ Task 7
- Self-contained single .exe ✅ Task 10
- 3-level deep path support ✅ Task 7 (test included)
- FolderPath normalises `/` and `\` ✅ FolderService.BuildFullPath
