# Project State

## Current Position
- **Phase**: 1 of 7 (planned)
- **Status**: Phase 1 planned — 2 plans across 2 waves
- **Last Activity**: Phase 1 planning (2026-03-18)

## Progress
```
[░░░░░░░░░░░░░░░░░░░░] 0% — 0/15 plans complete
```

## Recent Decisions
- **Workflow**: Guided — review and confirm each plan before agents execute
- **Distribution**: NuGet dotnet tool (`dotnet tool install`)
- **Tech stack**: C# / .NET 8+, System.CommandLine, no external services
- **Coverage formats**: Cobertura, OpenCover, LCOV — auto-detected from file contents
- **Output formats**: Console summary, JSON report, SARIF 2.1.0
- **PR integration**: GitHub and Azure DevOps PR APIs (tokens via args, config, or env vars)
- **Architecture proposals**: Skipped for Phase 1 — user knows the approach
- **Spec pipeline**: Skipped for Phase 1

## Next Action
Run `/legion:build` to execute Phase 1: Foundation & CLI Scaffolding
