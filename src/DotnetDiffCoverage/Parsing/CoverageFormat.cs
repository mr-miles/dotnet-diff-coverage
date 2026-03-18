namespace DotnetDiffCoverage.Parsing;

/// <summary>
/// Identifies the format of a coverage file.
/// </summary>
public enum CoverageFormat
{
    Unknown,
    Cobertura,
    OpenCover,
    Lcov,
}
