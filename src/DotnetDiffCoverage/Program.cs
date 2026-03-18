using System.CommandLine;
using DotnetDiffCoverage.Commands;
using DotnetDiffCoverage.Services;
using Microsoft.Extensions.DependencyInjection;
using Microsoft.Extensions.Hosting;
using Microsoft.Extensions.Logging;

// Build the DI host so services are available to command handlers.
var host = Host.CreateDefaultBuilder(args)
    .ConfigureLogging(logging =>
    {
        logging.SetMinimumLevel(LogLevel.Warning);
    })
    .ConfigureServices((_, services) =>
    {
        services.AddDiffCoverageServices();
    })
    .Build();

// Build the CLI root command and invoke it.
var rootCommand = RootCommandBuilder.Build();
return await rootCommand.InvokeAsync(args);
