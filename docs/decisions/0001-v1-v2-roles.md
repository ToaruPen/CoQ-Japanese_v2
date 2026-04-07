# 0001: v1 / v2 リポジトリの役割分担

- **Status**: Accepted
- **Date**: 2026-04-07
- **Scope**: `ToaruPen/Caves-of-Qud_Japanese` (v1) と `ToaruPen/CoQ-Japanese_v2` (v2) の関係

## Context

Caves of Qud には新旧 2 系統のローカライゼーション API が併存している。

- **旧 API** — `Grammar.Cardinal` / `Grammar.MakeAndList` / `.t()` / `.Does()` / `.an()` 等の拡張メソッドベース。stable 1.0 (v2.0.4) まで一貫して load-bearing 経路。
- **新 API** — `Strings/_T/_S` / `[LanguageProvider]` / `ExampleLanguage` / `TextBuilder` / `GameText/ReplaceBuilder`。`lang-experimental` ブランチ (build 212.x) で試験運用中。

v2 リポジトリは当初、新 API 主軸の green-field 再設計として作成された。Freehold Games が experimental ブランチを「rewrite」と公式発信していたこと、および新フレームワークが翻訳可能性を前提に設計されていたことが根拠であった。

しかし 2026-04-06〜07 のデコンパイル調査 (詳細は `docs/snapshots/2026-04-07-beta-l10n-status.md`) により以下が判明した:

- experimental ブランチの load-bearing 経路は依然として旧 API
- `Translator.Provider` の abstract メンバの一部は dead code (含 `GetCultureInfo`)
- Freehold 自身が公式発言で「early alpha」「experimental alpha of the early stages」と自認し、テスト言語はトルコ語 1 言語のみ
- 主要 UI 画面 (`StatusScreen` / `EquipmentScreen` / `InventoryScreen` / `MissileWeapon` 等) は新 API 未移行

## Decision

### v1 (`ToaruPen/Caves-of-Qud_Japanese`) — 主開発 / Active mainline

- **対象ゲームライン**: Caves of Qud stable 1.0 (v2.0.4)
- **役割**: 日本語化 Mod の出荷リポジトリ。実プレイ可能な品質を維持する。
- **アプローチ**: Harmony patch + 旧 API + Markov コーパス + path-dependent 翻訳資産
- **正当性**: 現在の CoQ 実装の load-bearing 層 (= 旧 API) と整合し、ユーザーが体感できる翻訳カバレッジを最大化できる

### v2 (`ToaruPen/CoQ-Japanese_v2`) — 観測モード / Observation

- **対象ゲームライン**: Caves of Qud experimental branch (`lang-experimental`, build 212.x 系)
- **役割**: experimental ブランチの進展追跡、調査メモ保管、再評価トリガ監視
- **アプローチ**: production code は凍結。観測ツール (`scripts/inventory_beta_strings.py` / `scripts/diff_beta_strings.py`) と decision/snapshot ドキュメントの保守に限定
- **正当性**: 新 API がまだ load-bearing でない以上、production 投資はサンクコスト化する。同時に、experimental が成熟した際に green-field 復帰できる準備は維持する価値がある

## 再評価トリガ (Normative)

以下のいずれかが満たされた時点で、本 decision を破棄して v2 を主軸に戻すことを検討する。

1. Freehold が公式言語パックを 1 つでも出荷する (現時点では内部のトルコ語実験のみ)
2. `lang-experimental` ブランチが `beta` (211.x 系列) または stable に昇格する
3. `Translator.Provider` 経路の load-bearing 比が旧 `Grammar.*` API と拮抗する
4. 主要 UI 画面 (`StatusScreen` / `EquipmentScreen` / `InventoryScreen` / `MissileWeapon` 等) が `_S/_T` 化される
5. `Translator.Provider` の dead code メンバ (特に `GetCultureInfo`) が実際に呼ばれるようになる

監視は GitHub Issue #40 (再評価トリガ watchdog) で自動化を進める。

## Consequences

### Positive

- ユーザーが実際にプレイできる翻訳を継続出荷できる
- experimental の進展は失わない (v2 で観測継続)
- 観測モードで投資コストを最小化できる
- 再評価条件を normative に定義したことで、再開判断の主観性が下がる

### Negative

- 2 リポジトリの並行管理コストが残る
- v2 の path-independent な翻訳資産 (Markov コーパス / inventory script 等) を v1 にバックポートする際、手動 cherry-pick が必要
- experimental が突然成熟した場合、観測モードからの移行に追加コストがかかる

## References

- `docs/snapshots/2026-04-07-beta-l10n-status.md` — 凍結判断の根拠となった一次調査結果
- README "Status" / "What This Repo Is For" / "Related Repo" 節
- GitHub Issue #40 — 再評価トリガ watchdog
- GitHub Issue #41 — README + decision record + dated snapshot
- `docs/RULES.md` — route 優先順 (`Strings/_T/_S > ExampleLanguage > TextConstants > GameText/ReplaceBuilder > [LanguageProvider] > Harmony`)
