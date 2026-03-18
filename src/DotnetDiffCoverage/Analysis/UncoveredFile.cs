namespace DotnetDiffCoverage.Analysis;

/// <summary>
/// Per-file result of cross-referencing a diff against coverage data.
/// </summary>
/// <param name="FilePath">Normalized file path from the diff (no a/ or b/ prefix).</param>
/// <param name="AddedLines">All line numbers added in the diff for this file.</param>
/// <param name="UncoveredLines">Subset of AddedLines that have no coverage hit. Empty when fully covered.</param>
public sealed record UncoveredFile(
    string FilePath,
    IReadOnlyList<int> AddedLines,
    IReadOnlyList<int> UncoveredLines);
