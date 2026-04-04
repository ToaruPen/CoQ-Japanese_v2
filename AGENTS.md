# CoQ-Japanese_v2

## Why

Green-field Japanese localization workspace for the Caves of Qud beta localization pipeline.

## What

Scoped guides for each area:
- `Mods/QudJP/Assemblies/AGENTS.md` — C# runtime code and tests
- `Mods/QudJP/Localization/AGENTS.md` — XML, dictionaries, and corpus assets
- `scripts/AGENTS.md` — Python and shell tooling

## How

Beta-first: `Strings/_T/_S` > `ExampleLanguage` > `TextConstants` > `GameText/ReplaceBuilder` > `[LanguageProvider]` > Harmony patches (last resort).

## Source of truth

| Document | Scope |
|----------|-------|
| `docs/RULES.md` | Workflow conventions, TDD policy, asset rules, code standards |
| `docs/TOOLS.md` | Build/test/validation commands, CI gates, external paths |
| `docs/test-architecture.md` | Test layer definitions (L1/L2/L2G) |
| `README.md` | Project direction and planned layout |
