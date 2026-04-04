# Assemblies

## Why

Beta runtime DLL and tests for code that must exist outside the official localization asset pipeline.

## What

- `QudJP.csproj` — runtime assembly (net48)
- `QudJP.Tests/` — automated tests (net10.0, L1/L2/L2G layers)
- `QudJP.Analyzers/` — custom Roslyn analyzers (QJ001–QJ003)
- `src/Patches/` — route-specific Harmony patches
- `src/` — shared translators, builders, and helpers

## How

- Assume redesign by default. Old Harmony patches are reference material, not port targets.
- Prefer beta-native surfaces first (see `docs/RULES.md` for priority order).
- Add Harmony only for uncovered routes such as raw string concatenation or direct TMP assignment.
- Build/test commands: see `docs/TOOLS.md`.
