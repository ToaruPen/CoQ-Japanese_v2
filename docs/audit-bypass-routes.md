# Localization Bypass Route Audit (Issue #12)

Comprehensive audit of all Grammar/Semantics direct calls and raw string
concatenation hotspots in the decompiled beta source (212.17).

## Scope

**This audit covers:**
- All `Grammar.*` static method call sites (366+ calls across ~80 files)
- All `Semantics.*` call sites (3 calls)
- Raw string concatenation hotspots that bypass `_S()`/`_T()`/`GameText`/Provider/Replacer pipelines

**Out of scope (handled by other issues):**
- `Strings/_S/_T` pipeline and `StringsLoader` — Issue #6
- `GameText`/`ReplaceBuilder` — Issue #10
- `HistoricStringExpander` — Issue #11
- `TextConstants` — Issue #14
- `[LanguageProvider]` — Issue #15
- Direct TMP/TextBuilder text assignment — tracked as needed in route-specific issues

## Classification Legend

| Code | Meaning | Action |
|------|---------|--------|
| **H-done** | Already covered by Harmony patches (Issue #13) | None |
| **H-new** | Needs new Harmony patch | Create patch |
| **R** | Covered by Replacer/PostProcessor pipeline | Add ja-specific replacer |
| **P** | Covered by Provider (TranslatorJapanese) | Override in provider |
| **S** | Needs `_S()`/`_T()` wrapping | Modding PR / upstream request |
| **X** | No action needed | Internal/debug/English-only |

---

## 1. Grammar Methods — Patched (Issue #13)

These 14 Harmony Prefix patches intercept **all** call sites globally when
`LanguageLoader.ActiveLanguage == "ja"`. No per-call-site action needed.

| Method | Patch class | Call sites |
|--------|------------|------------|
| `Pluralize(string)` | `GrammarPluralizePatch` | 99 |
| `A(string, bool)` | `GrammarArticleStringPatch` | 42 (shared) |
| `A(string, StringBuilder, bool)` | `GrammarArticleStringBuilderPatch` | (shared) |
| `A(string, TextBuilder, bool)` | `GrammarArticleTextBuilderPatch` | (shared) |
| `A(int, bool)` | `GrammarArticleNumberPatch` | (shared) |
| `A(int, StringBuilder, bool)` | `GrammarArticleNumberStringBuilderPatch` | (shared) |
| `MakePossessive(string)` | `GrammarMakePossessivePatch` | 117 |
| `MakeAndList(IReadOnlyList<string>, bool)` | `GrammarMakeAndListPatch` | 43 |
| `MakeOrList(string[], bool)` | `GrammarMakeOrListArrayPatch` | 12 (shared) |
| `MakeOrList(IReadOnlyList<string>, bool)` | `GrammarMakeOrListPatch` | (shared) |
| `InitCap(string)` | `GrammarInitCapPatch` | 23 |
| `InitLower(string)` | `GrammarInitLowerPatch` | 11 |
| `ThirdPerson(string, bool)` | `GrammarThirdPersonPatch` | 5 |
| `PastTenseOf(string)` | `GrammarPastTenseOfPatch` | 5 |

**Total intercepted call sites: ~357**

---

## 2. Grammar Methods — Unpatched

### 2a. Needs New Harmony Patch (H-new)

| Method | Calls | Impact | Rationale |
|--------|-------|--------|-----------|
| `InitLowerIfArticle(string)` | 2 | Medium | Lowercases if word starts with English article; no-op for Japanese. Patch: return word unchanged when ja. |
| `ConvertAtoAn(string)` | 14 | Medium | Scans sentence for "a" before vowels; irrelevant in Japanese. Patch: return input unchanged when ja. |
| `IndefiniteArticleShouldBeAn(string)` | 7 | Low | Returns bool for a/an choice. Called from TranslatorBase and parts. Patch: return false when ja (no article distinction). |
| `IndefiniteArticleShouldBeAn(int)` | (included above) | Low | Number overload. Same patch. |
| `MakeAndList(IReadOnlyList<GameObject>, ...)` | ~10 | **H-done** | Investigated: internally delegates to `MakeAndList(List<string>, bool)` at Grammar.cs:910. Already intercepted by existing patch. |
| `MakeOrList(List<GameObject>, ...)` | ~4 | **H-done** | Investigated: internally delegates to `MakeOrList(List<string>, bool)` at Grammar.cs:886. Already intercepted by existing patch. |
| `MakeTheList(IReadOnlyList<string>, bool)` | unknown | Low | "The X, the Y, and the Z" — adds definite articles. Patch: join without "the" when ja. |
| `AOrAnBeforeNumber(int)` | 1 | Low | "a 0" vs "an 8". Patch: return Cardinal(number) when ja. |

### 2b. Covered by Replacer Pipeline (R)

| Method | Calls | Location | Notes |
|--------|-------|----------|-------|
| `MakeTitleCase(string)` | 1 | PostProcessors.cs:124 | `{Title}` postprocessor. Add ja postprocessor that returns input unchanged. |
| `MakeTitleCaseWithArticle(string)` | 1 | PostProcessors.cs:137 | Same approach. |
| `GetWordRoot(string)` | 1 | PostProcessors.cs:272 | `{WordRoot}` — English morphology. Ja replacer returns input. |
| `Adjectify(string)` | 1 | StringReplacers.cs:99 | `{Adjectify}` — English adjective form. Ja replacer returns input. |
| `GetRandomMeaningfulWord(string)` | 1 | PostProcessors.cs:293 | Random word extraction. May need Japanese-aware version. |

### 2c. Covered by Provider (P)

| Method | Calls | Notes |
|--------|-------|-------|
| `Cardinal(int/long)` | 4 | Already delegates to `Translator.Cardinal`. TranslatorJapanese can override. |
| `Ordinal(int/long)` | 4 | Delegates to `Translator.Ordinal`. |
| `Multiplicative(int/long)` | 4 | GameStateReplacers only. |
| `GetRomanNumeral(int/long)` | 4 | Universal format, no localization needed. |

### 2d. No Action Needed (X)

| Method | Calls | Rationale |
|--------|-------|-----------|
| `TrimLeadingThe(string)` | 1 | StringReplacers only. Japanese text won't have "The " prefix. No-op. |
| `Obfuscate(string/TextBuilder)` | 1 | Visual noise effect. Works on any script. |
| `Weirdify(string)` | 1 | Obsolete. Marked `[Obsolete]`. |
| `AllowSecondPerson` | 17 | Property/flag, not a method. Controls you/they pronoun choice. Handled at pronoun provider level. |
| `GetProsaicZoneName(Zone)` | 1 | Zone name generation. English-specific phrasing but low priority — zone names are proper nouns. |
| `Stutterize(string, string)` | 0 (external) | Speech effect. Script-agnostic. |
| `MakeCompoundWord(string, string, bool)` | 0 (external) | Compound word formation. Low priority. |
| `ContainsBadWords(string)` | 0 (external) | Content filter. English-only but non-visible. |
| `LevenshteinDistance(string, string)` | 0 (external) | Algorithm. Language-agnostic. |
| Pronoun methods (`RandomShePronoun`, etc.) | varies | Handled by GenderedNoun/PronounProvider system. |

---

## 3. Semantics Direct Calls

| File | Line | Method | Classification |
|------|------|--------|----------------|
| `GameObjectReplacers.cs` | 467 | `Semantics.GetSingularSemantic` | **R** — `{{semantic:...}}` replacer. Add ja-specific semantic lookup. |
| `GameObject.cs` | 6521 | `Semantics.GetSingularSemantic` | **X** — Thin wrapper. Callers go through replacer pipeline. |
| `GameObject.cs` | 6526 | `Semantics.GetPluralSemantic` | **X** — Same. |

---

## 4. Raw String Concatenation Hotspots

### 4a. Highest Priority — Every-Turn Combat Messages

| File | Lines | Pattern | Classification |
|------|-------|---------|----------------|
| `Combat.cs` | 106, 122, 126 | `"You block with " + ...` | **S** |
| `Combat.cs` | 1544, 1568, 1572, 1576 | `"You don't penetrate " + ...` | **S** |
| `Combat.cs` | 1583–1591 | `Attacker.Does("don't") + " penetrate..."` | **S** |

### 4b. High Priority — Story/UI Popups

| File | Lines | Pattern | Classification |
|------|-------|---------|----------------|
| `ChavvahSystem.cs` | 227, 240 | `"You discover " + ...` | **S** |
| `AbsorbablePsyche.cs` | 68, 71, 77 | `"psyche of " + name` | **S** |
| `Leveler.cs` | 332, 343, 349, 359 | Level-up mutation popups | **S** |
| `ITombAnchorSystem.cs` | 93, 156 | Bell of Rest messages | **S** |
| `SvardymSystem.cs` | 122, 126, 177, 180 | Environment narration | **S** |

### 4c. Medium Priority — Object Names with Word Order Issues

| File | Lines | Pattern | Classification |
|------|-------|---------|----------------|
| `RandomStatue.cs` | 42, 46, 54, 58 | `material + " statue of " + name` | **S** — `_T()` template with word order inversion. Simple `_S()` insufficient due to Japanese word order (`name + "の" + material + "像"`); requires template key with positional placeholders. |
| `SultanMuralController.cs` | 347, 375, 401, 440 | `"mural of " + text` | **S** |
| `VillageCoda.cs` | 2118, 2122, 2444 | `"village of " + name` (`_T()` missing) | **S** |
| `SultanShrine.cs` | 247 | `"shrine to " + name` | **S** |

### 4d. Medium Priority — System Messages

| File | Lines | Pattern | Classification |
|------|-------|---------|----------------|
| `FireSuppressionSystem.cs` | 74, 78 | `dram/drams` pluralization | **S** |
| `HiddenRender.cs` | 193 | `" revealed "` word order | **S** |
| `PuffInfection.cs` | 113, 118 | Body part + `spew/spews` | **S** |
| `LiquidFueledPowerPlant.cs` | 270 | Possessive + liquid name | **S** |
| `Fetches.cs` | 40 | `message + obj.an()` | **S** |

### 4e. Low Priority — Skill Messages (high volume, lower visibility)

| File | Lines | Pattern |
|------|-------|---------|
| `Axe_Cleave.cs` | 166–186 | `"cleave through " + poss(text)` |
| `Tactics_Kickback.cs` | 28–69 | `"kick at " + ...` |
| `ShortBlades_Hobble.cs` | 77–81 | `"find a weakness in " + poss` |
| `Cudgel_Backswing.cs` | 50–54 | `"backswing with " + its_` |
| `Discipline_IronMind.cs` | 34–38 | `"shake off some of..."` |
| `Rifle_DrawABead.cs` | 148, 159 | `"lose sight of..."` |
| `Endurance_ShakeItOff.cs` | 52–68 | `"shook off the stun"` |

---

## 5. PostProcessors/Replacers — Grammar Call Summary

12 delegate files in `XRL.World.Text.Delegates/` contain Grammar calls.
Most are in the text replacement pipeline and are already intercepted by
the global Harmony patches (Section 1). Unpatched methods are listed in
Section 2b.

| File | Grammar methods used | Status |
|------|---------------------|--------|
| `PostProcessors.cs` | Pluralize, A, InitLowerIfArticle, MakeTitleCase, MakeTitleCaseWithArticle, MakePossessive, GetWordRoot, GetRandomMeaningfulWord, ConvertAtoAn, Obfuscate | Mixed (most H-done, some H-new/R) |
| `GameObjectReplacers.cs` | Pluralize, ThirdPerson, AllowSecondPerson, MakePossessive | H-done + X |
| `GenderedNounReplacers.cs` | MakePossessive, A, InitCap, IndefiniteArticleShouldBeAn | H-done + H-new |
| `NumberReplacers.cs` | Pluralize, GetRomanNumeral, AOrAnBeforeNumber | H-done + H-new |
| `StringReplacers.cs` | Adjectify, TrimLeadingThe, Pluralize, PastTenseOf | H-done + R + X |
| `ListReplacers.cs` | MakeAndList, MakeOrList, Pluralize | H-done (string overloads) |
| `GameStateReplacers.cs` | Cardinal, Ordinal, GetRomanNumeral, Multiplicative | P |
| `BodyPartReplacers.cs` | InitCap, Pluralize | H-done |
| `GameObjectBlueprintReplacers.cs` | MakePossessive | H-done |
| `JournalReplacers.cs` | InitLowerIfArticle, InitCap | H-done + H-new |
| `LiquidReplacers.cs` | MakeOrList | H-done |
| `VariableReplacers.cs` | Weirdify, GetProsaicZoneName | X |

---

## 6. Required New Harmony Patches — Minimal Set

| Priority | Method | Estimated effort |
|----------|--------|-----------------|
| 1 | `ConvertAtoAn(string)` | Low — return input when ja |
| 2 | `InitLowerIfArticle(string)` | Low — return input when ja |
| 3 | `IndefiniteArticleShouldBeAn(string)` | Low — return false when ja |
| 4 | `IndefiniteArticleShouldBeAn(int)` | Low — return false when ja |
| 5 | `AOrAnBeforeNumber(int)` | Low — return Cardinal(number) when ja |
| 6 | `MakeTheList(IReadOnlyList<string>, bool)` | Low — join without "the" when ja |

~~`MakeAndList(IReadOnlyList<GameObject>)`~~ and ~~`MakeOrList(List<GameObject>)`~~
were investigated and found to delegate to the string overloads internally
(Grammar.cs:910, :886). Already covered by existing patches — no new patches needed.

**Total: 6 new patches (all trivial)**

---

## 7. Summary Statistics

| Category | Count |
|----------|-------|
| Call sites covered by existing Harmony patches | ~371 |
| Call sites needing new Harmony patches | ~26 |
| Call sites covered by Replacer/PostProcessor | ~5 |
| Call sites covered by Provider | ~16 |
| Call sites needing `_S()`/`_T()` wrapping | ~50+ |
| Call sites needing no action | ~30 |
| **Total Grammar.* call sites audited** | **~498** |
| Semantics.* call sites | 3 |
| Raw concatenation hotspots identified | ~30 files |
