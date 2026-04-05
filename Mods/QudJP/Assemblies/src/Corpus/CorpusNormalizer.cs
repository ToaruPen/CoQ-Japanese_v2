using System;
using System.Text.RegularExpressions;

namespace QudJP.Corpus;

/// <summary>
/// Pure-logic helpers for Japanese Markov corpus processing.
/// No game-type dependencies — testable in L1.
/// </summary>
internal static class CorpusNormalizer
{
    private const string LibraryCorpusFileName = "LibraryCorpus.json";

    private static readonly Regex WhitespacePattern = new Regex(@"\s+", RegexOptions.Compiled);

    /// <summary>
    /// Returns <c>true</c> when the corpus name matches the hardcoded
    /// <c>LibraryCorpus.json</c> that the Markov engine loads by default.
    /// </summary>
    public static bool ShouldUseJapaneseCorpus(string corpusName)
    {
        return string.Equals(corpusName, LibraryCorpusFileName, StringComparison.Ordinal);
    }

    /// <summary>
    /// Normalizes a Japanese corpus sentence for the Markov engine:
    /// collapses whitespace, converts Japanese period to ASCII period,
    /// and ensures the sentence ends with a period.
    /// The ASCII period is load-bearing — <c>MarkovChain.GenerateSentence</c>
    /// terminates on <c>text.Contains(".")</c>.
    /// </summary>
    public static string NormalizeSentence(string sentence)
    {
        if (string.IsNullOrWhiteSpace(sentence))
        {
            return string.Empty;
        }

        var normalized = WhitespacePattern.Replace(sentence.Trim(), " ");

        // Convert Japanese period (。) to ASCII period
        if (normalized.Length > 0 && normalized[normalized.Length - 1] == '\u3002')
        {
            normalized = normalized.Substring(0, normalized.Length - 1) + ".";
        }

        // Ensure trailing period
        if (normalized.Length == 0 || normalized[normalized.Length - 1] != '.')
        {
            normalized += ".";
        }

        return normalized;
    }

    /// <summary>
    /// Returns <c>true</c> if <paramref name="text"/> contains at least one
    /// Hiragana, Katakana, or CJK Unified Ideograph character.
    /// </summary>
    public static bool ContainsJapaneseCharacters(string text)
    {
        for (var i = 0; i < text.Length; i++)
        {
            var c = text[i];
            if ((c >= '\u3040' && c <= '\u309F')
                || (c >= '\u30A0' && c <= '\u30FF')
                || (c >= '\u4E00' && c <= '\u9FFF'))
            {
                return true;
            }
        }

        return false;
    }
}
