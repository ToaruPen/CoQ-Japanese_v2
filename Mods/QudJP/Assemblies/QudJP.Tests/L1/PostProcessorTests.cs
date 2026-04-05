#pragma warning disable CA1515
#pragma warning disable CA1707

using System.Text;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for DummyPostProcessors — faithful reproduction of language-independent
/// postprocessors from XRL.World.Text.Delegates.PostProcessors (212.17).
/// Golden test values taken from decompiled [VariablePostProcessorExample] attributes.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class PostProcessorTests
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

    // --- Capitalize ---

    [TestCase("snapjaw scavenger", "Snapjaw scavenger")]
    [TestCase("", "")]
    [TestCase("A", "A")]
    public void Capitalize_GoldenTests(string input, string expected)
    {
        SetValue(input);
        DummyPostProcessors.Capitalize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    // --- CapEachLine ---

    [Test]
    public void CapEachLine_CapitalizesEachLine()
    {
        SetValue("snapjaw scavenger\n   this too");
        DummyPostProcessors.CapEachLine(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Snapjaw scavenger\n   This too"));
    }

    // --- Lower ---

    [Test]
    public void Lower_LowercasesAllCharacters()
    {
        SetValue("Barathrum the Old");
        DummyPostProcessors.Lower(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("barathrum the old"));
    }

    // --- Upper ---

    [Test]
    public void Upper_UppercasesAllCharacters()
    {
        SetValue("Q Girl");
        DummyPostProcessors.Upper(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Q GIRL"));
    }

    // --- InitLower ---

    [Test]
    public void InitLower_LowercasesFirstCharOnly()
    {
        SetValue("Barathrum the Old");
        DummyPostProcessors.InitLower(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("barathrum the Old"));
    }

    // --- Strip ---

    [TestCase("{{rainbow|Q Girl}}", "Q Girl")]
    [TestCase("&YAsphodel", "Asphodel")]
    public void Strip_RemovesColorMarkup(string input, string expected)
    {
        SetValue(input);
        DummyPostProcessors.Strip(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    // --- Color ---

    [Test]
    public void Color_WithParameter_WrapsInColorTag()
    {
        SetValue("snapjaw scavenger");
        _ctx.Parameters = ["r"];
        DummyPostProcessors.Color(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("{{r|snapjaw scavenger}}"));
    }

    [Test]
    public void Color_WithTypedArgument_PrefersArgOverParam()
    {
        SetValue("snapjaw scavenger");
        _ctx.Parameters = ["r"];
        DummyPostProcessors.Color(_ctx, ["W"]);
        Assert.That(GetValue(), Is.EqualTo("{{W|snapjaw scavenger}}"));
    }

    [Test]
    public void Color_EmptyValue_NoWrapping()
    {
        SetValue("");
        _ctx.Parameters = ["r"];
        DummyPostProcessors.Color(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(""));
    }

    [Test]
    public void Color_NoColorSpecified_NoWrapping()
    {
        SetValue("snapjaw scavenger");
        DummyPostProcessors.Color(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("snapjaw scavenger"));
    }

    // --- Rules ---

    [Test]
    public void Rules_WrapsInRulesTag()
    {
        SetValue("snapjaw scavenger");
        DummyPostProcessors.Rules(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("{{rules|snapjaw scavenger}}"));
    }

    // --- ColorSafe ---

    [Test]
    public void ColorSafe_WrapsInEmptyColorTag()
    {
        SetValue("snapjaw scavenger");
        DummyPostProcessors.ColorSafe(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("{{|snapjaw scavenger}}"));
    }

    // --- SpaceAfter ---

    [Test]
    public void SpaceAfter_AppendsSpace()
    {
        SetValue("hello");
        DummyPostProcessors.SpaceAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("hello "));
    }

    [Test]
    public void SpaceAfter_NoSpaceAfterHyphen()
    {
        SetValue("well-");
        DummyPostProcessors.SpaceAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("well-"));
    }

    [Test]
    public void SpaceAfter_NoSpaceAfterWhitespace()
    {
        SetValue("hello ");
        DummyPostProcessors.SpaceAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("hello "));
    }

    [Test]
    public void SpaceAfter_EmptyValue_NoOp()
    {
        SetValue("");
        DummyPostProcessors.SpaceAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(""));
    }

    // --- SpaceAfterEvenIfHyphen ---

    [Test]
    public void SpaceAfterEvenIfHyphen_AddsSpaceAfterHyphen()
    {
        SetValue("well-");
        DummyPostProcessors.SpaceAfterEvenIfHyphen(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("well- "));
    }

    // --- ColonAfter ---

    [Test]
    public void ColonAfter_AppendsColon()
    {
        SetValue("Name");
        DummyPostProcessors.ColonAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Name:"));
    }

    [Test]
    public void ColonAfter_NoColonAfterWhitespace()
    {
        SetValue("Name ");
        DummyPostProcessors.ColonAfter(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Name "));
    }

    // --- SpaceBefore ---

    [Test]
    public void SpaceBefore_PrependsSpace()
    {
        SetValue("world");
        DummyPostProcessors.SpaceBefore(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(" world"));
    }

    [Test]
    public void SpaceBefore_NoSpaceBeforeWhitespace()
    {
        SetValue(" world");
        DummyPostProcessors.SpaceBefore(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(" world"));
    }

    // --- Trim ---

    [Test]
    public void Trim_RemovesLeadingAndTrailingWhitespace()
    {
        SetValue("  hello  ");
        DummyPostProcessors.Trim(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("hello"));
    }

    // --- Slim ---

    [Test]
    public void Slim_ReplacesDoubleSpaces()
    {
        SetValue("hello  world  !");
        DummyPostProcessors.Slim(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("hello world !"));
    }

    // --- RemoveLastPeriod ---

    [Test]
    public void RemoveLastPeriod_RemovesTrailingPeriod()
    {
        SetValue("The end.");
        DummyPostProcessors.RemoveLastPeriod(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("The end"));
    }

    [Test]
    public void RemoveLastPeriod_NoPeriod_NoOp()
    {
        SetValue("The end");
        DummyPostProcessors.RemoveLastPeriod(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("The end"));
    }

    // --- BeforeComma ---

    [Test]
    public void BeforeComma_TruncatesAtFirstComma()
    {
        SetValue("snapjaw scavenger, angry");
        DummyPostProcessors.BeforeComma(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("snapjaw scavenger"));
    }

    [Test]
    public void BeforeComma_NoComma_NoOp()
    {
        SetValue("snapjaw scavenger");
        DummyPostProcessors.BeforeComma(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("snapjaw scavenger"));
    }

    // --- Integration: PostProcessors through Process pipeline ---

    [Test]
    public void Integration_PostProcessorThroughPipeline()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("greeting", (_, _) => "hello world");
        DummyPostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=greeting|upper=");

        Assert.That(result, Is.EqualTo("HELLO WORLD"));
    }

    [Test]
    public void Integration_ChainedPostProcessors()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register("greeting", (_, _) => "  hello world  ");
        DummyPostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=greeting|trim|capitalize=");

        Assert.That(result, Is.EqualTo("Hello world"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
