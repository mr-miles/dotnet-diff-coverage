# Plan 01-02 Summary: CLI Entry Point, Argument Definitions, and DI Host

## Status
Complete

## Files Created / Modified
- `src/DotnetDiffCoverage/Commands/RootCommandBuilder.cs` — Created. Defines all seven CLI options with correct types, descriptions, and a stub SetHandler.
- `src/DotnetDiffCoverage/Services/ServiceRegistration.cs` — Created. Empty `AddDiffCoverageServices` extension method stub.
- `src/DotnetDiffCoverage/Program.cs` — Replaced stub. Builds IHost with DI, invokes root command via `InvokeAsync`.
- `tests/DotnetDiffCoverage.Tests/CliSmokeTests.cs` — Created. 10 tests covering option registration and --help/no-args exit codes.

## Verification Results
- [PASS] `src/DotnetDiffCoverage/Commands/RootCommandBuilder.cs` exists
- [PASS] `RootCommandBuilder.cs` contains references to all seven options: diff, coverage, output-json, output-sarif, threshold, config, no-color
- [PASS] `Program.cs` references `RootCommandBuilder` and calls `InvokeAsync`
- [PASS] `Services/ServiceRegistration.cs` exists with `AddDiffCoverageServices`
- [PASS] `dotnet build --configuration Release --nologo` — Build succeeded, 0 warnings, 0 errors
- [PASS] `dotnet test --configuration Release --nologo` — Passed: 10, Failed: 0, Skipped: 0
- [PASS] `dotnet run --project src/DotnetDiffCoverage -- --help` — All options appear in output

## Test Results
```
Passed!  - Failed: 0, Passed: 10, Skipped: 0, Total: 10, Duration: 433 ms - DotnetDiffCoverage.Tests.dll (net8.0)
```

## --help Output
```
Description:
  Cross-references a code diff with .NET coverage files to surface uncovered lines introduced by a PR.

Usage:
  dotnet-diff-coverage [options]

Options:
  --diff <diff>                  Path to a unified diff (.patch) file. Use '-' to read from stdin.
  --coverage <coverage>          One or more coverage report files (Cobertura XML, OpenCover XML, or LCOV). Auto-detected format.
  --output-json <output-json>    Write JSON coverage-diff report to this file path. Use '-' for stdout.
  --output-sarif <output-sarif>  Write SARIF 2.1.0 report to this file path for use with GitHub/ADO annotations.
  --threshold <threshold>        Maximum allowed percentage of uncovered diff lines before exit code 1 (0-100, default 0). [default: 0]
  --config <config>              Path to a JSON or YAML config file. Defaults to dotnet-diff-coverage.json in the current directory.
  --no-color                     Suppress ANSI color codes in console output.
  --version                      Show version information
  -?, -h, --help                 Show help and usage information
```

## Decisions
- **`using System.CommandLine;` added to Program.cs**: The plan's Program.cs template omitted this using directive, which caused a CS1061 build error because `InvokeAsync` is an extension method in that namespace. Added as the first using statement.
- **`option.Name` without dashes confirmed**: Verified empirically that System.CommandLine beta4 stores option names without leading `--` dashes (e.g., `"diff"` not `"--diff"`). Test assertions use the correct bare-name form.
- **10 tests, not 9**: The 7 `RegistersOption` theory cases + `HasDescription` + `HelpFlag_ExitsWithZero` + `NoArgs_ExitsWithZero` = 10 total, all passing.
