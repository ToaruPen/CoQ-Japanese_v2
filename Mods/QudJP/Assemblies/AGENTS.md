# Assemblies

## Why

This area contains the beta runtime DLL and tests for code that still must exist outside the official localization asset pipeline.

## What

- `QudJP.csproj` for the runtime assembly
- `QudJP.Tests/` for automated tests
- `src/Patches/` for route-specific Harmony patches
- `src/` for shared translators, builders, and helpers

## How

- Assume redesign by default. Old Harmony patches are reference material, not port targets.
- Prefer beta-native surfaces first: `Strings/_T/_S`, `GameText`, `ReplaceBuilder`, `VariableReplacers`, and XML assets.
- Add Harmony only for uncovered routes such as raw string concatenation or direct TMP assignment.
- Build and test with:

```bash
dotnet build Mods/QudJP/Assemblies/QudJP.csproj
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2G
```
