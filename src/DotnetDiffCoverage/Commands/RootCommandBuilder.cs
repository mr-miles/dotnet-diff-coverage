using System.CommandLine;

namespace DotnetDiffCoverage.Commands;

public static class RootCommandBuilder
{
    public static RootCommand Build()
    {
        var rootCommand = new RootCommand(
            "Cross-references a code diff with .NET coverage files to surface uncovered lines introduced by a PR.");

        // --diff: path to a unified diff file, or '-' for stdin
        var diffOption = new Option<FileInfo?>(
            name: "--diff",
            description: "Path to a unified diff (.patch) file. Use '-' to read from stdin.")
        {
            IsRequired = false,
        };

        // --coverage: one or more coverage file paths (Cobertura, OpenCover, or LCOV)
        var coverageOption = new Option<FileInfo[]>(
            name: "--coverage",
            description: "One or more coverage report files (Cobertura XML, OpenCover XML, or LCOV). Auto-detected format.")
        {
            AllowMultipleArgumentsPerToken = false,
            IsRequired = false,
        };
        coverageOption.Arity = ArgumentArity.OneOrMore;

        // --output-json: write JSON report to this path (use '-' for stdout)
        var outputJsonOption = new Option<FileInfo?>(
            name: "--output-json",
            description: "Write JSON coverage-diff report to this file path. Use '-' for stdout.");

        // --output-sarif: write SARIF 2.1.0 report to this path
        var outputSarifOption = new Option<FileInfo?>(
            name: "--output-sarif",
            description: "Write SARIF 2.1.0 report to this file path for use with GitHub/ADO annotations.");

        // --threshold: maximum allowed uncovered-line percentage (0-100, default 0)
        var thresholdOption = new Option<double>(
            name: "--threshold",
            getDefaultValue: () => 0.0,
            description: "Maximum allowed percentage of uncovered diff lines before exit code 1 (0-100, default 0).");

        // --config: path to dotnet-diff-coverage.json or .yml config file
        var configOption = new Option<FileInfo?>(
            name: "--config",
            description: "Path to a JSON or YAML config file. Defaults to dotnet-diff-coverage.json in the current directory.");

        // --no-color: suppress ANSI color codes in console output
        var noColorOption = new Option<bool>(
            name: "--no-color",
            description: "Suppress ANSI color codes in console output.");

        rootCommand.AddOption(diffOption);
        rootCommand.AddOption(coverageOption);
        rootCommand.AddOption(outputJsonOption);
        rootCommand.AddOption(outputSarifOption);
        rootCommand.AddOption(thresholdOption);
        rootCommand.AddOption(configOption);
        rootCommand.AddOption(noColorOption);

        // Stub handler — returns 0. Real handler added in Phase 4/5.
        rootCommand.SetHandler(() =>
        {
            Console.WriteLine("dotnet-diff-coverage: no inputs provided. Run --help for usage.");
        });

        return rootCommand;
    }
}
