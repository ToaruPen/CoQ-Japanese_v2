# Rules

## Operating Assumptions

- This repo targets the beta localization pipeline, not the legacy pre-beta route.
- Static and template-driven localization should move into `Strings`, `ExampleLanguage`, and related XML assets before any Harmony patch is added.
- Runtime patches should be added only for routes that remain outside the beta localization surface.

## Asset Rules

- Preserve markup and placeholders exactly, including `{{...}}`, `&X`, `^x`, `&&`, `^^`, and `=token=` forms.
- Treat old repo assets as migration sources. Port them intentionally instead of bulk-copying the old runtime model.
- Keep decompiled game source, extracted binaries, and Steam files out of git.

## Investigation Rules

- Verify beta behavior against `~/Dev/coq-decompiled-beta-212.17/`.
- Prefer stable producer or template ownership over sink-side translation.
- Separate static/table-driven text from procedural/runtime text when planning coverage.
