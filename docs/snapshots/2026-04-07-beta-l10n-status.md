# Snapshot: 2026-04-07 — CoQ experimental ブランチ ローカライゼーション実装状況

> **このドキュメントは dated snapshot です。** 2026-04-06〜07 時点の事実を凍結保存するためのもので、以後は更新しません。最新の実装状況は本 snapshot と異なる可能性があります。

## 調査範囲

- **対象ビルド**: Caves of Qud `2.0.212.17` (`lang-experimental` branch / "experimental" labelled in Wiki)
- **対象**: デコンパイル後の `Assembly-CSharp.dll` 全 5,411 .cs ファイル
- **調査者**: ToaruPen + Codex advisor + Claude
- **目的**: experimental ブランチの新ローカライゼーションフレームワークが実装としてどの段階にあるかの一次評価

## Freehold Games 自身の自己評価

| ソース | 引用 |
|---|---|
| Bluesky 公式 ([post `3mi2su2djc226`](https://bsky.app/profile/cavesofqud.com/post/3mi2su2djc226)) | "**experimental alpha of the early stages** of our new localizable generative text systems. **No language packs are available in this early alpha**" |
| Steam / Wiki トップ | "We put up a new, **experimental branch (lang-experimental) for testing a total rewrite** of our text generation systems. ... **We don't have any new languages to test yet; we just want to test the framework**" |
| [Wiki Version history](https://wiki.cavesofqud.com/wiki/Version_history) | `Build 212.17` は **"(experimental)"** ラベル。`Build 211.36` / `211.35` / `211.33` が "(beta)" ラベル。 |
| 公式テスト言語 | **トルコ語 1 言語のみ** |

## 新旧 API 併存 — 旧 API 優位

`Translator.Provider` (新 API) vs 旧 `Grammar.*` + 拡張メソッドの load-bearing 比 (5,411 .cs 横断):

| API | 呼び出し回数 |
|---|---:|
| 新 `Translator.Cardinal` | 6 |
| 旧 `Grammar.Cardinal` | **50** |
| 新 `Translator.MakeAndList` | 1 |
| 旧 `Grammar.MakeAndList` | **44** |
| 新 `Translator.Provider` 全メンバ合計 | ~19 |
| 旧 拡張メソッド `.t(` | **367** |
| 旧 拡張メソッド `.Does(` | **274** |
| 旧 拡張メソッド `.an(` | **175** |
| **新 API 総計 vs 旧 API 総計** | **~19 vs ~910 (比 ~1/48)** |

### `Translator.Provider` の dead code メンバ

24 abstract メンバのうち **5 メンバが dead code** (= `Translator.Provider.{Member}` 形式の呼び出しが 0 件):

1. **`GetCultureInfo`** ← `ja-JP` culture を配送するはずの根幹 API が **一度も呼ばれていない**
2. `GetStringComparer`
3. `GetStringComparerIgnoreCase`
4. `Unweirdify`
5. `AddArticle`

## Hook 化率 (主要 namespace)

| namespace | 総 .cs | hook 使用 | hook 化率 | hardcoded UI 残存 |
|---|---:|---:|---:|---:|
| `XRL.World.Parts` | 1,370 | 226 | 17% | 179 |
| `XRL.World.Effects` | 395 | 67 | 17% | **177** |
| `XRL.UI` | 110 | 26 | 24% | 28 |
| `XRL.UI.Framework` | 66 | **0** | **0%** | 0 |
| `XRL.World` | 709 | 33 | 5% | 10 |
| `XRL.World.ZoneBuilders` | 233 | 12 | 5% | 4 |
| `XRL.Annals` | 66 | 53 | **80%** | 0 |
| `XRL.World.Quests.GolemQuest` | 11 | 11 | **100%** | 3 |
| `XRL.CharacterBuilds.Qud.UI` | 15 | 14 | **93%** | 0 |
| `XRL.World.Parts.Mutation` | 157 | 82 | 52% | 43 |

**全体**: 5,411 .cs 中 hook 使用 **740 ファイル (14%)**、hardcoded UI 残存 **553 ファイル**、両方混在 **212 ファイル**

## 未移行の主要 UI 画面

すべて `_S/_T` 呼び出しゼロ:

| ファイル | hardcoded UI 呼び出し数 |
|---|---:|
| `XRL.World.Parts/MissileWeapon.cs` | **99** |
| `XRL.UI/StatusScreen.cs` | 41 |
| `XRL.World.Capabilities/Wishing.cs` | 38 |
| `XRL.UI/Sidebar.cs` | 30 |
| `XRL/PronounAndGenderSets.cs` | 30 |
| `XRL.UI/TinkeringScreen.cs` | 29 |
| `XRL.UI/KeyMappingUI.cs` | 21 |
| `XRL.UI/EquipmentScreen.cs` | 19 |
| `XRL.UI/OptionsUI.cs` | 19 |
| `XRL.UI/JournalScreen.cs` | 15 |
| `XRL.UI/InventoryScreen.cs` | 10 |

## 過渡期パターンの痕跡

- `XRL.World.Effects/RealityStabilized.cs` に `Strings.AssertLocalizationMatch("English original", ...)` 形式で英語原文を引数に retain しつつ新ルートを足す refactor パターン
- `Grammar.cs:764` に `use Translator.Cardinal` の Obsolete コメント
- `GameObject.cs:6078` に `Use replacer =GameObject.a.name=` の Obsolete コメント
- `XRL.UI/StatusScreen.cs:421` に `TODOJASON GLIMMER` (Jason Grinblat 宛の未処理 TODO)
- 混在ファイル **212 件** (同じ `.cs` に `_S/_T` 経路と hardcoded English が共存)

## 結論 (本 snapshot の評価)

1. ベータブランチはマーケティング上「大幅近代化」と発信されているが、実装の load-bearing 層はほぼ未着手
2. v1 の Harmony patches + owner translation routes アプローチは、現在の CoQ の実装状態に対しては依然として正解
3. v2 の `Strings/_T/_S > ExampleLanguage > [LanguageProvider] > Harmony (last resort)` ポリシーは、Freehold の宣伝に基づく期待値であり、現実の load-bearing 分布とはずれている
4. Freehold が `_S/_T` 化を主要 UI / game part に拡大するまで、v1 の Harmony アプローチのほうが体感翻訳率が高い

詳細な意思決定と再評価トリガは [`docs/decisions/0001-v1-v2-roles.md`](../decisions/0001-v1-v2-roles.md) を参照。

## 一次情報

- [Caves of Qud Wiki — Version history](https://wiki.cavesofqud.com/wiki/Version_history)
- [Caves of Qud Wiki — main page (lang-experimental 告知)](https://wiki.cavesofqud.com/wiki/Caves_of_Qud_Wiki)
- [Bluesky — cavesofqud.com post 3mi2su2djc226](https://bsky.app/profile/cavesofqud.com/post/3mi2su2djc226)
- [Steam Community — Caves of Qud](https://steamcommunity.com/app/333640)
- [Steam — Beta Patch March 1, 2026](https://store.steampowered.com/news/app/333640/view/508480685901611761)
- [itch.io devlog — Patch 1.04 ("architecture changes to prepare for localization")](https://freeholdgames.itch.io/cavesofqud/devlog/950623/patch-104-is-here)
