namespace DotnetDiffCoverage.Parsing;

/// <summary>
/// Parses a coverage file of a specific known format into a normalized CoverageResult.
/// </summary>
public interface ICoverageFormatParser
{
    CoverageResult Parse(string filePath);
}
