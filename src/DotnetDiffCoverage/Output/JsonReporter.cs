using System.Text.Json;
using System.Text.Json.Serialization;
using DotnetDiffCoverage.Analysis;

namespace DotnetDiffCoverage.Output;

public sealed class JsonReporter
{
    private static readonly JsonSerializerOptions Options = new()
    {
        WriteIndented = true,
        PropertyNamingPolicy = JsonNamingPolicy.CamelCase,
        DefaultIgnoreCondition = JsonIgnoreCondition.WhenWritingNull,
    };

    /// <summary>Groups a sorted sequence of line numbers into contiguous ranges.</summary>
    private static IEnumerable<(int Start, int End)> ToRanges(IEnumerable<int> lines)
    {
        int? start = null, prev = null;
        foreach (var line in lines.OrderBy(x => x))
        {
            if (prev == null || line != prev + 1)
            {
                if (start != null) yield return (start.Value, prev!.Value);
                start = line;
            }
            prev = line;
        }
        if (start != null) yield return (start.Value, prev!.Value);
    }

    public async Task WriteAsync(CrossReferenceResult result, Stream stream)
    {
        var report = new
        {
            summary = new
            {
                totalAddedLines = result.TotalAddedLines,
                totalUncoveredLines = result.TotalUncoveredLines,
                uncoveredPercentage = Math.Round(result.UncoveredPercent, 2),
            },
            uncoveredFiles = result.Files
                .Where(f => f.UncoveredLines.Count > 0)
                .Select(f => new
                {
                    path = f.FilePath,
                    uncoveredLines = f.UncoveredLines,
                    uncoveredRanges = ToRanges(f.UncoveredLines)
                        .Select(r => new { start = r.Start, end = r.End })
                        .ToList(),
                }),
        };
        await JsonSerializer.SerializeAsync(stream, report, Options);
    }
}
