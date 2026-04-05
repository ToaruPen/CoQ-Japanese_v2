#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 golden tests for Japanese-specific postprocessor overrides.
/// Verifies that Japanese postprocessors correctly override English defaults
/// and produce appropriate output for CJK text.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class JapanesePostProcessorTests
{
    private DummyVariableContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new DummyVariableContext();
    }

    private void SetValue(string text)
    {
        _ctx.Value.Clear();
        _ctx.Value.Append(text);
    }

    private string GetValue() => _ctx.Value.ToString();

    // --- Pluralize: no-op ---

    [Test]
    public void Pluralize_JapaneseText_ReturnsUnchanged()
    {
        SetValue("スナップジョー");
        DummyJapanesePostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("スナップジョー"));
    }

    [Test]
    public void Pluralize_EnglishText_StillUnchanged()
    {
        // Japanese pluralize is identity regardless of input language
        SetValue("snapjaw");
        DummyJapanesePostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("snapjaw"));
    }

    // --- Article: no-op ---

    [Test]
    public void Article_JapaneseText_NoArticlePrepended()
    {
        SetValue("スナップジョー");
        DummyJapanesePostProcessors.Article(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("スナップジョー"));
    }

    [Test]
    public void Article_Capitalized_StillNoArticle()
    {
        SetValue("スナップジョー");
        _ctx.Capitalize = true;
        DummyJapanesePostProcessors.Article(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("スナップジョー"));
    }

    // --- Possessive: の ---

    [Test]
    public void Possessive_JapaneseText_AppendsNo()
    {
        SetValue("戦士");
        DummyJapanesePostProcessors.Possessive(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("戦士の"));
    }

    [Test]
    public void Possessive_EnglishText_AppendsNo()
    {
        // Even English names get 「の」 in Japanese mode
        SetValue("Barathrum");
        DummyJapanesePostProcessors.Possessive(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Barathrumの"));
    }

    // --- Title: no-op ---

    [Test]
    public void Title_JapaneseText_ReturnsUnchanged()
    {
        SetValue("喰らう者の墓所");
        DummyJapanesePostProcessors.Title(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("喰らう者の墓所"));
    }

    // --- TitleCaseWithArticle: no-op ---

    [Test]
    public void TitleCaseWithArticle_ReturnsUnchanged()
    {
        SetValue("ある素晴らしいもの");
        DummyJapanesePostProcessors.TitleCaseWithArticle(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("ある素晴らしいもの"));
    }

    // --- InitLowerIfArticle: no-op ---

    [Test]
    public void InitLowerIfArticle_ReturnsUnchanged()
    {
        SetValue("ある場所");
        DummyJapanesePostProcessors.InitLowerIfArticle(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("ある場所"));
    }

    // --- TrimLeadingThe: no-op ---

    [Test]
    public void TrimLeadingThe_JapaneseText_ReturnsUnchanged()
    {
        SetValue("その洞窟");
        DummyJapanesePostProcessors.TrimLeadingThe(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("その洞窟"));
    }

    // --- ScanForAn: no-op ---

    [Test]
    public void ScanForAn_JapaneseText_ReturnsUnchanged()
    {
        SetValue("リンゴとバナナ");
        DummyJapanesePostProcessors.ScanForAn(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("リンゴとバナナ"));
    }

    // --- MakeHedge ---

    [Test]
    public void MakeHedge_RemovesTreeAndAppendsHedge()
    {
        SetValue("オークの木");
        DummyJapanesePostProcessors.MakeHedge(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("オークの垣根"));
    }

    [Test]
    public void MakeHedge_RemovesPlantAndAppendsHedge()
    {
        SetValue("野生の草");
        DummyJapanesePostProcessors.MakeHedge(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("野生の垣根"));
    }

    // --- Override integration: en → ja pipeline ---

    [Test]
    public void Override_JaPluralizeReplacesEnPluralize_InPipeline()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("greeting", (_, _) => "snapjaw");

        // Register English first, then Japanese override
        DummyEnglishPostProcessors.RegisterAll();
        DummyJapanesePostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        // With ja override, pluralize should be no-op
        string result = DummyGameText.Process("=greeting|pluralize=");
        Assert.That(result, Is.EqualTo("snapjaw"));
    }

    [Test]
    public void Override_JaArticleReplacesEnArticle_InPipeline()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("greeting", (_, _) => "スナップジョー");

        DummyEnglishPostProcessors.RegisterAll();
        DummyJapanesePostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        // With ja override, article should not prepend "a"/"an"
        string result = DummyGameText.Process("=greeting|article=");
        Assert.That(result, Is.EqualTo("スナップジョー"));
    }

    [Test]
    public void Override_JaPossessiveReplacesEnPossessive_InPipeline()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("name", (_, _) => "戦士");

        DummyEnglishPostProcessors.RegisterAll();
        DummyJapanesePostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        // With ja override, possessive should append の instead of 's
        string result = DummyGameText.Process("=name|possessive=");
        Assert.That(result, Is.EqualTo("戦士の"));
    }

    // --- En-only pipeline (no ja override) ---

    [Test]
    public void EnOnly_PluralizeStillWorks_WithoutJaOverride()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("name", (_, _) => "box");

        // Only English registered — no Japanese override
        DummyEnglishPostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=name|pluralize=");
        Assert.That(result, Is.EqualTo("boxes"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
