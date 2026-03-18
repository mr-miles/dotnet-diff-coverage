---
plan: 03-01
phase: 03-coverage-parsing
status: Complete
wave: 1
agent: engineering-senior-developer
date: 2026-03-18
---

# Plan 03-01 Summary — Coverage Models + Parsers + CLI Option

## Status
Complete — all tasks executed, build succeeded with zero warnings, all 23 existing tests pass.

## Files Created / Modified
- `src/DotnetDiffCoverage/Commands/RootCommandBuilder.cs` — modified (added `--coverage-format` option, updated `--coverage` description)
- `src/DotnetDiffCoverage/Parsing/CoverageFormat.cs` — created (enum: Unknown, Cobertura, OpenCover, Lcov)
- `src/DotnetDiffCoverage/Parsing/CoverageResult.cs` — created (IReadOnlyDictionary<string, IReadOnlySet<int>> FileCoveredLines + Empty static)
- `src/DotnetDiffCoverage/Parsing/ICoverageFormatParser.cs` — created (interface: Parse(string filePath))
- `src/DotnetDiffCoverage/Parsing/CoberturaCoverageParser.cs` — created (XPath-based XML parser, hits > 0)
- `src/DotnetDiffCoverage/Parsing/OpenCoverCoverageParser.cs` — created (XPath-based XML parser, uid→path map, vc > 0)
- `src/DotnetDiffCoverage/Parsing/LcovCoverageParser.cs` — created (line-by-line, SF:/DA: records)
- `src/DotnetDiffCoverage/Parsing/CoverageParser.cs` — created (orchestrator: Parse(string, CoverageFormat), no file-content detection)
- `src/DotnetDiffCoverage/Services/ServiceRegistration.cs` — modified (added AddTransient for all 4 coverage types)

## Verification Results
- All 13 verification commands passed
- `dotnet build --configuration Release`: Build succeeded, 0 warnings, 0 errors
- `dotnet test --configuration Release`: 23 passed, 0 failed, 0 skipped

## Decisions
- No deviations from plan. CoverageFormatDetector is NOT present — format is caller-supplied.
- `--coverage-format` registered as `Option<string?>` (nullable); validation deferred to Phase 5 handler.

## Notes for Plan 03-02 (Tests)
- `CoverageParser.Parse(string filePath, CoverageFormat format)` is the primary test target
- Constructor: `new CoverageParser(new CoberturaCoverageParser(), new OpenCoverCoverageParser(), new LcovCoverageParser())`
- `CoverageFormat.Unknown` → returns `CoverageResult.Empty` (no exception, no file access)
- Cobertura: `filename` attribute on `<class>` elements; `hits` attribute on `<line>` elements
- OpenCover: `File` elements have `uid`+`fullPath`; `SequencePoint` elements have `vc`+`sl`+`fileid`
- LCOV: `SF:path` / `DA:line,hits` / `end_of_record` pattern
- All paths normalized via `Replace('\\', '/')` in each parser
- Fixture files from Phase 2 are already in output dir via the `Fixtures/**` Content glob in test .csproj — new fixtures will be picked up automatically
