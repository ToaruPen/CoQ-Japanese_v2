#pragma warning disable CA1707

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of English-specific (language-dependent) postprocessors from
/// XRL.World.Text.Delegates.PostProcessors (decompiled beta 212.17).
/// These call into <see cref="DummyGrammar"/> for English grammar operations.
/// <para>
/// In the game, these are registered via <c>[HasVariableReplacer]</c> (no Lang filter)
/// and can be overridden by <c>[HasVariableReplacer(Lang="ja")]</c> classes.
/// </para>
/// </summary>
internal static class DummyEnglishPostProcessors
{
    /// <summary>
    /// Pluralizes Value. Keys: "pluralize", "plural".
    /// Faithful to PostProcessors.Pluralize.
    /// </summary>
    public static string? Pluralize(DummyVariableContext ctx, object[] _)
    {
        string word = ctx.Value.ToString();
        word = DummyGrammar.Pluralize(word);
        ctx.Value.Clear();
        ctx.Value.Append(word);
        return null;
    }

    /// <summary>
    /// Prepends indefinite article (a/an) to Value. Capitalization-aware.
    /// Key: "article" (with Capitalization=true → also "Article").
    /// Faithful to PostProcessors.Article.
    /// </summary>
    public static string? Article(DummyVariableContext ctx, object[] _)
    {
        string word = ctx.Value.ToString();
        ctx.Value.Clear();
        DummyGrammar.A(word, ctx.Value, ctx.Capitalize);
        return null;
    }

    /// <summary>
    /// Makes Value possessive ('s or ').
    /// Keys: "possessive", "makePossessive", "'s".
    /// Faithful to PostProcessors.Possessive.
    /// </summary>
    public static string? Possessive(DummyVariableContext ctx, object[] _)
    {
        string value = DummyGrammar.MakePossessive(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(value);
        return null;
    }

    /// <summary>
    /// Title cases Value. Key: "title".
    /// Faithful to PostProcessors.Title.
    /// </summary>
    public static string? Title(DummyVariableContext ctx, object[] _)
    {
        string phrase = DummyGrammar.MakeTitleCase(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(phrase);
        return null;
    }

    /// <summary>
    /// Title cases Value but preserves leading article lowercase.
    /// Key: "titleCaseWithArticle".
    /// Faithful to PostProcessors.TitleCaseWithArticle.
    /// </summary>
    public static string? TitleCaseWithArticle(DummyVariableContext ctx, object[] _)
    {
        string phrase = DummyGrammar.MakeTitleCaseWithArticle(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(phrase);
        return null;
    }

    /// <summary>
    /// Lowercases first letter if Value starts with an article.
    /// Key: "initLowerIfArticle".
    /// Faithful to PostProcessors.InitLowerIfArticle.
    /// </summary>
    public static string? InitLowerIfArticle(DummyVariableContext ctx, object[] _)
    {
        string word = DummyGrammar.InitLowerIfArticle(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(word);
        return null;
    }

    /// <summary>
    /// Removes leading "the " (case-insensitive on 't').
    /// Key: "trimLeadingThe".
    /// Faithful to PostProcessors.TrimLeadingThe.
    /// </summary>
    public static string? TrimLeadingThe(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length > 4 &&
            char.ToLowerInvariant(ctx.Value[0]) == 't' &&
            ctx.Value[1] == 'h' && ctx.Value[2] == 'e' && ctx.Value[3] == ' ')
        {
            ctx.Value.Remove(0, 4);
        }

        return null;
    }

    /// <summary>
    /// Converts "a" to "an" where appropriate in Value.
    /// Keys: "scanForAn", "a.to.an".
    /// Faithful to PostProcessors.ScanForAn.
    /// </summary>
    public static string? ScanForAn(DummyVariableContext ctx, object[] _)
    {
        string value = DummyGrammar.ConvertAtoAn(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(value);
        return null;
    }

    /// <summary>
    /// Replaces " plant"/" tree" with " hedge".
    /// Key: "makeHedge".
    /// Faithful to PostProcessors.MakeHedge.
    /// </summary>
    public static string? MakeHedge(DummyVariableContext ctx, object[] _)
    {
        ctx.Value.Replace(" plant", "").Replace(" tree", "").Append(" hedge");
        return null;
    }

    /// <summary>
    /// Registers all English-specific postprocessors into <see cref="DummyVariableReplacers"/>.
    /// </summary>
    public static void RegisterAll()
    {
        DummyVariableReplacers.RegisterPost(["pluralize", "plural"], new DummyReplacerEntry(Pluralize, []));
        DummyVariableReplacers.RegisterPost(["article"], new DummyReplacerEntry(Article, []));
        DummyVariableReplacers.RegisterPost(["Article"], new DummyReplacerEntry(Article, [], capitalize: true));
        DummyVariableReplacers.RegisterPost(["possessive", "makePossessive", "'s"], new DummyReplacerEntry(Possessive, []));
        DummyVariableReplacers.RegisterPost("title", Title);
        DummyVariableReplacers.RegisterPost("titleCaseWithArticle", TitleCaseWithArticle);
        DummyVariableReplacers.RegisterPost("initLowerIfArticle", InitLowerIfArticle);
        DummyVariableReplacers.RegisterPost("trimLeadingThe", TrimLeadingThe);
        DummyVariableReplacers.RegisterPost(["scanForAn", "a.to.an"], new DummyReplacerEntry(ScanForAn, []));
        DummyVariableReplacers.RegisterPost("makeHedge", MakeHedge);
    }
}

#pragma warning restore CA1707
