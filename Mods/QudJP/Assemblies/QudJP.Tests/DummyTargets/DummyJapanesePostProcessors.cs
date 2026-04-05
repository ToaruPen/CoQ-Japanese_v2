#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Japanese-specific postprocessor overrides for the GameText variable expansion system.
/// These would be registered via <c>[HasVariableReplacer(Lang="ja")]</c> with <c>Override=true</c>
/// in the actual mod to replace the English defaults when ActiveLanguage=="ja".
/// <para>
/// Design principles:
/// - Japanese has no articles (a/an/the) → article postprocessors are no-ops
/// - Japanese has no singular/plural distinction → pluralize is no-op
/// - Japanese possessive uses 「の」 instead of 's/'
/// - Japanese has no title case → title/capitalize postprocessors are no-ops for CJK text
/// - Japanese has no a/an distinction → scanForAn is no-op
/// </para>
/// </summary>
internal static class DummyJapanesePostProcessors
{
    /// <summary>
    /// Japanese pluralize: no-op. Japanese does not distinguish singular/plural.
    /// Overrides: "pluralize", "plural".
    /// </summary>
    public static string? Pluralize(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese article: no-op. Japanese has no indefinite/definite articles.
    /// Overrides: "article", "Article".
    /// </summary>
    public static string? Article(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese possessive: appends 「の」.
    /// Overrides: "possessive", "makePossessive", "'s".
    /// </summary>
    public static string? Possessive(DummyVariableContext ctx, object[] _)
    {
        ctx.Value.Append('の');
        return null;
    }

    /// <summary>
    /// Japanese title case: no-op. CJK scripts have no case distinction.
    /// Override: "title".
    /// </summary>
    public static string? Title(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese titleCaseWithArticle: no-op.
    /// Override: "titleCaseWithArticle".
    /// </summary>
    public static string? TitleCaseWithArticle(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese initLowerIfArticle: no-op. No articles to detect.
    /// Override: "initLowerIfArticle".
    /// </summary>
    public static string? InitLowerIfArticle(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese trimLeadingThe: no-op. No "the" in Japanese.
    /// Override: "trimLeadingThe".
    /// </summary>
    public static string? TrimLeadingThe(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese scanForAn: no-op. No a/an distinction in Japanese.
    /// Override: "scanForAn", "a.to.an".
    /// </summary>
    public static string? ScanForAn(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese secondToThirdPerson: no-op.
    /// Override: "secondToThirdPerson".
    /// </summary>
    public static string? SecondToThirdPerson(DummyVariableContext ctx, object[] _) => null;

    /// <summary>
    /// Japanese makeHedge: adapts for Japanese plant/tree naming.
    /// Removes 「の木」「の草」 and appends 「の垣根」.
    /// Override: "makeHedge".
    /// </summary>
    public static string? MakeHedge(DummyVariableContext ctx, object[] _)
    {
        ctx.Value.Replace("の木", "").Replace("の草", "").Append("の垣根");
        return null;
    }

    /// <summary>
    /// Registers all Japanese-specific postprocessor overrides into
    /// <see cref="DummyVariableReplacers"/> with Override=true.
    /// Simulates a <c>[HasVariableReplacer(Lang="ja")]</c> class registration.
    /// </summary>
    public static void RegisterAll()
    {
        DummyVariableReplacers.RegisterPost(
            ["pluralize", "plural"],
            new DummyReplacerEntry(Pluralize, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["article"],
            new DummyReplacerEntry(Article, []),
            @override: true);
        DummyVariableReplacers.RegisterPost(
            ["Article"],
            new DummyReplacerEntry(Article, [], capitalize: true),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["possessive", "makePossessive", "'s"],
            new DummyReplacerEntry(Possessive, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["title"],
            new DummyReplacerEntry(Title, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["titleCaseWithArticle"],
            new DummyReplacerEntry(TitleCaseWithArticle, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["initLowerIfArticle"],
            new DummyReplacerEntry(InitLowerIfArticle, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["trimLeadingThe"],
            new DummyReplacerEntry(TrimLeadingThe, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["scanForAn", "a.to.an"],
            new DummyReplacerEntry(ScanForAn, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["secondToThirdPerson"],
            new DummyReplacerEntry(SecondToThirdPerson, []),
            @override: true);

        DummyVariableReplacers.RegisterPost(
            ["makeHedge"],
            new DummyReplacerEntry(MakeHedge, []),
            @override: true);
    }
}

#pragma warning restore CA1707
