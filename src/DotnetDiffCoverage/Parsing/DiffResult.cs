using DotnetDiffCoverage.Models;

namespace DotnetDiffCoverage.Parsing;

/// <summary>
/// The normalized result of parsing a unified diff.
/// Maps each modified file path to the ranges of lines added in the diff.
/// </summary>
public sealed class DiffResult
{
    /// <summary>
    /// File path (normalized, no a/ or b/ prefix) → sorted list of added line ranges.
    /// Files with only removals or context changes have no entry here.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<LineRange>> FileAddedRanges { get; }

    /// <summary>
    /// Flat view: file path → sorted list of added line numbers.
    /// Computed from <see cref="FileAddedRanges"/>. Kept for backwards compatibility.
    /// </summary>
    public IReadOnlyDictionary<string, IReadOnlyList<int>> FileAddedLines =>
        FileAddedRanges.ToDictionary(
            kvp => kvp.Key,
            kvp => (IReadOnlyList<int>)kvp.Value.SelectMany(r => r.Lines).OrderBy(l => l).ToList(),
            StringComparer.OrdinalIgnoreCase);

    public DiffResult(IReadOnlyDictionary<string, IReadOnlyList<LineRange>> fileAddedRanges)
    {
        FileAddedRanges = fileAddedRanges;
    }

    /// <summary>Returns a DiffResult with no files — used for empty or no-op diffs.</summary>
    public static DiffResult Empty { get; } =
        new(new Dictionary<string, IReadOnlyList<LineRange>>());
}
