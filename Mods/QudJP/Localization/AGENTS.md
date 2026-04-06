# Localization

## Why

Beta-first Japanese localization assets: XML overlays, dictionaries, and corpus assets.

## What

- `Languages.xml` — language registration (`"ja"` with Source Han Mono font, the unsuffixed = JP primary variant per Adobe naming)
- `Strings.ja.xml` — Japanese string table entries
- `*.jp.xml` — XML merge overlays for game documents
- `Dictionaries/*.ja.json` — dictionary assets (v1 legacy, triage needed)
- `Corpus/` — Japanese Markov corpus assets

## How

- Reuse old repo assets selectively; validate against beta schema before committing.
- Preserve placeholders and markup exactly (see `docs/RULES.md`).
- Validation commands: see `docs/TOOLS.md`.
