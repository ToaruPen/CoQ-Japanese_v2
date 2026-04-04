# Font Requirements for Japanese Localization

## How Caves of Qud renders text

The game uses **TextMeshPro (TMP)** — Unity's `TextMeshProUGUI` component — for all
UI text. Every text-bearing UI element carries a `UITextSkin` MonoBehaviour
(`XRL.UI.UITextSkin`) that calls `FontManager.GetFont(familyName)` on `Awake` and
`Apply` to resolve the correct `TMP_FontAsset`.

The central registry is `FontManager` (`XRL.UI.FontManager`), a `MonoBehaviour` with
a serialized `List<TMP_FontAsset> Fonts`. At startup the game bundles a set of font
assets in `resources.assets`; mods cannot add new `TMP_FontAsset` objects to that
list at runtime.

## Language-to-font mapping

`LanguageLoader.HandleLangNode` stores a `FontFamily` string per language code.
When the language changes, `FontManager.UpdateFontFallbacks()` is called:

```csharp
// FontManager.cs (simplified)
public void UpdateFontFallbacks()
{
    TMP_Settings.fallbackFontAssets.RemoveAll(
        f => f.faceInfo.familyName.StartsWith("Source Han"));

    string fontName = LanguageLoader.ActiveFont;   // from Languages.xml FontFamily
    if (string.IsNullOrEmpty(fontName) || !fontName.StartsWith("Source Han"))
        fontName = "Source Han Mono SC";            // default CJK fallback

    TMP_FontAsset item = Fonts.Find(f => f.faceInfo.familyName == fontName);
    TMP_Settings.fallbackFontAssets.Add(item);

    foreach (UITextSkin skin in FindObjectsByType<UITextSkin>(...))
        skin.ApplyFont();
}
```

This means:

1. The `FontFamily` value in `Languages.xml` **must** match a `TMP_FontAsset`
   already present in `FontManager.Fonts` (i.e., shipped in `resources.assets`).
2. If the font is found it is inserted as a **TMP fallback**; every glyph not in the
   primary font is looked up there.
3. If the font is not found, the default SC (Simplified Chinese) fallback is used
   instead — which covers most CJK codepoints but may show simplified-form glyphs
   instead of Japanese-form glyphs for some kanji.

## Bundled Source Han variants

`FontManager.UpdateFontFallbacks` references names starting with `"Source Han"`.
Based on game beta 2.0.212.17 the following variants are known to be bundled:

| `TMP_FontAsset.faceInfo.familyName` | Script coverage |
|---|---|
| `Source Han Mono SC` | Simplified Chinese + Basic Latin |
| `Source Han Mono JP` | Japanese (preferred for `ja` locale) |

`"Source Han Mono JP"` is the correct value for the `FontFamily` attribute in
`Mods/QudJP/Localization/Languages.xml`.

## What Japanese display needs

| Requirement | Notes |
|---|---|
| Hiragana (U+3040–U+309F) | Fully covered by Source Han Mono JP |
| Katakana (U+30A0–U+30FF) | Fully covered |
| CJK Unified Ideographs (U+4E00–U+9FFF) | Covered; JP variant uses JIS-preferred glyphs |
| Half-width Katakana (U+FF65–U+FF9F) | Covered via Source Han |
| Punctuation (U+3000–U+303F) | Covered |
| Latin / ASCII overlay | Already in the primary (monospace) font |

No additional font files need to be distributed by the mod, because
`Source Han Mono JP` is already present in the game's `resources.assets` bundle.
The `NotoSansCJKjp-Regular-Subset.otf` file in `Mods/QudJP/Fonts/` is retained as a
fallback reference for future tooling (e.g., offline rendering previews) but is **not**
loaded at runtime.

## What would be needed if the bundled font is absent

If a future game update removes or renames the Source Han JP asset, the mod would
need to:

1. Register a custom `TMP_FontAsset` via a Harmony patch or Unity asset bundle — both
   are non-trivial and outside the current beta-first scope.
2. Alternatively, accept SC glyphs as a temporary visual degradation (only affects a
   small set of kanji with variant forms between JP and SC standards).

## TextConstants

`TextConstants` (`XRL.Language.TextConstants`) loads via
`YieldXMLStreamsWithRoot("textconstants")` with no `Lang`-gating at the root level.
It manages game-wide glyph definitions, weird character sets, word lists, and the
cryptic machine charset — none of which are language-specific. Japanese localization
does **not** require a `textconstants` override; the existing English definitions are
sufficient.
