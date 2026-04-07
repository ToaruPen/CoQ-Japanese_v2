# CoQ-Japanese_v2

> **Status (2026-04-07〜)**: Observation mode (frozen)
>
> **対象ゲームライン**: Caves of Qud experimental branch (`lang-experimental`, build 212.x 系)
>
> 主開発リポジトリは v1 の [`ToaruPen/Caves-of-Qud_Japanese`](https://github.com/ToaruPen/Caves-of-Qud_Japanese) (stable 1.0 / v2.0.4 対応) に復帰しました。役割分担と再評価条件は [`docs/decisions/0001-v1-v2-roles.md`](docs/decisions/0001-v1-v2-roles.md) を参照。

## Why

Caves of Qud の `lang-experimental` ブランチで開発が進んでいる新ローカライゼーションフレームワーク (`Strings/_T/_S` / `[LanguageProvider]` / `ExampleLanguage` / `TextBuilder` / `GameText/ReplaceBuilder`) に最適化された日本語化 Mod を、green-field で再設計するために作成された。

ただし experimental ブランチの実装は load-bearing 層が依然として旧 API (`Grammar.*` + `.t()` / `.Does()` / `.an()` 等) に依存し、新フレームワーク自体は Freehold Games 自身が "early alpha" と公式自認している段階。詳細な一次調査結果は [`docs/snapshots/2026-04-07-beta-l10n-status.md`](docs/snapshots/2026-04-07-beta-l10n-status.md)、観測モード復帰の意思決定と再評価トリガは [`docs/decisions/0001-v1-v2-roles.md`](docs/decisions/0001-v1-v2-roles.md)。

## What This Repo Is For

- experimental ブランチ (`lang-experimental`) の進展追跡 / 観測
- 再評価トリガ自動監視ツールの保守 (`scripts/inventory_beta_strings.py` / `scripts/diff_beta_strings.py`)
- decision record と dated investigation snapshot の保管
- 将来 experimental が成熟段階に到達した際の green-field 復帰準備

## What It Is Not For

- stable 1.0 ユーザー向けの翻訳出荷 (→ v1)
- experimental ターゲットの新規 production code 追加 (観測モード中)
- v1 から path-dependent な runtime patch のミラーリング
- 当初の green-field 計画の前倒し復活

## Docs

| ドキュメント | 役割 |
|---|---|
| [`docs/decisions/0001-v1-v2-roles.md`](docs/decisions/0001-v1-v2-roles.md) | v1/v2 役割分担と再評価トリガの normative 定義 |
| [`docs/snapshots/2026-04-07-beta-l10n-status.md`](docs/snapshots/2026-04-07-beta-l10n-status.md) | 凍結判断の根拠となった一次調査結果 (dated, 不更新) |
| [`docs/RULES.md`](docs/RULES.md) | ワークフロー / route 優先順 / TDD / asset 規約 / コード規約 |
| [`docs/TOOLS.md`](docs/TOOLS.md) | ビルド / テスト / 検証コマンド / CI ゲート / 外部パス |
| [`docs/test-architecture.md`](docs/test-architecture.md) | L1 / L2 / L2G テスト層定義 |
| [`docs/audit-bypass-routes.md`](docs/audit-bypass-routes.md) | localization bypass route 監査 |
| [`docs/contributing.md`](docs/contributing.md) | 貢献ガイド |
| [`docs/deployment.md`](docs/deployment.md) | デプロイ手順 |
| [`docs/font-requirements.md`](docs/font-requirements.md) | フォント要件 |

## Development / Verification

- ビルド / テスト / 検証コマンドはすべて [`docs/TOOLS.md`](docs/TOOLS.md) に集約
- C# テストは NUnit、L1 / L2 / L2G の 3 層構成 ([`docs/test-architecture.md`](docs/test-architecture.md))
- Python ツールは pytest + Ruff + ast-grep
- 静的解析: `QudJP.Analyzers` (Roslyn analyzer suite, CI 実行)
- 観測モード中は新規機能追加を避け、観測ツールと decision/snapshot ドキュメントの保守に限定

## Related Repo

- [`ToaruPen/Caves-of-Qud_Japanese`](https://github.com/ToaruPen/Caves-of-Qud_Japanese) — **v1, 主開発, Caves of Qud stable 1.0 (v2.0.4) 対応の active mainline**

## License

TBD
