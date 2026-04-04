# Scripts

## Why

Migration, validation, extraction, and sync tooling for the beta-first localization workspace.

## What

- `scripts/*.py` — Python utilities
- `scripts/*.sh` — shell tooling (Rosetta launcher)
- `scripts/tests/` — pytest coverage
- `pyproject.toml` — Ruff and pytest configuration

## How

- Prefer extending an existing script over creating overlapping tooling.
- Keep script behavior deterministic and error messages actionable.
- Lint/test commands: see `docs/TOOLS.md`.
