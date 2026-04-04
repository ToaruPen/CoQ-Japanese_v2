# Test Architecture

This repo will keep test layers aligned with the beta-first implementation model.

## Planned Layers

- `L1`: pure translation, normalization, corpus, and helper logic
- `L2`: integration tests for repo-owned runtime code without direct game-type instantiation
- `L2G`: game-DLL-aware tests for signature resolution and beta route compatibility

## Boundary

- Prefer tests around `Strings`, `ReplaceBuilder`, asset validation, and migration helpers first.
- Add route-specific runtime tests only when a route remains outside the beta localization system.
