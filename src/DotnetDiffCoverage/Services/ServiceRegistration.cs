using DotnetDiffCoverage.Parsing;
using Microsoft.Extensions.DependencyInjection;

namespace DotnetDiffCoverage.Services;

public static class ServiceRegistration
{
    public static IServiceCollection AddDiffCoverageServices(this IServiceCollection services)
    {
        // Diff parsing
        services.AddTransient<DiffParser>();

        // Coverage parsing
        services.AddTransient<CoberturaCoverageParser>();
        services.AddTransient<OpenCoverCoverageParser>();
        services.AddTransient<LcovCoverageParser>();
        services.AddTransient<CoverageParser>();

        // Cross-reference engine, output formatters, and API clients will be registered in later phases.
        return services;
    }
}
