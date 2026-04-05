#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests verifying TextFilters compatibility with the postprocessor pipeline.
/// The obsolete TextFilters class (XRL.Language.TextFilters) delegates all calls
/// through the GameText template system: <c>"text".StartReplace().AddArgument(text).ToString()</c>
/// which routes through <c>=text|filterKey=</c> postprocessors.
/// <para>
/// These tests verify that flavor-filter postprocessors (corvid, angry, fish, etc.)
/// can be registered and dispatched through the same pipeline, confirming that the
/// obsolete TextFilters → postprocessor delegation path is functional.
/// </para>
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class TextFiltersCompatTests
{
    [SetUp]
    public void SetUp()
    {
        DummyVariableReplacers.Reset();

        // Register a simple source replacer
        DummyVariableReplacers.Register("text", (_, _) => "the quick brown fox");

        // toString for subject resolution
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry((_, args) => args.Length > 0 ? args[0]?.ToString() : null, [typeof(object)]));

        // Register stub flavor-filter postprocessors (simplified reproductions)
        RegisterFlavorFilters();

        DummyVariableReplacers.FinalizeInit();
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
    }

    /// <summary>
    /// Registers simplified stub versions of TextFilters flavor postprocessors.
    /// In the actual game, these perform complex text transformations (adding animal sounds,
    /// mangling text, etc.). For compatibility testing, we verify registration and dispatch.
    /// </summary>
    private static void RegisterFlavorFilters()
    {
        // Corvid: in game, inserts "caw" sounds. Stub: wraps in [corvid]
        DummyVariableReplacers.RegisterPost("corvid",
            (ctx, _) => { ctx.Value.Insert(0, "[corvid:"); ctx.Value.Append(']'); return null; });

        // Angry: in game, uppercases and adds exclamation. Stub: wrap in [angry]
        DummyVariableReplacers.RegisterPost("angry",
            (ctx, _) => { ctx.Value.Insert(0, "[angry:"); ctx.Value.Append(']'); return null; });

        // Fish: in game, replaces words with fish references. Stub: wrap
        DummyVariableReplacers.RegisterPost("fish",
            (ctx, _) => { ctx.Value.Insert(0, "[fish:"); ctx.Value.Append(']'); return null; });

        // Frog: in game, replaces words with frog references. Stub: wrap
        DummyVariableReplacers.RegisterPost("frog",
            (ctx, _) => { ctx.Value.Insert(0, "[frog:"); ctx.Value.Append(']'); return null; });

        // WaterBird: in game, adds bird sounds. Stub: wrap
        DummyVariableReplacers.RegisterPost("waterbird",
            (ctx, _) => { ctx.Value.Insert(0, "[waterbird:"); ctx.Value.Append(']'); return null; });

        // CrypticMachine: in game, encrypts text. Stub: wrap
        DummyVariableReplacers.RegisterPost("crypticmachine",
            (ctx, _) => { ctx.Value.Insert(0, "[crypticmachine:"); ctx.Value.Append(']'); return null; });

        // Stutterize: in game, adds stuttering. Stub: wrap
        DummyVariableReplacers.RegisterPost("stutterize",
            (ctx, _) => { ctx.Value.Insert(0, "[stutterize:"); ctx.Value.Append(']'); return null; });

        // Obfuscate: in game, replaces chars with random. Stub: wrap
        DummyVariableReplacers.RegisterPost("obfuscate",
            (ctx, _) => { ctx.Value.Insert(0, "[obfuscate:"); ctx.Value.Append(']'); return null; });
    }

    // --- TextFilters delegation: =text|filterKey= pattern ---

    [Test]
    public void Corvid_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|corvid=");
        Assert.That(result, Is.EqualTo("[corvid:the quick brown fox]"));
    }

    [Test]
    public void Angry_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|angry=");
        Assert.That(result, Is.EqualTo("[angry:the quick brown fox]"));
    }

    [Test]
    public void Fish_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|fish=");
        Assert.That(result, Is.EqualTo("[fish:the quick brown fox]"));
    }

    [Test]
    public void Frog_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|frog=");
        Assert.That(result, Is.EqualTo("[frog:the quick brown fox]"));
    }

    [Test]
    public void WaterBird_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|waterbird=");
        Assert.That(result, Is.EqualTo("[waterbird:the quick brown fox]"));
    }

    [Test]
    public void CrypticMachine_DispatchesThroughPipeline()
    {
        string result = DummyGameText.Process("=text|crypticmachine=");
        Assert.That(result, Is.EqualTo("[crypticmachine:the quick brown fox]"));
    }

    // --- Chained flavor filters ---

    [Test]
    public void ChainedFilters_ApplyInOrder()
    {
        string result = DummyGameText.Process("=text|corvid|angry=");
        Assert.That(result, Is.EqualTo("[angry:[corvid:the quick brown fox]]"));
    }

    // --- TextFilters with argument (StartReplace pattern) ---

    [Test]
    public void ArgumentPassthrough_TextFiltersPattern()
    {
        // The TextFilters.Corvid(text) pattern is:
        //   "=text|corvid=".StartReplace().AddArgument(text, "text").ToString()
        // which expands to: resolve "text" alias → argument → toString → then |corvid
        List<object> args = ["custom text"];
        Dictionary<string, int> aliases = new() { ["text"] = 0 };

        // Use the same =text|corvid= pattern but with custom argument
        // First register "text" as an alias-resolvable key
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry((_, a) => a.Length > 0 ? a[0]?.ToString() : null, [typeof(object)]));
        RegisterFlavorFilters();
        DummyVariableReplacers.FinalizeInit();

        // =text|corvid= where "text" resolves to alias → args[0] → toString → corvid
        string result = DummyGameText.Process("=text|corvid=", arguments: args, aliases: aliases);
        Assert.That(result, Is.EqualTo("[corvid:custom text]"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
