# Localization

## Why

This area contains beta-first Japanese localization assets, including XML overlays, dictionaries, and corpus assets.

## What

- `*.jp.xml` for XML merge overlays
- `Dictionaries/*.ja.json` for dictionary assets
- `Corpus/` for Japanese Markov corpus assets

## How

- Reuse old repo assets broadly, but validate them against the beta schema and token model.
- Prefer localization assets for stable leaf text, template text, and structured beta XML routes.
- Preserve placeholders and markup exactly, including `{{...}}`, `=token=`, `&X`, and `^x`.
- Validate assets with:

```bash
xmllint --noout <file>
file <file>
hexdump -C <file> | head -1
```
