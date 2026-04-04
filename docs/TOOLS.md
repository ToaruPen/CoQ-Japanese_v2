# Tools

Commands, validation gates, and external resources for the localization workspace.

## C# (Assemblies)

```bash
# Build
dotnet build Mods/QudJP/Assemblies/QudJP.csproj --configuration Release
dotnet build Mods/QudJP/Assemblies/QudJP.sln --configuration Release

# Test — all layers
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --configuration Release

# Test — by layer
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L1
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2
dotnet test Mods/QudJP/Assemblies/QudJP.Tests/QudJP.Tests.csproj --filter TestCategory=L2G

# Analyzer tests
dotnet test Mods/QudJP/Assemblies/QudJP.Analyzers.Tests/QudJP.Analyzers.Tests.csproj --configuration Release
```

### Test layers

| Layer | Scope | Game DLL | Category filter |
|-------|-------|----------|-----------------|
| L1 | Pure logic, normalization, helpers | No | `TestCategory=L1` |
| L2 | Integration, repo-owned runtime code | No | `TestCategory=L2` |
| L2G | Game-DLL-aware, signature resolution | Yes (`HAS_GAME_DLL`) | `TestCategory=L2G` |

See `docs/test-architecture.md` for boundary definitions.

### Custom analyzers

| ID | Rule | Enforces |
|----|------|----------|
| QJ001 | Harmony patch body must have try-catch | Crash safety |
| QJ002 | Fallback value must be preceded by logging | Observability |
| QJ003 | Trace log must include `QudJP:` prefix | Log identification |

## Python (Scripts)

```bash
# Lint
ruff check scripts/
ruff format --check scripts/

# Format
ruff format scripts/

# Test
pytest scripts/tests/
pytest scripts/tests/ -k <pattern>
```

### Key scripts

| Script | Purpose |
|--------|---------|
| `scripts/inventory_beta_strings.py` | Extract Context/ID pairs from ExampleLanguage, inventory [LanguageXml] roots |
| `scripts/validate_assets.py` | UTF-8/BOM, XML well-formedness, placeholder preservation, overlay comparison |

## XML validation

```bash
# Well-formedness
python3 -c "import xml.etree.ElementTree as ET; ET.parse('<file>')"

# Asset validation (preferred)
python scripts/validate_assets.py <file_or_dir> [--overlay-source <ExampleLanguage_dir>]

# Schema inventory
python scripts/inventory_beta_strings.py <ExampleLanguage_dir> [--format json|csv|summary]
```

## External references

| Resource | Path | Notes |
|----------|------|-------|
| Decompiled beta source | `~/Dev/coq-decompiled-beta-212.17/` | Read-only reference. Never commit. |
| Old v1 repo | `~/Dev/coq-japanese-v1-old/` | Migration source. Not architecture to preserve. |
| Game DLLs (Steam) | `~/Library/Application Support/Steam/steamapps/common/Caves of Qud/CoQ.app/Contents/Resources/Data/Managed/` | macOS default path. Override with `-p:GameDir=...` |
| Rosetta launcher | `./Launch CavesOfQud (Rosetta).command` | Apple Silicon workaround for Harmony W^X |

## CI gates

CI runs on push/PR to `main`. Both jobs must pass:

- **beta_quality**: solution build, .NET tests, analyzer tests
- **python_quality**: ruff lint, pytest
