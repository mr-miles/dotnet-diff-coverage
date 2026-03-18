using DotnetDiffCoverage.Parsing;

namespace DotnetDiffCoverage.Analysis;

/// <summary>
/// Intersects a <see cref="DiffResult"/> with a <see cref="CoverageResult"/> to produce
/// a <see cref="CrossReferenceResult"/> listing uncovered diff lines per file with aggregate statistics.
/// </summary>
public sealed class CrossReferenceEngine
{
    /// <summary>
    /// Analyzes which lines added in the diff are not covered by the provided coverage data.
    /// </summary>
    /// <param name="diff">Parsed diff result.</param>
    /// <param name="coverage">Parsed coverage result.</param>
    /// <param name="coveragePathPrefix">
    /// Optional prefix to strip from coverage file paths before matching against diff paths.
    /// Use this when coverage paths are absolute (e.g., <c>/home/ci/repo/</c>) and diff paths are relative
    /// (e.g., <c>src/Foo.cs</c>). Pass <c>null</c> when paths already match exactly.
    /// </param>
    public CrossReferenceResult Analyze(DiffResult diff, CoverageResult coverage, string? coveragePathPrefix = null)
    {
        if (diff.FileAddedLines.Count == 0)
            return CrossReferenceResult.Empty;

        var files = new List<UncoveredFile>(diff.FileAddedLines.Count);

        foreach (var (diffPath, addedLines) in diff.FileAddedLines)
        {
            var coveredLines = FindCoverageMatch(diffPath, coverage, coveragePathPrefix);

            IReadOnlyList<int> uncoveredLines = coveredLines is null
                ? addedLines
                : addedLines.Where(l => !coveredLines.Contains(l)).ToList();

            files.Add(new UncoveredFile(diffPath, addedLines, uncoveredLines));
        }

        var totalAdded = files.Sum(f => f.AddedLines.Count);
        var totalUncovered = files.Sum(f => f.UncoveredLines.Count);
        var uncoveredPercent = totalAdded == 0
            ? 0.0
            : (double)totalUncovered / totalAdded * 100.0;

        return new CrossReferenceResult(files, totalAdded, totalUncovered, uncoveredPercent);
    }

    /// <summary>
    /// Finds the coverage entry that matches <paramref name="diffPath"/>.
    /// Returns null when no match is found (file treated as fully uncovered).
    /// </summary>
    private static IReadOnlySet<int>? FindCoverageMatch(
        string diffPath, CoverageResult coverage, string? coveragePathPrefix)
    {
        // 1. Exact match (dictionary uses OrdinalIgnoreCase)
        if (coverage.FileCoveredLines.TryGetValue(diffPath, out var exact))
            return exact;

        // 2. Prefix-stripped match: strip the user-supplied prefix from coverage paths,
        //    then compare exactly. Handles: diff "src/Foo.cs" ↔ coverage "/home/ci/repo/src/Foo.cs"
        //    when --coverage-path-prefix "/home/ci/repo/" is provided.
        if (coveragePathPrefix is not null)
        {
            var normalizedPrefix = coveragePathPrefix.Replace('\\', '/');
            if (!normalizedPrefix.EndsWith('/'))
                normalizedPrefix += '/';

            foreach (var (coveragePath, lines) in coverage.FileCoveredLines)
            {
                var normalizedCoverage = coveragePath.Replace('\\', '/');
                if (normalizedCoverage.StartsWith(normalizedPrefix, StringComparison.OrdinalIgnoreCase))
                {
                    var stripped = normalizedCoverage[normalizedPrefix.Length..];
                    if (string.Equals(stripped, diffPath, StringComparison.OrdinalIgnoreCase))
                        return lines;
                }
            }
        }

        return null;
    }
}
