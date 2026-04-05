using System.Collections.Generic;
using System.Text;

namespace QudJP.Patches;

/// <summary>
/// Pure helper methods for Japanese grammar patch behavior.
/// These helpers are intentionally decoupled from Harmony, game DLLs, and global game state.
/// Language gate is passed as an explicit parameter to avoid shared mutable state.
/// </summary>
internal static class GrammarPatchHelpers
{
    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaPluralizeResult(string word, bool isJa) => isJa ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaArticleResult(string word, bool capitalize, bool isJa)
    {
        _ = capitalize;
        return isJa ? word : null;
    }

    /// <summary>
    /// Appends the unchanged word when Japanese is active.
    /// Returns <c>true</c> if handled (Japanese), <c>false</c> to fall through.
    /// </summary>
    public static bool JaArticleAppend(string word, StringBuilder result, bool capitalize, bool isJa)
    {
        _ = capitalize;
        if (!isJa)
        {
            return false;
        }

        result.Append(word);
        return true;
    }

    /// <summary>
    /// Returns the Japanese possessive form by appending <c>の</c>;
    /// otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakePossessiveResult(string word, bool isJa) => isJa ? string.Concat(word, "の") : null;

    /// <summary>
    /// Returns a Japanese "and" list; otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakeAndListResult(IReadOnlyList<string> words, bool serial, bool isJa)
    {
        _ = serial;
        return isJa ? MakeList(words, "と", "、と") : null;
    }

    /// <summary>
    /// Returns a Japanese "or" list; otherwise <c>null</c> when Japanese is inactive.
    /// </summary>
    public static string? JaMakeOrListResult(IReadOnlyList<string> words, bool serial, bool isJa)
    {
        _ = serial;
        return isJa ? MakeList(words, "または", "、または") : null;
    }

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaInitCapResult(string word, bool isJa) => isJa ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaInitLowerResult(string word, bool isJa) => isJa ? word : null;

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaThirdPersonResult(string word, bool prependSpace, bool isJa)
    {
        _ = prependSpace;
        return isJa ? word : null;
    }

    /// <summary>
    /// Returns the unchanged word when Japanese is active; otherwise <c>null</c>.
    /// </summary>
    public static string? JaPastTenseOfResult(string word, bool isJa) => isJa ? word : null;

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
