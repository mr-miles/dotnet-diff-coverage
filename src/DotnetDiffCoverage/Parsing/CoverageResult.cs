namespace DotnetDiffCoverage.Parsing;

/// <summary>
/// The normalized result of parsing a coverage file.
/// Maps each source file path to the set of line numbers with at least one hit.
/// </summary>
public sealed class CoverageResult
{
    /// <summary>
    /// Source file path (normalized, forward slashes) → set of covered line numbers (hits > 0).
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlySet<int>> FileCoveredLines { get; }

    public CoverageResult(IReadOnlyDictionary<string, IReadOnlySet<int>> fileCoveredLines)
    {
        FileCoveredLines = fileCoveredLines;
    }

    /// <summary>Returns a CoverageResult with no files — used for unknown or empty coverage files.</summary>
    public static CoverageResult Empty { get; } =
        new(new Dictionary<string, IReadOnlySet<int>>());
}
