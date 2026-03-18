using DotnetDiffCoverage.Parsing;
using FluentAssertions;
using Xunit;

namespace DotnetDiffCoverage.Tests;

public class DiffParserTests
{
    private readonly DiffParser _parser = new();

    // ─── Empty / null input ───────────────────────────────────────────────────

    [Fact]
    public void Parse_EmptyString_ReturnsEmptyResult()
    {
        var result = _parser.Parse(string.Empty);
        result.FileAddedLines.Should().BeEmpty();
    }

    [Fact]
    public void Parse_WhitespaceOnly_ReturnsEmptyResult()
    {
        var result = _parser.Parse("   \n  \n");
        result.FileAddedLines.Should().BeEmpty();
    }

    // ─── Single file, simple additions ───────────────────────────────────────

    [Fact]
    public void Parse_SingleFile_ExtractsCorrectAddedLines()
    {
        var diff = """
            --- a/src/Foo.cs
            +++ b/src/Foo.cs
            @@ -1,4 +1,6 @@
             namespace MyApp;

            +public class Foo
            +{
             }
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().ContainKey("src/Foo.cs");
        result.FileAddedLines["src/Foo.cs"].Should().BeEquivalentTo(new[] { 2, 3 });
    }

    // ─── Path normalization (strip a/ and b/ prefixes) ───────────────────────

    [Fact]
    public void Parse_StripsBPrefix_FromFilePath()
    {
        var diff = """
            --- a/src/Bar.cs
            +++ b/src/Bar.cs
            @@ -1,2 +1,3 @@
             line1
            +added line
             line2
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines.Keys.Should().Contain("src/Bar.cs");
        result.FileAddedLines.Keys.Should().NotContain("b/src/Bar.cs");
        result.FileAddedLines.Keys.Should().NotContain("a/src/Bar.cs");
    }

    // ─── Multi-file diff ──────────────────────────────────────────────────────

    [Fact]
    public void Parse_MultiFile_ExtractsAddedLinesPerFile()
    {
        var diff = """
            --- a/src/A.cs
            +++ b/src/A.cs
            @@ -1,3 +1,4 @@
             line1
            +added in A
             line2
             line3
            --- a/src/B.cs
            +++ b/src/B.cs
            @@ -1,2 +1,4 @@
             line1
            +added1 in B
            +added2 in B
             line2
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().HaveCount(2);
        result.FileAddedLines["src/A.cs"].Should().BeEquivalentTo(new[] { 2 });
        result.FileAddedLines["src/B.cs"].Should().BeEquivalentTo(new[] { 2, 3 });
    }

    // ─── Removed lines do not increment new-file line counter ─────────────────

    [Fact]
    public void Parse_RemovedLines_DoNotAdvanceLineCounter()
    {
        var diff = """
            --- a/src/C.cs
            +++ b/src/C.cs
            @@ -1,5 +1,4 @@
             context1
            -removed line
             context2
            +added line
             context3
            """;

        var result = _parser.Parse(diff);

        // new file: line1=context1, line2=context2, line3=added line, line4=context3
        result.FileAddedLines["src/C.cs"].Should().BeEquivalentTo(new[] { 3 });
    }

    // ─── File with only removals — no entry in result ─────────────────────────

    [Fact]
    public void Parse_FileWithOnlyRemovals_NotInResult()
    {
        var diff = """
            --- a/src/D.cs
            +++ b/src/D.cs
            @@ -1,3 +1,2 @@
             line1
            -removed line
             line2
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().NotContainKey("src/D.cs");
    }

    // ─── Binary file — skipped silently ──────────────────────────────────────

    [Fact]
    public void Parse_BinaryFile_SkippedWithNoEntry()
    {
        var diff = """
            diff --git a/logo.png b/logo.png
            Binary files a/logo.png and b/logo.png differ
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().BeEmpty();
    }

    [Fact]
    public void Parse_BinaryAndTextFile_TextFileExtractedBinarySkipped()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "binary.patch");
        var diff = File.ReadAllText(fixturePath);

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().NotContainKey("assets/logo.png",
            because: "binary files must be skipped");
        result.FileAddedLines.Should().ContainKey("src/Helper.cs",
            because: "the normal file in the same diff must still be parsed");
    }

    // ─── Rename ───────────────────────────────────────────────────────────────

    [Fact]
    public void Parse_RenamedFile_UsesNewFilePath()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "rename.patch");
        var diff = File.ReadAllText(fixturePath);

        var result = _parser.Parse(diff);

        // The new name (from +++ b/src/NewName.cs) must be the key
        result.FileAddedLines.Should().ContainKey("src/NewName.cs",
            because: "after a rename, additions are attributed to the new file path");
        result.FileAddedLines.Should().NotContainKey("src/OldName.cs",
            because: "the old file path should not appear in added lines");
    }

    // ─── Multiple hunks in one file ───────────────────────────────────────────

    [Fact]
    public void Parse_MultipleHunks_AllAddedLinesCollected()
    {
        var diff = """
            --- a/src/Multi.cs
            +++ b/src/Multi.cs
            @@ -1,3 +1,4 @@
             line1
            +hunk1 addition
             line2
             line3
            @@ -10,3 +11,4 @@
             line10
            +hunk2 addition
             line11
             line12
            """;

        var result = _parser.Parse(diff);

        result.FileAddedLines["src/Multi.cs"].Should().Contain(2)
            .And.Contain(12);
    }

    // ─── Fixture file: multi-file diff ────────────────────────────────────────

    [Fact]
    public void Parse_SimpleMultiFileFixture_ParsesCorrectly()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "simple-multi-file.patch");
        var diff = File.ReadAllText(fixturePath);

        var result = _parser.Parse(diff);

        result.FileAddedLines.Should().ContainKey("src/Calculator.cs");
        result.FileAddedLines.Should().ContainKey("src/Program.cs");
        result.FileAddedLines["src/Calculator.cs"].Should().NotBeEmpty();
        result.FileAddedLines["src/Program.cs"].Should().NotBeEmpty();
    }

    // ─── Empty fixture file ───────────────────────────────────────────────────

    [Fact]
    public void ParseFile_EmptyFixture_ReturnsEmptyResult()
    {
        var fixturePath = Path.Combine(
            AppContext.BaseDirectory, "Fixtures", "empty.patch");
        var result = _parser.ParseFile(fixturePath);
        result.FileAddedLines.Should().BeEmpty();
    }
}
