# Rules

Workflow conventions and decision criteria for the localization workspace.

## Strategy

- This repo targets the **beta** localization pipeline, not the legacy pre-beta route.
- Prefer beta-native surfaces in this order: `Strings/_T/_S` > `ExampleLanguage` XML > `TextConstants` > `GameText/ReplaceBuilder` > `[LanguageProvider]` / `[HasVariableReplacer]` > Harmony patches.
- Harmony patches are the last resort, only for routes that remain outside all beta extension points.
- Raw string-concatenation patches come after all stable beta routes are covered.

## TDD

- Tests are "生き写し" — faithful reproductions of decompiled beta source behavior.
- Red-green cycle: write failing tests first, then implement to pass.
- Bug fixes require a failing-then-passing test.
- Never delete or disable existing tests.

## Assets

- Preserve markup and placeholders exactly: `{{...}}`, `&X`, `^x`, `&&`, `^^`, `=token=`.
- Treat old repo assets as migration sources. Port intentionally, not by bulk copy.
- Validate ported assets against beta schema before committing (see `docs/TOOLS.md`).
- Keep decompiled game source, extracted binaries, and Steam files out of git.

## Investigation

- Verify beta behavior against the decompiled source before implementing.
- Prefer stable producer or template ownership over sink-side translation.
- Separate static/table-driven text from procedural/runtime text when planning coverage.
- Use the decompiled source as ground truth, not assumptions or documentation.

## Code

- Edit only within ticket/issue scope. No drive-by fixes.
- No unilateral new architecture layers; discuss large refactors first.
- Harmony patches must be wrapped in try-catch (QJ001 enforced).
- Fallback values must be preceded by logging (QJ002 enforced).
- Trace logs must include `QudJP:` prefix (QJ003 enforced).

## Commits and PRs

- Commit only when explicitly requested.
- All PRs go through CI (beta_quality + python_quality) and CodeRabbit review.
- Squash merge to main; delete branch after merge.
- Never force-push to main.

## External resources

- Decompiled beta source: `~/Dev/coq-decompiled-beta-212.17/` (read-only, never commit)
- Old v1 repo: `~/Dev/coq-japanese-v1-old/` (reference material, not architecture to preserve)
- Game installation: macOS Steam default path (see `docs/TOOLS.md` for details)
