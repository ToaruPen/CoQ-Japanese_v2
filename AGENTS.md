# CoQ-Japanese_v2

## Why

This repo is the green-field Japanese localization workspace for the Caves of Qud beta localization pipeline.

## What

- Read the scoped guide for the area you are changing:
  - `Mods/QudJP/Assemblies/AGENTS.md` for C# runtime code and tests
  - `Mods/QudJP/Localization/AGENTS.md` for XML, dictionaries, and corpus assets
  - `scripts/AGENTS.md` for Python and shell tooling
- Source of truth:
  - current direction: `README.md`
  - workflow rules: `docs/RULES.md`
  - architecture boundaries: `docs/test-architecture.md`

## How

- Prefer the beta-first route: `Strings/_T/_S`, `ExampleLanguage`, `TextConstants`, and `GameText/ReplaceBuilder`.
- Treat old repo assets as inputs, not as architecture to preserve.
- Keep runtime patching narrow and route-specific; defer raw string-concatenation fixes until the stable beta routes are covered.
- Core commands:

```bash
dotnet build Mods/QudJP/Assemblies/QudJP.csproj
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj
ruff check scripts/
pytest scripts/tests/
```

- Decompiled beta game source lives in `~/Dev/coq-decompiled_beta/` and must never be committed.
- Do not commit game binaries or Steam installation files.
