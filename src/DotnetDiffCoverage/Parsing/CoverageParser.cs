namespace DotnetDiffCoverage.Parsing;

/// <summary>
/// Orchestrating coverage parser: selects the appropriate format parser based on the
/// caller-supplied <see cref="CoverageFormat"/> and delegates parsing to it.
/// Returns <see cref="CoverageResult.Empty"/> for <see cref="CoverageFormat.Unknown"/>.
/// </summary>
public sealed class CoverageParser
{
    private readonly CoberturaCoverageParser _coberturaParser;
    private readonly OpenCoverCoverageParser _openCoverParser;
    private readonly LcovCoverageParser _lcovParser;

    public CoverageParser(
        CoberturaCoverageParser coberturaParser,
        OpenCoverCoverageParser openCoverParser,
        LcovCoverageParser lcovParser)
    {
        _coberturaParser = coberturaParser;
        _openCoverParser = openCoverParser;
        _lcovParser = lcovParser;
    }

    /// <summary>
    /// Parses the coverage file using the specified format.
    /// Returns <see cref="CoverageResult.Empty"/> if the format is <see cref="CoverageFormat.Unknown"/>.
    /// </summary>
    public CoverageResult Parse(string filePath, CoverageFormat format) =>
        format switch
        {
            CoverageFormat.Cobertura => _coberturaParser.Parse(filePath),
            CoverageFormat.OpenCover => _openCoverParser.Parse(filePath),
            CoverageFormat.Lcov      => _lcovParser.Parse(filePath),
            _                        => CoverageResult.Empty,
        };
}
