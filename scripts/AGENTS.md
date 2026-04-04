# Scripts

## Why

This area contains migration, validation, extraction, and sync tooling for the beta-first localization workspace.

## What

- `scripts/*.py` for Python utilities
- `scripts/*.sh` for shell tooling
- `scripts/tests/` for pytest coverage
- `pyproject.toml` for Ruff and pytest configuration

## How

- Prefer extending an existing script over creating overlapping tooling.
- Keep script behavior deterministic and error messages actionable.
- Main commands:

```bash
ruff check scripts/
ruff format scripts/
pytest scripts/tests/
pytest scripts/tests/ -k <pattern>
```
