namespace DotnetDiffCoverage.Analysis;

/// <summary>
/// The aggregate result of cross-referencing a diff against coverage data.
/// Contains per-file breakdowns and overall statistics.
/// </summary>
public sealed class CrossReferenceResult
{
    /// <summary>All diff files — including fully-covered files (UncoveredLines is empty for those).</summary>
    public IReadOnlyList<UncoveredFile> Files { get; }

    /// <summary>Total number of lines added across all diff files.</summary>
    public int TotalAddedLines { get; }

    /// <summary>Total number of added lines that have no coverage hit.</summary>
    public int TotalUncoveredLines { get; }

    /// <summary>Percentage of added lines that are uncovered (0.0–100.0). 0.0 when no lines added.</summary>
    public double UncoveredPercent { get; }

    public CrossReferenceResult(
        IReadOnlyList<UncoveredFile> files,
        int totalAddedLines,
        int totalUncoveredLines,
        double uncoveredPercent)
    {
        Files = files;
        TotalAddedLines = totalAddedLines;
        TotalUncoveredLines = totalUncoveredLines;
        UncoveredPercent = uncoveredPercent;
    }

    /// <summary>Empty result — used when the diff has no added lines.</summary>
    public static CrossReferenceResult Empty { get; } =
        new([], 0, 0, 0.0);
}
