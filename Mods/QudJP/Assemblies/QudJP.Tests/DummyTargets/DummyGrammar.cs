#pragma warning disable CA1707
#pragma warning disable CA1308 // Pluralize uses ToLower faithfully

using System.Text;
using System.Text.RegularExpressions;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of English grammar methods from XRL.Language.Grammar
/// (decompiled beta 212.17). Covers methods called by language-dependent PostProcessors.
/// <para>
/// Dictionary-based lookups (irregularPluralization, latinPluralization, etc.) are
/// omitted for test portability. Core suffix rules and helper methods are faithfully
/// reproduced. Game singletons are replaced with injectable parameters where needed.
/// </para>
/// </summary>
internal static class DummyGrammar
{
    // --- Compiled Regex patterns for Pluralize (avoid recompilation per call) ---

    private static readonly Regex RxColorMarkup = new(@"^{{(.*?)\|(.*)}}$", RegexOptions.Compiled);
    private static readonly Regex RxTrailingColor = new(@"(.*?)(&.(?:\^.)?)$", RegexOptions.Compiled);
    private static readonly Regex RxLeadingColor = new(@"^(&.(?:\^.)?)(.*?)$", RegexOptions.Compiled);
    private static readonly Regex RxQuotedWrap = new(@"^([*\-_~'""/])(.*)(\1)$", RegexOptions.Compiled);
    private static readonly Regex RxTrailingSpace = new(@"(.*?)( +)$", RegexOptions.Compiled);
    private static readonly Regex RxLeadingSpace = new(@"^( +)(.*?)$", RegexOptions.Compiled);
    private static readonly Regex RxParenSuffix = new(@"^(.*)( \(.*\))$", RegexOptions.Compiled);
    private static readonly Regex RxBracketSuffix = new(@"^(.*)( \[.*\])$", RegexOptions.Compiled);
    private static readonly Regex RxPrepPhrase = new(@"^(.*)( (?:of|in a|in an|in the|into|for|from|o'|to|with) .*)$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RxMarkSuffix = new(@"^(.*)( (?:mk\.?|mark) *(?:[ivx]+))$", RegexOptions.Compiled | RegexOptions.IgnoreCase);
    private static readonly Regex RxHyphenated = new(@"^(.*)-(.*)$", RegexOptions.Compiled);

    // --- Articles and prepositions (faithful to Grammar.cs line ~100-110) ---

    private static readonly HashSet<string> Articles = ["an", "a", "the"];

    private static readonly HashSet<string> ShortPrepositions =
    [
        "a", "an", "and", "as", "at", "but", "by", "for", "if", "in",
        "nor", "of", "off", "on", "or", "per", "so", "the", "to", "up", "via", "vs", "yet",
    ];

    private static readonly HashSet<string> Conjunctions =
    [
        "and", "but", "for", "nor", "or", "so", "yet",
    ];

    /// <summary>
    /// Simplified article exceptions list (faithful to Grammar.articleExceptions).
    /// Words that start with a vowel but use "a" (or start with consonant but use "an").
    /// </summary>
    private static readonly string[] ArticleExceptions =
    [
        "one", "once", "uni", "unique", "unicorn", "union", "united", "unit", "universe",
        "universal", "university", "usage", "use", "used", "useful", "user", "usual", "usually",
        "usurp", "usurper", "utensil", "utility", "utopia", "utter", "unanimous", "uniform",
        "uniformly", "uranium", "urinal",
        "hour", "honest", "honor", "honour", "heir",
    ];

    // --- Pluralize ---

    /// <summary>
    /// Faithful reproduction of Grammar.Pluralize (line 292-571).
    /// Core suffix rules without dictionary-based special cases.
    /// </summary>
    public static string Pluralize(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        // Variable reference passthrough
        if (word[0] == '=')
        {
            return "=pluralize=" + word;
        }

        // Color markup: {{color|text}} → recurse on inner text
        Match match = RxColorMarkup.Match(word);
        if (match.Success)
        {
            return "{{" + match.Groups[1].Value + "|" + Pluralize(match.Groups[2].Value) + "}}";
        }

        // Trailing color codes: text&X → pluralize(text) + &X
        match = RxTrailingColor.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Leading color codes: &Xtext → &X + pluralize(text)
        match = RxLeadingColor.Match(word);
        if (match.Success)
        {
            return match.Groups[1].Value + Pluralize(match.Groups[2].Value);
        }

        // Quoted/wrapped: *text* → * + pluralize(text) + *
        match = RxQuotedWrap.Match(word);
        if (match.Success)
        {
            return match.Groups[1].Value + Pluralize(match.Groups[2].Value) + match.Groups[3].Value;
        }

        // Trailing space
        match = RxTrailingSpace.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Leading space
        match = RxLeadingSpace.Match(word);
        if (match.Success)
        {
            return match.Groups[1].Value + Pluralize(match.Groups[2].Value);
        }

        // Parenthesized suffix: text (stuff) → pluralize(text) + (stuff)
        match = RxParenSuffix.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Bracketed suffix
        match = RxBracketSuffix.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Prepositional phrase: text of/in/from/... rest
        match = RxPrepPhrase.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Mark/MK suffix
        match = RxMarkSuffix.Match(word);
        if (match.Success)
        {
            return Pluralize(match.Groups[1].Value) + match.Groups[2].Value;
        }

        // Hyphenated (no spaces): pluralize after last hyphen
        if (!word.Contains(' ', StringComparison.Ordinal))
        {
            match = RxHyphenated.Match(word);
            if (match.Success)
            {
                return match.Groups[1].Value + "-" + Pluralize(match.Groups[2].Value);
            }
        }

        // -folk: identical plural
        if (word.EndsWith("folk", StringComparison.Ordinal))
        {
            return word;
        }

        // Special suffixes (faithful to line 517-539)
        if (word.EndsWith("elf", StringComparison.Ordinal) || word.EndsWith("olf", StringComparison.Ordinal) ||
            word.EndsWith("arf", StringComparison.Ordinal) || word.EndsWith("alf", StringComparison.Ordinal))
        {
            return word[..^1] + "ves";
        }

        if (word.EndsWith("man", StringComparison.Ordinal) && !string.Equals(word, "human", StringComparison.Ordinal))
        {
            return word[..^2] + "en";
        }

        if (word.EndsWith("ife", StringComparison.Ordinal))
        {
            return word[..^2] + "ves";
        }

        if (word.EndsWith("mensch", StringComparison.Ordinal))
        {
            return word + "en";
        }

        // Single letter
        if (word.Length == 1)
        {
            return word + "s";
        }

        // Multi-word: pluralize last word
        if (word.Contains(' ', StringComparison.Ordinal))
        {
            string[] parts = word.Split(' ');
            parts[^1] = Pluralize(parts[^1]);
            return string.Join(" ", parts).TrimEnd();
        }

        // Case handling (simplified from ColorUtility checks)
        if (word.All(c => char.IsUpper(c) || !char.IsLetter(c)))
        {
            // ALL CAPS → pluralize lowercase, then re-uppercase
            return Pluralize(word.ToLowerInvariant()).ToUpperInvariant();
        }

        if (char.IsUpper(word[0]) && word.Skip(1).All(c => char.IsLower(c) || !char.IsLetter(c)))
        {
            // Title Case → pluralize lowercase, then capitalize
            string lower = Pluralize(word.ToLowerInvariant());
            return char.ToUpperInvariant(lower[0]) + lower[1..];
        }

        // Core suffix rules (faithful to line 550-590)
        char last = word[^1];
        char secondLast = word[^2];
        string lastTwo = word[^2..];
        string result = word;

        // Double z after vowel
        if (last == 'z' && "aeiou".Contains(secondLast, StringComparison.Ordinal))
        {
            result += "z";
        }

        if (last is 's' or 'x' or 'z' || lastTwo is "sh" or "ss" or "ch")
        {
            return result + "es";
        }

        if (last == 'o' && secondLast != 'o' && secondLast != 'b')
        {
            return result + "es";
        }

        if (last == 'y' && !"aeiou".Contains(secondLast, StringComparison.Ordinal))
        {
            return result[..^1] + "ies";
        }

        return result + "s";
    }

    // --- Article (a/an) ---

    /// <summary>
    /// Faithful to Grammar.IndefiniteArticleShouldBeAn(string) (line 1256-1312).
    /// Simplified: skips ColorUtility stripping for test portability.
    /// </summary>
    public static bool IndefiniteArticleShouldBeAn(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return false;
        }

        string text = DummyColorUtility.StripFormatting(word);
        text = text.TrimStart();

        int spaceIdx = text.IndexOf(' ', StringComparison.Ordinal);
        if (spaceIdx != -1)
        {
            text = text[..spaceIdx];
        }

        // Strip non-word characters
        StringBuilder sb = new();
        foreach (char c in text)
        {
            if (char.IsLetterOrDigit(c) || c == '-')
            {
                sb.Append(c);
            }
        }

        text = sb.ToString().TrimStart('-');
        if (text.Length == 0)
        {
            return false;
        }

        char first = text[0];
        bool isVowelOrEight = first is 'a' or 'e' or 'i' or 'o' or 'u'
                              or 'A' or 'E' or 'I' or 'O' or 'U' or '8';
        bool isException = IsArticleException(text);

        if (isVowelOrEight ^ isException)
        {
            return !text.StartsWith("one-", StringComparison.OrdinalIgnoreCase);
        }

        return false;
    }

    /// <summary>
    /// Faithful to Grammar.IsArticleException (line 1060-1074).
    /// </summary>
    private static bool IsArticleException(string word)
    {
        foreach (string exception in ArticleExceptions)
        {
            if (string.Equals(word, exception, StringComparison.OrdinalIgnoreCase))
            {
                return true;
            }
        }

        return false;
    }

    /// <summary>
    /// Faithful to Grammar.A(string, bool) (line 1336-1343).
    /// Returns "a " or "an " + word, with optional capitalization.
    /// </summary>
    public static string A(string word, bool capitalize = false)
    {
        if (word.Length > 0 && word[0] == '=')
        {
            return "=article=" + word;
        }

        string article = GetArticlePrefix(word, capitalize);
        return article + word;
    }

    /// <summary>
    /// Faithful to Grammar.A(string, StringBuilder, bool) (line 1345-1355).
    /// </summary>
    public static void A(string word, StringBuilder result, bool capitalize = false)
    {
        if (word.Length > 0 && word[0] == '=')
        {
            result.Append("=article=").Append(word);
        }
        else
        {
            string article = GetArticlePrefix(word, capitalize);
            result.Append(article).Append(word);
        }
    }

    private static string GetArticlePrefix(string word, bool capitalize)
    {
        if (IndefiniteArticleShouldBeAn(word))
        {
            return capitalize ? "An " : "an ";
        }

        return capitalize ? "A " : "a ";
    }

    // --- MakePossessive ---

    /// <summary>
    /// Faithful to Grammar.MakePossessive (line 944-964).
    /// </summary>
    public static string MakePossessive(string word)
    {
        // Strip trailing }}
        int closeBraces = 0;
        while (word.EndsWith("}}", StringComparison.Ordinal))
        {
            closeBraces++;
            word = word[..^2];
        }

        word = word switch
        {
            "you" => "your",
            "You" => "Your",
            "YOU" => "YOUR",
            _ => word.EndsWith('s') ? word + "'" : word + "'s",
        };

        for (int i = 0; i < closeBraces; i++)
        {
            word += "}}";
        }

        return word;
    }

    // --- MakeTitleCase ---

    /// <summary>
    /// Faithful to Grammar.IsLowerCapWord (line 1044-1052).
    /// </summary>
    public static bool IsLowerCapWord(string word)
    {
        string lower = word.Any(char.IsUpper) ? word.ToLowerInvariant() : word;
        return ShortPrepositions.Contains(lower) || Articles.Contains(lower) || Conjunctions.Contains(lower);
    }

    /// <summary>
    /// Faithful to Grammar.InitialCap (line 1009-1032).
    /// </summary>
    public static string InitialCap(string word)
    {
        if (string.IsNullOrEmpty(word))
        {
            return word;
        }

        if (word.Contains('-', StringComparison.Ordinal))
        {
            string[] parts = word.Split('-');
            for (int i = 0; i < parts.Length; i++)
            {
                if (parts[i].Length > 1)
                {
                    parts[i] = char.ToUpperInvariant(parts[i][0]) + parts[i][1..];
                }
                else if (i == 0)
                {
                    parts[i] = parts[i].ToUpperInvariant();
                }
            }

            return string.Join("-", parts);
        }

        return char.ToUpperInvariant(word[0]) + word[1..];
    }

    /// <summary>
    /// Faithful to Grammar.MakeTitleCase (line 983-1007).
    /// </summary>
    public static string MakeTitleCase(string phrase)
    {
        string[] words = phrase.Split(' ');
        bool first = true;
        StringBuilder sb = new();

        foreach (string word in words)
        {
            if (first)
            {
                sb.Append(InitialCap(word));
                first = false;
            }
            else if (IsLowerCapWord(word))
            {
                sb.Append(word);
            }
            else
            {
                sb.Append(InitialCap(word));
            }

            sb.Append(' ');
        }

        return sb.ToString().TrimEnd();
    }

    /// <summary>
    /// Faithful to Grammar.MakeTitleCaseWithArticle (line 1233-1241).
    /// </summary>
    public static string MakeTitleCaseWithArticle(string phrase)
    {
        if (phrase.StartsWith("a ", StringComparison.Ordinal) || phrase.StartsWith("A ", StringComparison.Ordinal) ||
            phrase.StartsWith("an ", StringComparison.Ordinal) || phrase.StartsWith("An ", StringComparison.Ordinal) ||
            phrase.StartsWith("the ", StringComparison.Ordinal) || phrase.StartsWith("The ", StringComparison.Ordinal) ||
            phrase.StartsWith("some ", StringComparison.Ordinal) || phrase.StartsWith("Some ", StringComparison.Ordinal))
        {
            string text = MakeTitleCase(phrase);
            return char.ToLowerInvariant(text[0]) + text[1..];
        }

        return MakeTitleCase(phrase);
    }

    // --- InitLowerIfArticle ---

    /// <summary>
    /// Faithful to Grammar.InitLowerIfArticle (line 1190-1218).
    /// Simplified: uses hardcoded article list instead of TextConstants.GetWordList.
    /// </summary>
    public static string InitLowerIfArticle(string word)
    {
        string[] capitalizedArticles = ["A ", "An ", "The ", "Some "];
        foreach (string article in capitalizedArticles)
        {
            if (word.StartsWith(article, StringComparison.Ordinal))
            {
                return char.ToLowerInvariant(word[0]) + word[1..];
            }
        }

        return word;
    }

    // --- ConvertAtoAn ---

    /// <summary>
    /// Faithful to Grammar.ConvertAtoAn (line 1379-1396).
    /// Scans for "a" followed by a vowel-sound word and converts to "an".
    /// </summary>
    public static string ConvertAtoAn(string sentence)
    {
        string[] words = sentence.Split(' ');
        StringBuilder sb = new();

        for (int i = 0; i < words.Length; i++)
        {
            sb.Append(words[i]);
            if (i < words.Length - 1)
            {
                if ((words[i] == "a" || words[i] == "A") &&
                    !string.IsNullOrEmpty(words[i + 1]) &&
                    IndefiniteArticleShouldBeAn(words[i + 1]))
                {
                    sb.Append('n');
                }

                sb.Append(' ');
            }
        }

        return sb.ToString();
    }
}

#pragma warning restore CA1308
#pragma warning restore CA1707
