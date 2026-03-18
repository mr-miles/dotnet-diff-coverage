# Codebase Map — dotnet-diff-coverage

**Analyzed**: 2026-03-18
**SDK**: .NET 8 / SDK 10.0.104
**Language**: C# (`LangVersion=latest`, `Nullable=enable`, `ImplicitUsings=enable`, `TreatWarningsAsErrors=true`)
**Type**: NuGet dotnet global tool (`PackAsTool=true`, command `dotnet-diff-coverage`)

---

## Directory Structure

```
tool/
├── src/
│   └── DotnetDiffCoverage/          ← main executable project
│       ├── Program.cs               ← entry point: DI host + CLI invocation
│       ├── Commands/
│       │   └── RootCommandBuilder.cs
│       ├── Parsing/
│       │   ├── CoverageFormat.cs
│       │   ├── CoverageParser.cs
│       │   ├── CoverageResult.cs
│       │   ├── DiffFile.cs          (internal)
│       │   ├── DiffParser.cs
│       │   ├── DiffResult.cs
│       │   ├── ICoverageFormatParser.cs
│       │   └── Formats/
│       │       ├── CoberturaCoverageParser.cs
│       │       ├── LcovCoverageParser.cs
│       │       └── OpenCoverCoverageParser.cs
│       ├── Analysis/
│       │   ├── CrossReferenceEngine.cs
│       │   ├── CrossReferenceResult.cs
│       │   └── UncoveredFile.cs
│       └── Services/
│           └── ServiceRegistration.cs
├── tests/
│   └── DotnetDiffCoverage.Tests/
│       ├── CliSmokeTests.cs
│       ├── CoverageParserTests.cs
│       ├── CrossReferenceEngineTests.cs
│       ├── DiffParserTests.cs
│       ├── IntegrationTests.cs
│       └── Fixtures/
│           ├── binary.patch
│           ├── empty.patch
│           ├── rename.patch
│           ├── simple-multi-file.patch
│           ├── sample-cobertura.xml
│           ├── sample-lcov.info
│           └── sample-opencover.xml
├── .planning/                       ← Legion planning artefacts
├── Directory.Build.props            ← shared: TargetFramework, Nullable, TreatWarningsAsErrors
├── .editorconfig                    ← C# style rules
└── README.md
```

---

## Namespaces

| Namespace | Contents |
|-----------|----------|
| `DotnetDiffCoverage` | Entry point (`Program.cs`) |
| `DotnetDiffCoverage.Commands` | CLI option/command definitions |
| `DotnetDiffCoverage.Parsing` | Diff and coverage parsing, models, format enum |
| `DotnetDiffCoverage.Parsing.Formats` | Per-format parser implementations |
| `DotnetDiffCoverage.Analysis` | Cross-reference engine and output models |
| `DotnetDiffCoverage.Services` | DI registration extension method |

---

## Data Flow

```
CLI args
    │
    ▼
RootCommandBuilder        ← System.CommandLine beta4
    │                        option.Name stores names WITHOUT leading dashes
    │
    ▼
Program.cs (IHost)
    │  DI resolves:
    ├─► DiffParser
    ├─► CoverageParser  ←── IEnumerable<ICoverageFormatParser> injected
    └─► CrossReferenceEngine
              │
              ▼
     DiffResult  ──────────┐
     CoverageResult  ───────┤
                            ▼
                  CrossReferenceEngine.Analyze()
                            │
                            ▼
                  CrossReferenceResult
                     ├── Files: IReadOnlyList<UncoveredFile>
                     ├── TotalAddedLines
                     ├── TotalUncoveredLines
                     └── UncoveredPercent
```

---

## Key Types

### Models — Parsing layer

| Type | Kind | Key property |
|------|------|-------------|
| `DiffResult` | sealed class | `IReadOnlyDictionary<string, IReadOnlyList<int>> FileAddedLines` — path → sorted added line numbers; `OrdinalIgnoreCase` keys |
| `CoverageResult` | sealed class | `IReadOnlyDictionary<string, IReadOnlySet<int>> FileCoveredLines` — path → set of covered lines (hits > 0); `OrdinalIgnoreCase` keys |
| `CoverageFormat` | enum | `Unknown`, `Cobertura`, `OpenCover`, `Lcov` |
| `DiffFile` | internal sealed class | Mutable accumulator used only inside `DiffParser`; not exposed |

### Models — Analysis layer

| Type | Kind | Notes |
|------|------|-------|
| `UncoveredFile` | sealed record | `(FilePath, AddedLines, UncoveredLines)` — `UncoveredLines` is empty list (never null) when fully covered |
| `CrossReferenceResult` | sealed class | Aggregate; `static Empty` sentinel |

### Services

| Type | Responsibility |
|------|---------------|
| `DiffParser` | Parses unified diff text → `DiffResult`. Strips `a/`/`b/` prefixes; handles binary markers, renames, new files (`--- /dev/null`), deleted files (`+++ /dev/null`). Blank lines (empty string after trim) in hunks are **ignored** — they do NOT advance the line counter. |
| `ICoverageFormatParser` | Interface: `CoverageFormat Format { get; }` + `CoverageResult Parse(string filePath)` |
| `CoberturaCoverageParser` | XPath `//class[@filename]` → `lines/line[@hits > 0]`. Normalizes `\` → `/`. |
| `OpenCoverCoverageParser` | XPath `//File[@uid]` builds uid→path map; XPath `//SequencePoint[@vc > 0]` collects covered lines. Normalizes `\` → `/`. |
| `LcovCoverageParser` | Line-by-line: `SF:` sets current file, `DA:<line>,<hits>` adds covered line when hits > 0, `end_of_record` resets. Normalizes `\` → `/`. |
| `CoverageParser` | Orchestrator. Receives `IEnumerable<ICoverageFormatParser>` via DI, builds `Dictionary<CoverageFormat, ICoverageFormatParser>` for O(1) dispatch. Returns `CoverageResult.Empty` for `Unknown` format. |
| `CrossReferenceEngine` | `Analyze(DiffResult, CoverageResult, string? coveragePathPrefix = null)`. Exact match first; then prefix-stripped exact match if prefix supplied. **No fuzzy suffix matching.** All diff files appear in result, including fully covered ones. |
| `RootCommandBuilder` | Constructs `RootCommand` with all options. Stub handler (Phase 5 adds real handler). |
| `ServiceRegistration` | `AddDiffCoverageServices(IServiceCollection)` extension. Registers parsers as `ICoverageFormatParser` (interface) so DI injects them as a collection into `CoverageParser`. |

---

## DI Registration

```csharp
// Each parser registered as the interface — DI collects all into IEnumerable<ICoverageFormatParser>
services.AddTransient<ICoverageFormatParser, CoberturaCoverageParser>();
services.AddTransient<ICoverageFormatParser, OpenCoverCoverageParser>();
services.AddTransient<ICoverageFormatParser, LcovCoverageParser>();
services.AddTransient<CoverageParser>();          // receives IEnumerable<ICoverageFormatParser>
services.AddTransient<DiffParser>();
services.AddTransient<CrossReferenceEngine>();
// Output formatters and PR API clients: not yet registered (Phase 5+)
```

---

## CLI Options (registered in RootCommandBuilder)

| Option name | Type | Notes |
|-------------|------|-------|
| `diff` | `FileInfo?` | Path to `.patch` file; `-` for stdin |
| `coverage` | `FileInfo[]` | One-or-more; `AllowMultipleArgumentsPerToken=false` |
| `coverage-format` | `string?` | Required with `--coverage` |
| `coverage-path-prefix` | `string?` | Strips absolute prefix from coverage paths for exact matching |
| `output-json` | `FileInfo?` | `-` for stdout |
| `output-sarif` | `FileInfo?` | SARIF 2.1.0 |
| `threshold` | `double` | Default `0.0` |
| `config` | `FileInfo?` | JSON or YAML config file |
| `no-color` | `bool` | Suppresses ANSI codes |

> **System.CommandLine note**: `option.Name` returns the name **without** leading dashes (e.g. `"diff"`, not `"--diff"`). This is a beta4 quirk tested in `CliSmokeTests`.

---

## Test Fixtures

| File | Used by | Content |
|------|---------|---------|
| `simple-multi-file.patch` | `DiffParserTests`, `IntegrationTests` | Two-file diff: `src/Calculator.cs` + `src/Program.cs` |
| `rename.patch` | `DiffParserTests` | Rename `src/OldName.cs` → `src/NewName.cs` with additions |
| `binary.patch` | `DiffParserTests` | Mixed binary (`assets/logo.png`) + text (`src/Helper.cs`) |
| `empty.patch` | `DiffParserTests` | Zero-byte file |
| `sample-cobertura.xml` | `CoverageParserTests`, `IntegrationTests` | `src/Calculator.cs` (lines 5,6,7 covered; 8,9 not) + `src/Helper.cs` (lines 3,4 covered; 5 not) |
| `sample-opencover.xml` | `CoverageParserTests` | Absolute Windows paths `C:\src\...`; Calculator lines 10,11,12 covered; 13 not; Helper line 20 covered; 21 not |
| `sample-lcov.info` | `CoverageParserTests`, `IntegrationTests` | Same semantic coverage as Cobertura fixture for Calculator + Helper |

---

## Conventions

- **Empty/null sentinel pattern**: all models have a `static Empty` property. Never use `null` for "no result".
- **Path normalization**: all parsers call `path.Replace('\\', '/')`. DiffParser strips `a/`/`b/` prefixes.
- **Blank hunk lines**: empty strings (trimmed) inside a diff hunk are silently ignored — they do **not** advance the new-file line counter. This affects line number calculations in tests.
- **Immutability**: mutable types (`DiffFile`, `HashSet<int>`, `Dictionary`) are used only during construction; all public APIs expose read-only interfaces.
- **Test helpers**: `CrossReferenceEngineTests` has `MakeDiff` and `MakeCoverage` factory helpers shared-by-convention (they're private to that class). `IntegrationTests` has `ParseCobertura`, `ParseLcov`, `ParseOpenCover` helpers writing temp files.
- **Fixture access**: tests use `AppContext.BaseDirectory` + `"Fixtures"` to locate fixture files.

---

## Risk Areas

| Area | Risk | Notes |
|------|------|-------|
| `DiffParser` blank-line handling | Correctness | Truly empty lines in hunks are ignored (not treated as context). Real git diffs typically use a space character for blank context lines, but test fixtures and raw string literals may produce empty strings instead. This shifts all subsequent line numbers. |
| `CoverageParser` format dispatch | Silent failure | Unknown/misspelled `--coverage-format` returns `CoverageResult.Empty` with no exception or warning. Phase 5 should add a user-facing error for unknown formats. |
| `CrossReferenceEngine` path matching | Coverage gap | Only exact match and prefix-stripped exact match are implemented. No fuzzy matching by design. Users with non-standard path structures must supply `--coverage-path-prefix`. |
| `RootCommandBuilder` stub handler | Incomplete | `SetHandler` writes a placeholder message and exits 0. Real handler wiring is deferred to Phase 5. |
| Output formatters, PR API clients | Not yet built | Phases 5 and 6 respectively. `ServiceRegistration` has a comment placeholder. |

---

## What Is Not Yet Built (Phases 5–7)

- **Console output formatter** — human-readable summary with ANSI colours
- **JSON report writer** — structured `--output-json` report
- **SARIF 2.1.0 report writer** — for GitHub/ADO inline annotations
- **Exit code logic** — threshold comparison → exit 0/1/2
- **Real command handler** — wires CLI options to DiffParser + CoverageParser + CrossReferenceEngine
- **GitHub PR API client** — fetch diff from a PR URL
- **Azure DevOps PR API client** — fetch diff via ADO REST API
- **Config file support** — auto-discover `dotnet-diff-coverage.json` / `.yml`
- **CI integration artefacts** — GitHub Actions workflow, ADO pipeline example
