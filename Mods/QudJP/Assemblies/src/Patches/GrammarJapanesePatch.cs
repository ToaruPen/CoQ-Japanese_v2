#if HAS_GAME_DLL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.Reflection;
using System.Text;
using HarmonyLib;
using XRL.Language;
using XRL.World.Text;

namespace QudJP.Patches;

[HarmonyPatch(typeof(Grammar), nameof(Grammar.Pluralize))]
[HarmonyPatch(new Type[] { typeof(string) })]
internal static class GrammarPluralizePatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaPluralizeResult(word);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarPluralizePatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.A))]
[HarmonyPatch(new Type[] { typeof(string), typeof(bool) })]
internal static class GrammarArticleStringPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, bool capitalize, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaArticleResult(word, capitalize);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarArticleStringPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.A))]
[HarmonyPatch(new Type[] { typeof(string), typeof(StringBuilder), typeof(bool) })]
internal static class GrammarArticleStringBuilderPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, StringBuilder result, bool capitalize)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            if (!GrammarPatchHelpers.IsJapaneseActive)
            {
                return true;
            }

            GrammarPatchHelpers.JaArticleAppend(word, result, capitalize);
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarArticleStringBuilderPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.A))]
[HarmonyPatch(new Type[] { typeof(string), typeof(TextBuilder), typeof(bool) })]
internal static class GrammarArticleTextBuilderPatch
{
    private static readonly MethodInfo? AppendStringMethod =
        typeof(TextBuilder).GetMethod(nameof(TextBuilder.Append), new Type[] { typeof(string) });

    [HarmonyPrefix]
    internal static bool Prefix(string word, TextBuilder result, bool capitalize)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var article = GrammarPatchHelpers.JaArticleResult(word, capitalize);
            if (article == null)
            {
                return true;
            }

            if (AppendStringMethod == null)
            {
                Trace.TraceWarning("QudJP: GrammarArticleTextBuilderPatch could not resolve TextBuilder.Append(string), falling back to English.");
                return true;
            }

            _ = AppendStringMethod.Invoke(result, new object[] { article });
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarArticleTextBuilderPatch failed, falling back to English. {0}", ex.InnerException?.Message ?? ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.A))]
[HarmonyPatch(new Type[] { typeof(int), typeof(bool) })]
internal static class GrammarArticleNumberPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(int number, bool capitalize, ref string __result)
    {
        try
        {
            _ = capitalize;
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            if (!GrammarPatchHelpers.IsJapaneseActive)
            {
                return true;
            }

            __result = Translator.Cardinal(number);
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarArticleNumberPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.A))]
[HarmonyPatch(new Type[] { typeof(int), typeof(StringBuilder), typeof(bool) })]
internal static class GrammarArticleNumberStringBuilderPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(int number, StringBuilder result, bool capitalize)
    {
        try
        {
            _ = capitalize;
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            if (!GrammarPatchHelpers.IsJapaneseActive)
            {
                return true;
            }

            result.Append(Translator.Cardinal(number));
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarArticleNumberStringBuilderPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.MakePossessive))]
[HarmonyPatch(new Type[] { typeof(string) })]
internal static class GrammarMakePossessivePatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaMakePossessiveResult(word);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarMakePossessivePatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.MakeAndList))]
[HarmonyPatch(new Type[] { typeof(IReadOnlyList<string>), typeof(bool) })]
internal static class GrammarMakeAndListPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(IReadOnlyList<string> words, bool serial, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaMakeAndListResult(words, serial);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarMakeAndListPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.MakeOrList))]
[HarmonyPatch(new Type[] { typeof(string[]), typeof(bool) })]
internal static class GrammarMakeOrListArrayPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string[] words, bool serial, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaMakeOrListResult((IReadOnlyList<string>)words, serial);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarMakeOrListArrayPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.MakeOrList))]
[HarmonyPatch(new Type[] { typeof(IReadOnlyList<string>), typeof(bool) })]
internal static class GrammarMakeOrListPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(IReadOnlyList<string> words, bool serial, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaMakeOrListResult(words, serial);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarMakeOrListPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.InitCap))]
[HarmonyPatch(new Type[] { typeof(string) })]
internal static class GrammarInitCapPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaInitCapResult(word);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarInitCapPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.InitLower))]
[HarmonyPatch(new Type[] { typeof(string) })]
internal static class GrammarInitLowerPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaInitLowerResult(word);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarInitLowerPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.ThirdPerson))]
[HarmonyPatch(new Type[] { typeof(string), typeof(bool) })]
internal static class GrammarThirdPersonPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string word, bool prependSpace, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaThirdPersonResult(word, prependSpace);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarThirdPersonPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}

[HarmonyPatch(typeof(Grammar), nameof(Grammar.PastTenseOf))]
[HarmonyPatch(new Type[] { typeof(string) })]
internal static class GrammarPastTenseOfPatch
{
    [HarmonyPrefix]
    internal static bool Prefix(string verb, ref string __result)
    {
        try
        {
            GrammarPatchHelpers.IsJapaneseActive = LanguageLoader.ActiveLanguage == "ja";
            var result = GrammarPatchHelpers.JaPastTenseOfResult(verb);
            if (result == null)
            {
                return true;
            }

            __result = result;
            return false;
        }
        catch (Exception ex)
        {
            Trace.TraceWarning("QudJP: GrammarPastTenseOfPatch failed, falling back to English. {0}", ex.Message);
            return true;
        }
    }
}
#endif
