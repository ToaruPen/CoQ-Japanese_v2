using System.Collections.Generic;
using System.Text;

namespace QudJP.Patches;

/// <summary>
/// Pure helper methods for Japanese grammar patch behavior.
/// These helpers are intentionally decoupled from Harmony, game DLLs, and global game state.
/// </summary>
internal static class GrammarPatchHelpers
{
    /// <summary>
    /// Gets or sets a testable gate for Japanese-specific behavior.
    /// When <c>false</c>, string-returning helpers fall through by returning <c>null</c>.
    /// </summary>
    public static bool IsJapaneseActive { get; set; }

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaPluralizeResult(string word) => IsJapaneseActive ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaArticleResult(string word, bool capitalize)
    {
        _ = capitalize;
        return IsJapaneseActive ? word : null;
    }

    /// <summary>
    /// Appends the unchanged word when Japanese is active.
    /// When Japanese is inactive, this method does not append anything.
    /// </summary>
    public static void JaArticleAppend(string word, StringBuilder result, bool capitalize)
    {
        _ = capitalize;
        if (IsJapaneseActive)
        {
            result.Append(word);
        }
    }

    /// <summary>
    /// Returns the Japanese possessive form by appending <c>の</c>;
    /// otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakePossessiveResult(string word) => IsJapaneseActive ? string.Concat(word, "の") : null;

    /// <summary>
    /// Returns a Japanese "and" list; otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakeAndListResult(IReadOnlyList<string> words, bool serial)
    {
        _ = serial;
        return IsJapaneseActive ? MakeList(words, "と", "、と") : null;
    }

    /// <summary>
    /// Returns a Japanese "or" list; otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakeOrListResult(IReadOnlyList<string> words, bool serial)
    {
        _ = serial;
        return IsJapaneseActive ? MakeList(words, "または", "、または") : null;
    }

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaInitCapResult(string word) => IsJapaneseActive ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaInitLowerResult(string word) => IsJapaneseActive ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaThirdPersonResult(string word, bool prependSpace)
    {
        _ = prependSpace;
        return IsJapaneseActive ? word : null;
    }

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaPastTenseOfResult(string word) => IsJapaneseActive ? word : null;

    private static string MakeList(IReadOnlyList<string> words, string pairConnector, string finalConnector)
    {
        switch (words.Count)
        {
            case 0:
                return string.Empty;
            case 1:
                return words[0] ?? string.Empty;
            case 2:
                return string.Concat(words[0], pairConnector, words[1]);
            default:
            {
                var sb = new StringBuilder();
                for (var i = 0; i < words.Count - 1; i++)
                {
                    if (i > 0)
                    {
                        sb.Append('、');
                    }

                    sb.Append(words[i]);
                }

                sb.Append(finalConnector);
                sb.Append(words[words.Count - 1]);
                return sb.ToString();
            }
        }
    }
}
