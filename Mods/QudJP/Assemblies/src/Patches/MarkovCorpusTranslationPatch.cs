#if HAS_GAME_DLL
using System;
using System.Collections.Generic;
using System.Diagnostics;
using System.IO;
using System.Runtime.Serialization.Json;
using System.Text;
using HarmonyLib;
using QudJP.Corpus;
using XRL;
using XRL.World.Parts;

namespace QudJP.Patches;

/// <summary>
/// Harmony Prefix patch on <see cref="MarkovBook.EnsureCorpusLoaded(string)"/>.
/// Intercepts <c>LibraryCorpus.json</c> loads and substitutes the Japanese corpus.
/// <para>
/// Why Harmony: The beta hardcodes <c>"LibraryCorpus.json"</c> in 4 call sites
/// (GameText, VariableReplacers) and <c>MarkovBook</c> resolves via
/// <c>DataManager.FilePath</c> which only looks in <c>StreamingAssets/Base</c>.
/// No beta-native extension point (<c>[HasVariableReplacer]</c>, <c>[LanguageProvider]</c>)
/// covers the <c>MarkovBook.CorpusData</c> cache that in-game books read from.
/// </para>
/// </summary>
[HarmonyPatch(typeof(MarkovBook), nameof(MarkovBook.EnsureCorpusLoaded))]
internal static class MarkovCorpusTranslationPatch
{
    // Test-only override for corpus file path.
    private static string? corpusPathOverride;

    /// <summary>
    /// Harmony Prefix: intercepts <c>LibraryCorpus.json</c> and loads the
    /// Japanese corpus instead. Returns <c>false</c> (skip original) on success,
    /// <c>true</c> (fall through to English) on any failure.
    /// </summary>
    [HarmonyPrefix]
    internal static bool Prefix(string Corpus)
    {
        try
        {
            if (!CorpusNormalizer.ShouldUseJapaneseCorpus(Corpus))
            {
                return true; // Not LibraryCorpus.json — let original run
            }

            return !EnsureJapaneseCorpusLoaded(Corpus);
        }
        catch (Exception ex)
        {
            Trace.TraceError("QudJP: MarkovCorpusTranslationPatch failed, falling back to English. {0}", ex);
            return true; // Fall back to original English corpus
        }
    }

    /// <summary>
    /// Loads the Japanese corpus into <see cref="MarkovBook.CorpusData"/> under the
    /// same key (<c>"LibraryCorpus.json"</c>) so all 4 hardcoded references resolve
    /// to Japanese chain data.
    /// </summary>
    private static bool EnsureJapaneseCorpusLoaded(string corpusKey)
    {
        if (MarkovBook.CorpusData.ContainsKey(corpusKey))
        {
            return true; // Already loaded (idempotent)
        }

        var sentences = LoadJapaneseCorpusSentences();
        if (sentences.Length == 0)
        {
            Trace.TraceWarning("QudJP: Japanese corpus is empty, falling back to English.");
            return false;
        }

        var corpusText = JoinAndNormalize(sentences);
        var chainData = MarkovChain.BuildChain(corpusText, 2);
        MarkovBook.CorpusData[corpusKey] = chainData;

        try
        {
            MarkovBook.PostprocessLoadedCorpus(chainData);
        }
        catch (Exception ex)
        {
            // Postprocess failed (e.g. The.Game == null). Roll back cache entry.
            MarkovBook.CorpusData.Remove(corpusKey);
            Trace.TraceWarning("QudJP: PostprocessLoadedCorpus failed, rolling back. {0}", ex.Message);
            return false;
        }

        return true;
    }

    /// <summary>
    /// Reads and deserializes the Japanese corpus JSON file.
    /// Uses <see cref="DataContractJsonSerializer"/> (net48-compatible).
    /// </summary>
    private static string[] LoadJapaneseCorpusSentences()
    {
        var path = corpusPathOverride ?? ResolveCorpusPath();
        var json = File.ReadAllBytes(path);
        var serializer = new DataContractJsonSerializer(typeof(JapaneseCorpusDocument));
        using var stream = new MemoryStream(json);
        var doc = (JapaneseCorpusDocument?)serializer.ReadObject(stream)
            ?? throw new InvalidDataException("QudJP: Failed to deserialize Japanese corpus.");
        return doc.Sentences;
    }

    /// <summary>
    /// Joins all sentences into a single space-delimited corpus string,
    /// normalizing each sentence for the Markov engine.
    /// </summary>
    private static string JoinAndNormalize(string[] sentences)
    {
        var sb = new StringBuilder(sentences.Length * 70);
        foreach (var sentence in sentences)
        {
            var normalized = CorpusNormalizer.NormalizeSentence(sentence);
            if (normalized.Length > 0)
            {
                if (sb.Length > 0)
                {
                    sb.Append(' ');
                }

                sb.Append(normalized);
            }
        }

        return sb.ToString();
    }

    /// <summary>
    /// Resolves the corpus file path relative to the mod's Localization directory.
    /// </summary>
    private static string ResolveCorpusPath()
    {
        var assemblyDir = Path.GetDirectoryName(typeof(MarkovCorpusTranslationPatch).Assembly.Location);
        if (string.IsNullOrEmpty(assemblyDir))
        {
            assemblyDir = AppContext.BaseDirectory;
        }

        // Navigate from Assemblies/ up to Mods/QudJP/, then into Localization/Corpus/
        var modRoot = Path.GetDirectoryName(assemblyDir);
        if (modRoot == null)
        {
            Trace.TraceWarning("QudJP: Could not resolve mod root from assembly directory '{0}', using assembly directory.", assemblyDir);
            modRoot = assemblyDir;
        }

        return Path.Combine(modRoot, "Localization", "Corpus", "LibraryCorpus.ja.json");
    }

    // --- Test helpers ---

    internal static void SetCorpusPathForTests(string? path) => corpusPathOverride = path;

    internal static void ResetForTests()
    {
        corpusPathOverride = null;
    }

    internal static int GetOpeningWordCount(MarkovChainData data) =>
        data.OpeningWords.Count;

    internal static int GetTransitionCount(MarkovChainData data) =>
        MarkovChain.Count(data);

    internal static Dictionary<string, MarkovChainData> GetCorpusCacheForTests() =>
        MarkovBook.CorpusData;
}
#endif
