using DotnetDiffCoverage.Commands;
using FluentAssertions;
using System.CommandLine;
using Xunit;

namespace DotnetDiffCoverage.Tests;

public class CliSmokeTests
{
    private readonly RootCommand _rootCommand = RootCommandBuilder.Build();

    [Fact]
    public void RootCommand_HasDescription()
    {
        _rootCommand.Description.Should().NotBeNullOrWhiteSpace();
    }

    [Theory]
    [InlineData("diff")]
    [InlineData("coverage")]
    [InlineData("coverage-format")]
    [InlineData("coverage-path-prefix")]
    [InlineData("output-json")]
    [InlineData("output-sarif")]
    [InlineData("threshold")]
    [InlineData("config")]
    [InlineData("no-color")]
    public void RootCommand_RegistersOption(string optionName)
    {
        var option = _rootCommand.Options.FirstOrDefault(o => o.Name == optionName);
        option.Should().NotBeNull(because: $"option '--{optionName}' must be registered on the root command");
    }

    [Fact]
    public async Task RootCommand_HelpFlag_ExitsWithZero()
    {
        var result = await _rootCommand.InvokeAsync("--help");
        result.Should().Be(0, because: "--help should always succeed with exit code 0");
    }

    [Fact]
    public async Task RootCommand_NoArgs_ExitsWithZero()
    {
        var result = await _rootCommand.InvokeAsync(Array.Empty<string>());
        result.Should().Be(0);
    }
}
