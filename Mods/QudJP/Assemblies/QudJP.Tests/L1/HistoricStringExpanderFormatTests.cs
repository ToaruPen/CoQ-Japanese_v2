#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for the format-conversion surface of HistoricStringExpander
/// from decompiled beta source (212.17), focused on
/// CheckDeprecatedSpiceFormats (line 472) and ReformatStarVars (line 537).
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class HistoricStringExpanderFormatTests
{
    private static void AssertAngleQueryRewrite(string input, string expected)
    {
        AssertCheckDeprecatedRewrite(input, expected);
    }

    private static void AssertSetFormatRewrite(string input, string expected)
    {
        AssertCheckDeprecatedRewrite(input, expected);
    }

    private static void AssertPostRewrite(string input, string expected)
    {
        AssertCheckDeprecatedRewrite(input, expected);
    }

    private static void AssertFixedReplacementRewrite(string input, string expected)
    {
        AssertCheckDeprecatedRewrite(input, expected);
    }

    private static void AssertSpecialStarVarRewrite(string input, string expected)
    {
        AssertReformatStarVar(input, expected);
    }

    private static void AssertGenericStarVarRewrite(string input, string expected)
    {
        AssertReformatStarVar(input, expected);
    }

    private static void AssertCheckDeprecatedRewrite(string input, string expected)
    {
        string result = DummyHistoricStringExpander.CheckDeprecatedSpiceFormats(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    private static void AssertReformatStarVar(string input, string expected)
    {
        string result = DummyHistoricStringExpander.ReformatStarVars(input);

        Assert.That(result, Is.EqualTo(expected));
    }

    [Test]
    public void CheckDeprecatedSpiceFormats_NoLegacyMarkers_ReturnsUnchanged()
    {
        const string input = "No legacy formatting here.";

        AssertCheckDeprecatedRewrite(input, input);
    }

    [TestCase("<spice.query>", "=spice:query=")]
    [TestCase("<entity.query>", "=spice.entity:query=")]
    [TestCase("<^.query>", "=^:query=")]
    public void CheckDeprecatedSpiceFormats_LegacyAngleQueries_RewritesTokens(string input, string expected)
    {
        AssertAngleQueryRewrite(input, expected);
    }

    [TestCase("<$var=spiceval>", "=spice.set:val:$var=")]
    [TestCase("<$var=entity.val>", "=spice.set.entity:val:$var=")]
    public void CheckDeprecatedSpiceFormats_LegacySetFormats_RewritesTokens(string input, string expected)
    {
        AssertSetFormatRewrite(input, expected);
    }

    [TestCase("<spice.query.pluralize>", "=spice:query|pluralize=")]
    [TestCase("<spice.query.capitalize>", "=spice:query|capitalize=")]
    [TestCase("<spice.query.article>", "=spice:query|article=")]
    [TestCase("<spice.query.pluralize.capitalize.article>", "=spice:query|pluralize|capitalize|article=")]
    public void CheckDeprecatedSpiceFormats_PostFormats_RewritesPipePosts(string input, string expected)
    {
        AssertPostRewrite(input, expected);
    }

    [Test]
    public void CheckDeprecatedSpiceFormats_GenericStarVar_RewritesViaReformatStarVars()
    {
        const string input = "*starVar*";
        const string expected = "=starVar=";

        AssertCheckDeprecatedRewrite(input, expected);
    }

    [TestCase("*itemType*", "$itemType")]
    [TestCase("*element*", "$element")]
    [TestCase("*creatureNamePossessive*", "=creature.a.name's|strip=")]
    [TestCase("*adj.cap*", "=adj|title=")]
    [TestCase("*itemName.cap*", "=itemName|title=")]
    [TestCase("*dish*", "=spice:cooking.recipeNames.categorizedFoods.$dishName.!random=")]
    [TestCase("*descriptor.possessive*", "=descriptor|'s=")]
    public void ReformatStarVars_SpecialCases_ReturnsGoldenValue(string input, string expected)
    {
        AssertSpecialStarVarRewrite(input, expected);
    }

    [Test]
    public void CheckDeprecatedSpiceFormats_EmoteMarkup_SkipsStarVarRewrite()
    {
        const string input = "{{emote|*creatureNamePossessive*}}";
        const string expected = "{{emote|*creatureNamePossessive*}}";

        AssertCheckDeprecatedRewrite(input, expected);
    }

    [TestCase("@item.a", "=item.a=")]
    [TestCase("@item.name", "=item.name|strip=")]
    [TestCase("$focus", "=focus=")]
    [TestCase("$markovTitle", "=MARKOVTITLE=")]
    [TestCase("$workshop", "=mapnote.text|initLower|colorSafe=")]
    [TestCase("$location", "=mapnote.location=")]
    public void CheckDeprecatedSpiceFormats_FixedReplacements_RewritesKnownTokens(string input, string expected)
    {
        AssertFixedReplacementRewrite(input, expected);
    }

    [TestCase("x", "x")]
    [TestCase("starVar", "starVar")]
    [TestCase("*Star.Var:Pipe|Hash#*", "=star-Var-Pipe-Hash-=")]
    public void ReformatStarVars_GenericAndInvalidInputs_ReturnsExpectedValue(string input, string expected)
    {
        AssertGenericStarVarRewrite(input, expected);
    }

    [Test]
    public void CheckDeprecatedSpiceFormats_MixedLegacyPatterns_RewritesEntireString()
    {
        const string input = "<$prof=entity.@organizingPrincipleType><spice.professions.$prof.plural> *creatureNamePossessive* @item.a $focus $markovTitle @item.name $workshop $location";
        const string expected = "=spice.set.entity:@organizingPrincipleType:$prof==spice:professions.$prof.plural= =creature.a.name's|strip= =item.a= =focus= =MARKOVTITLE= =item.name|strip= =mapnote.text|initLower|colorSafe= =mapnote.location=";

        AssertCheckDeprecatedRewrite(input, expected);
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
