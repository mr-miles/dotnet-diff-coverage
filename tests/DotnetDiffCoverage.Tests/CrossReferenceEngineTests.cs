using DotnetDiffCoverage.Analysis;
using DotnetDiffCoverage.Models;
using DotnetDiffCoverage.Parsing;
using FluentAssertions;
using Xunit;

namespace DotnetDiffCoverage.Tests;

public class CrossReferenceEngineTests
{
    private readonly CrossReferenceEngine _engine = new();

    // ─── Helpers ──────────────────────────────────────────────────────────────

    private static DiffResult MakeDiff(params (string path, int[] lines)[] entries) =>
        new(entries.ToDictionary(
            e => e.path,
            e => (IReadOnlyList<LineRange>)LinesToRanges(e.lines),
            StringComparer.OrdinalIgnoreCase));

    private static List<LineRange> LinesToRanges(int[] lines)
    {
        var sorted = lines.OrderBy(l => l).ToList();
        var ranges = new List<LineRange>();
        int? start = null, prev = null;
        foreach (var l in sorted)
        {
            if (prev == null || l != prev + 1)
            {
                if (start != null) ranges.Add(new LineRange(start.Value, prev!.Value));
                start = l;
            }
            prev = l;
        }
        if (start != null) ranges.Add(new LineRange(start.Value, prev!.Value));
        return ranges;
    }

    private static CoverageResult MakeCoverage(params (string path, int[] lines)[] entries) =>
        new(entries.ToDictionary(
            e => e.path,
            e => (IReadOnlySet<int>)e.lines.ToHashSet(),
            StringComparer.OrdinalIgnoreCase));

    // ─── 1. Empty diff → Empty result ─────────────────────────────────────────

    [Fact]
    public void Analyze_EmptyDiff_ReturnsEmptyResult()
    {
        var result = _engine.Analyze(DiffResult.Empty, MakeCoverage(("src/Foo.cs", [10, 11])));

        result.Files.Should().BeEmpty();
        result.TotalAddedLines.Should().Be(0);
        result.TotalUncoveredLines.Should().Be(0);
        result.UncoveredPercent.Should().Be(0.0);
    }

    // ─── 2. Exact path match — fully covered ──────────────────────────────────

    [Fact]
    public void Analyze_ExactMatch_FullyCovered_ReturnsNoUncoveredLines()
    {
        var diff = MakeDiff(("src/Foo.cs", [10, 11, 12]));
        var coverage = MakeCoverage(("src/Foo.cs", [10, 11, 12]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        var file = result.Files[0];
        file.UncoveredLines.Should().BeEmpty();
        file.UncoveredRanges.Should().BeEmpty();
        result.TotalUncoveredLines.Should().Be(0);
        result.UncoveredPercent.Should().Be(0.0);
    }

    // ─── 3. Exact path match — fully uncovered ────────────────────────────────

    [Fact]
    public void Analyze_ExactMatch_FullyUncovered_ReturnsAllAddedLinesAsUncovered()
    {
        var diff = MakeDiff(("src/Foo.cs", [10, 11, 12]));
        var coverage = MakeCoverage(("src/Foo.cs", [5, 6]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().BeEquivalentTo(new[] { 10, 11, 12 });
        result.Files[0].UncoveredRanges.Should().ContainSingle()
            .Which.Should().Be(new LineRange(10, 12));
        result.TotalUncoveredLines.Should().Be(3);
    }

    // ─── 4. Exact path match — partially covered ──────────────────────────────

    [Fact]
    public void Analyze_ExactMatch_PartiallyCovered_ReturnsOnlyUncoveredLines()
    {
        var diff = MakeDiff(("src/Foo.cs", [10, 11, 12]));
        var coverage = MakeCoverage(("src/Foo.cs", [10, 12]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().ContainSingle()
            .Which.Should().Be(11);
        result.Files[0].UncoveredRanges.Should().ContainSingle()
            .Which.Should().Be(new LineRange(11, 11));
    }

    // ─── 5. --coverage-path-prefix strips absolute prefix for exact match ────────

    [Fact]
    public void Analyze_WithCoveragePathPrefix_StripsPrefix_MatchesCorrectly()
    {
        var diff = MakeDiff(("src/Foo.cs", [5, 6]));
        var coverage = MakeCoverage(("C:/repo/src/Foo.cs", [5, 6]));

        var result = _engine.Analyze(diff, coverage, coveragePathPrefix: "C:/repo/");

        result.Files.Should().ContainSingle();
        result.TotalUncoveredLines.Should().Be(0);
        result.Files[0].UncoveredLines.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_WithCoveragePathPrefix_PrefixWithoutTrailingSlash_StillMatches()
    {
        var diff = MakeDiff(("src/Foo.cs", [1]));
        var coverage = MakeCoverage(("/home/ci/repo/src/Foo.cs", [1]));

        var result = _engine.Analyze(diff, coverage, coveragePathPrefix: "/home/ci/repo");

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().BeEmpty();
    }

    [Fact]
    public void Analyze_WithoutCoveragePathPrefix_AbsoluteCoveragePathDoesNotMatch()
    {
        var diff = MakeDiff(("src/Foo.cs", [5, 6]));
        var coverage = MakeCoverage(("C:/repo/src/Foo.cs", [5, 6]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().BeEquivalentTo(new[] { 5, 6 },
            because: "no prefix was supplied so the coverage path could not be matched");
    }

    // ─── 6. File absent from coverage — all lines uncovered ───────────────────

    [Fact]
    public void Analyze_FileAbsentFromCoverage_AllAddedLinesAreUncovered()
    {
        var diff = MakeDiff(("src/Bar.cs", [1, 2, 3]));
        var coverage = MakeCoverage();

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().BeEquivalentTo(new[] { 1, 2, 3 });
    }

    // ─── 7. All diff files appear in result ───────────────────────────────────

    [Fact]
    public void Analyze_MultipleFiles_AllFilesAppearInResult()
    {
        var diff = MakeDiff(
            ("src/A.cs", [1, 2]),
            ("src/B.cs", [10, 11]));
        var coverage = MakeCoverage(("src/A.cs", [1, 2]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Count.Should().Be(2);

        var fileA = result.Files.Single(f => f.FilePath == "src/A.cs");
        fileA.UncoveredLines.Should().BeEmpty();

        var fileB = result.Files.Single(f => f.FilePath == "src/B.cs");
        fileB.UncoveredLines.Should().BeEquivalentTo(new[] { 10, 11 });
    }

    // ─── 8. Aggregate stats accuracy ──────────────────────────────────────────

    [Fact]
    public void Analyze_AggregateStats_AreCalculatedCorrectly()
    {
        var diff = MakeDiff(
            ("src/A.cs", [1, 2, 3]),
            ("src/B.cs", [10, 11]));
        var coverage = MakeCoverage(("src/A.cs", [1, 3]));

        var result = _engine.Analyze(diff, coverage);

        result.TotalAddedLines.Should().Be(5);
        result.TotalUncoveredLines.Should().Be(3);
        result.UncoveredPercent.Should().BeApproximately(60.0, 0.001);
    }

    // ─── 9. File with no added lines contributes zero to stats ────────────────

    [Fact]
    public void Analyze_FileWithNoAddedLines_ContributesZeroToStats()
    {
        var diff = MakeDiff(("src/Foo.cs", []));
        var coverage = MakeCoverage();

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].AddedLines.Should().BeEmpty();
        result.Files[0].UncoveredLines.Should().BeEmpty();
        result.TotalAddedLines.Should().Be(0);
        result.TotalUncoveredLines.Should().Be(0);
        result.UncoveredPercent.Should().Be(0.0);
    }

    // ─── 10. Case-insensitive path matching ───────────────────────────────────

    [Fact]
    public void Analyze_CaseInsensitivePath_MatchesCoverageCorrectly()
    {
        var diff = MakeDiff(("src/Foo.cs", [5, 6]));
        var coverage = MakeCoverage(("SRC/FOO.CS", [5]));

        var result = _engine.Analyze(diff, coverage);

        result.Files.Should().ContainSingle();
        result.Files[0].UncoveredLines.Should().ContainSingle()
            .Which.Should().Be(6);
    }

    // ─── 11. UncoveredRanges groups non-contiguous uncovered lines ────────────

    [Fact]
    public void Analyze_NonContiguousUncoveredLines_GroupedIntoSeparateRanges()
    {
        // Added: 10,11,12,13,14 — covered: 11,13 — uncovered: 10,12,14 (non-contiguous)
        var diff = MakeDiff(("src/Foo.cs", [10, 11, 12, 13, 14]));
        var coverage = MakeCoverage(("src/Foo.cs", [11, 13]));

        var result = _engine.Analyze(diff, coverage);

        result.Files[0].UncoveredRanges.Should().HaveCount(3);
        result.Files[0].UncoveredRanges.Should().Contain(new LineRange(10, 10));
        result.Files[0].UncoveredRanges.Should().Contain(new LineRange(12, 12));
        result.Files[0].UncoveredRanges.Should().Contain(new LineRange(14, 14));
    }

    // ─── 12. UncoveredRanges merges contiguous uncovered lines ───────────────

    [Fact]
    public void Analyze_ContiguousUncoveredLines_MergedIntoSingleRange()
    {
        // Added: 10,11,12 — covered: none — uncovered: 10-12 as single range
        var diff = MakeDiff(("src/Foo.cs", [10, 11, 12]));
        var coverage = MakeCoverage(("src/Foo.cs", []));

        var result = _engine.Analyze(diff, coverage);

        result.Files[0].UncoveredRanges.Should().ContainSingle()
            .Which.Should().Be(new LineRange(10, 12));
    }
}
