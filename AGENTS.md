# CoQ-Japanese_v2

## Status

**Observation mode (frozen, 2026-04-07〜).** Production code is frozen. Active development has moved back to v1 (`ToaruPen/Caves-of-Qud_Japanese`, CoQ stable 1.0 / v2.0.4). This repo tracks the CoQ `lang-experimental` branch (build 212.x) for a future thaw. The normative source for this decision and re-evaluation triggers is [`docs/decisions/0001-v1-v2-roles.md`](docs/decisions/0001-v1-v2-roles.md). The dated evidence that triggered the freeze is [`docs/snapshots/2026-04-07-beta-l10n-status.md`](docs/snapshots/2026-04-07-beta-l10n-status.md).

Do not add new production code, new translation assets, or new runtime patches while in observation mode. Permitted work: observation tooling (`scripts/inventory_beta_strings.py` / `scripts/diff_beta_strings.py`), decision/snapshot doc maintenance, test hygiene for existing code.

## Why

Originally a green-field Japanese localization workspace targeting the Caves of Qud `lang-experimental` localization pipeline. Frozen after the 2026-04-07 investigation showed the new framework is still early alpha and the load-bearing API in the experimental branch is still the legacy `Grammar.*` + extension-method route.

## What

Scoped guides for each area:
- `Mods/QudJP/Assemblies/AGENTS.md` — C# runtime code and tests
- `Mods/QudJP/Localization/AGENTS.md` — XML, dictionaries, and corpus assets
- `scripts/AGENTS.md` — Python and shell tooling

## How

While frozen: maintain observation tooling and documentation only.

Original beta-first route priority (retained for future thaw): `Strings/_T/_S` > `ExampleLanguage` > `TextConstants` > `GameText/ReplaceBuilder` > `[LanguageProvider]` > Harmony patches (last resort).

## Source of truth

| Document | Scope |
|----------|-------|
| `docs/decisions/0001-v1-v2-roles.md` | Normative v1/v2 role split and re-evaluation triggers (frozen-state decisions) |
| `docs/snapshots/2026-04-07-beta-l10n-status.md` | Dated evidence underlying the freeze (not updated) |
| `docs/RULES.md` | Workflow conventions, TDD policy, asset rules, code standards |
| `docs/TOOLS.md` | Build/test/validation commands, CI gates, external paths |
| `docs/test-architecture.md` | Test layer definitions (L1/L2/L2G) |
| `README.md` | Semi-stateless project overview mirrored with v1 |
