# CoQ-Japanese_v2

Japanese localization mod workspace for the Caves of Qud beta localization pipeline.

## Status

This repository starts from a green-field layout and explicitly targets the beta-branch localization model built around `Strings/_T/_S`, `ExampleLanguage`, `TextConstants`, and `GameText/ReplaceBuilder`.

It is a migration away from the legacy `Caves-of-Qud_Japanese` implementation. Old localization assets, the Japanese Markov corpus, and validation tooling are expected to be reused selectively, but the runtime `Assemblies` layer is being redesigned for the beta pipeline.

## Repository Goals

- build a beta-first Japanese localization mod structure
- port stable localization assets from the old repo
- validate new `Strings` and `ExampleLanguage` routes first
- keep procedural and runtime patching minimal and route-specific

## Planned Layout

- `Mods/QudJP/Assemblies/` for the beta runtime DLL and tests
- `Mods/QudJP/Localization/` for XML, dictionaries, and corpus assets
- `scripts/` for validation, extraction, sync, and migration tooling
- `docs/` for rules, architecture notes, and migration decisions

## Current Direction

1. Create the beta-first repo skeleton.
2. Port selected `Localization`, `Corpus`, and `scripts` assets from the old repo.
3. Establish Japanese base files from `Strings.example.xml` and `TextConstants.xml`.
4. Re-enable Markov through a Japanese corpus swap first.
5. Expand `_T/_S`-driven UI, quest, and conversation coverage.
6. Patch remaining raw string-concatenation routes last.

## Notes

- Decompiled beta game source is expected at `~/Dev/coq-decompiled_beta/` and must never be committed.
- Game binaries such as `Assembly-CSharp.dll` must never be committed.
- This repo may track source, tests, and tooling. Shipped output policy can differ from the old repo.
