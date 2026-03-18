# Phase 1: Foundation & CLI Scaffolding — Context

## Phase Goal
Establish the solution structure, CLI entry point, DI container, and NuGet tool packaging so all subsequent phases have a runnable, testable host to build on.

## Requirements Covered
- **REQ-01**: CLI entry point — Tool is invokable via `dotnet tool run dotnet-diff-coverage` with documented arguments for all inputs and outputs. Uses `System.CommandLine` with `--diff`, `--coverage`, `--output-json`, `--output-sarif`, `--threshold`, and `--config` options. Must print `--help` with all options documented.
- **REQ-02**: NuGet packaging — Project is packaged as a .NET Tool publishable to NuGet.org. `.csproj` must include `PackAsTool=true`, `ToolCommandName=dotnet-diff-coverage`, `PackageId`, `Version`, `Description`, `Authors`. Must produce a `.nupkg` on `dotnet pack`.

## What Already Exists (from prior phases)
Nothing — this is Phase 1 and the repository is empty. All files are net-new.

## Key Design Decisions
- **Solution layout**: `src/DotnetDiffCoverage/` for the CLI tool project, `tests/DotnetDiffCoverage.Tests/` for the test project. A `Directory.Build.props` at the root sets shared properties (nullable enable, implicit usings, target framework).
- **CLI framework**: `System.CommandLine` (Microsoft's official CLI library) — matches the .NET 8+ constraint and integrates naturally with `IHost`.
- **DI host**: `Microsoft.Extensions.Hosting` with `IHost` — makes it easy for later phases to register parser and formatter services.
- **Test framework**: xUnit + FluentAssertions — standard .NET test stack.
- **NuGet tool name**: `dotnet-diff-coverage` (command name matches package ID convention).
- **Architecture proposals**: Skipped by user — they know the approach.
- **Argument stubs**: All CLI arguments from REQ-01 are defined in Phase 1 even if their handlers are no-ops; this establishes the public interface before implementation phases begin.

## Plan Structure
- **Plan 01-01 (Wave 1)**: Solution scaffolding and NuGet tool packaging — Creates `.sln`, `.csproj` files, `Directory.Build.props`, and all NuGet tool metadata. Autonomous execution (no agent needed — pure project file configuration).
- **Plan 01-02 (Wave 2)**: CLI entry point, argument definitions, and DI host — Implements `Program.cs`, root command with all options, IHost registration, and smoke tests. `engineering-senior-developer` + `testing-evidence-collector`.
