# Phase 4 Context — Cross-Reference Engine

## Phase Goal
Intersect the diff added-lines map with the coverage model to produce uncovered diff lines per file, with aggregate statistics.

## Requirements Covered
- **REQ-07**: Cross-Reference Engine — intersect diff added lines with coverage model → list of uncovered diff lines per file

## Prior Phase Assets Available

### From Phase 2 (Diff Parsing)
- `src/DotnetDiffCoverage/Parsing/DiffResult.cs` — `IReadOnlyDictionary<string, IReadOnlyList<int>> FileAddedLines`
- `src/DotnetDiffCoverage/Parsing/DiffParser.cs` — registered in DI as `AddTransient<DiffParser>()`

### From Phase 3 (Coverage Parsing)
- `src/DotnetDiffCoverage/Parsing/CoverageResult.cs` — `IReadOnlyDictionary<string, IReadOnlySet<int>> FileCoveredLines`
- `src/DotnetDiffCoverage/Parsing/CoverageParser.cs` — registered in DI; `Parse(string filePath, CoverageFormat format)`
- `src/DotnetDiffCoverage/Parsing/Formats/` — three parser implementations registered as `ICoverageFormatParser`

## Algorithm Specification (REQ-07)

```
For each (diffPath, addedLines) in DiffResult.FileAddedLines:
  1. Find matching coverage entry:
     a. Exact key match: coverage.FileCoveredLines[diffPath]
     b. Suffix match: find coverage path where one ends with the other (OrdinalIgnoreCase)
        - Handles: diff "src/Foo.cs" ↔ coverage "C:/repo/src/Foo.cs"
        - If multiple suffix candidates, prefer longest common suffix (most specific)
     c. No match: treat all added lines as uncovered
  2. Collect uncovered lines: addedLines.Where(l => !coveredLines.Contains(l))
  3. Include ALL diff files in result (even fully covered ones — Phase 5 needs file counts)

Aggregate stats:
  totalAddedLines    = sum of AddedLines.Count across all UncoveredFile entries
  totalUncoveredLines = sum of UncoveredLines.Count across all UncoveredFile entries
  uncoveredPercent    = totalAdded == 0 ? 0.0 : (double)totalUncovered / totalAdded * 100.0
```

## Output Models Designed for This Phase

```csharp
// Per-file cross-reference result
public sealed record UncoveredFile(
    string FilePath,
    IReadOnlyList<int> AddedLines,
    IReadOnlyList<int> UncoveredLines);

// Aggregate result (consumed by Phase 5 output formatters)
public sealed class CrossReferenceResult
{
    public IReadOnlyList<UncoveredFile> Files { get; }
    public int TotalAddedLines { get; }
    public int TotalUncoveredLines { get; }
    public double UncoveredPercent { get; }
    public static CrossReferenceResult Empty { get; }  // zero-files sentinel
}
```

## Decisions
- Architecture proposals: skipped (single requirement, pure algorithm)
- Spec pipeline: skipped (algorithm is fully specified in REQ-07)
- `UncoveredFile` as record (structural equality useful in tests)
- `CrossReferenceResult` as sealed class (has static Empty and non-trivial construction)
- Include all diff files in result, not just those with uncovered lines — Phase 5 needs total file count
- `UncoveredLines` is empty list (not null) when file is fully covered

## Plan Structure

| Plan | Wave | Description | Agent |
|------|------|-------------|-------|
| 04-01 | 1 | Models + CrossReferenceEngine + DI | engineering-senior-developer |
| 04-02 | 2 | Unit tests (all REQ-07 scenarios) | engineering-senior-developer |
