#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for replacer discovery and registration mechanics.
/// Faithful to VariableReplacers.LoadReplacers / YieldMethods (212.17).
/// Tests cover: Lang filtering, Override behavior, DefaultRack setup,
/// multi-key registration, and EntryRack type dispatch priority.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class ReplacerDiscoveryTests
{
    [SetUp]
    public void SetUp()
    {
        DummyVariableReplacers.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
    }

    // --- DefaultRack ---

    [Test]
    public void FinalizeInit_SetsDefaultRackToToString()
    {
        // Register "toString" entry
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry((_, args) => args[0]?.ToString(), [typeof(object)]));
        DummyVariableReplacers.FinalizeInit();

        Assert.That(DummyVariableReplacers.DefaultRack, Is.Not.Null);
        Assert.That(DummyVariableReplacers.DefaultRack!.Key, Is.EqualTo("toString"));
    }

    [Test]
    public void FinalizeInit_WithoutToString_DefaultRackIsNull()
    {
        DummyVariableReplacers.Register("other", (_, _) => "x");
        DummyVariableReplacers.FinalizeInit();

        Assert.That(DummyVariableReplacers.DefaultRack, Is.Null);
    }

    [Test]
    public void FinalizeInit_SetsInitializedTrue()
    {
        DummyVariableReplacers.FinalizeInit();

        Assert.That(DummyVariableReplacers.Initialized, Is.True);
    }

    // --- Multi-key registration ---

    [Test]
    public void Register_MultipleKeys_AllKeysResolveSameEntry()
    {
        DummyReplacerEntry entry = new((_, _) => "result", []);
        DummyVariableReplacers.Register(["key1", "key2", "key3"], entry);

        Assert.That(DummyVariableReplacers.Map.ContainsKey("key1"), Is.True);
        Assert.That(DummyVariableReplacers.Map.ContainsKey("key2"), Is.True);
        Assert.That(DummyVariableReplacers.Map.ContainsKey("key3"), Is.True);
    }

    // --- Override behavior ---

    [Test]
    public void Register_WithOverrideTrue_ReplacesExistingEntry()
    {
        // Register en version
        DummyReplacerEntry enEntry = new((_, _) => "english", []);
        DummyVariableReplacers.Register(["name"], enEntry);

        // Register ja version with Override=true
        DummyReplacerEntry jaEntry = new((_, _) => "japanese", []);
        DummyVariableReplacers.Register(["name"], jaEntry, @override: true);

        // Should resolve to ja version
        DummyVariableReplacers.Map["name"].TryFind([], out var found);
        string? result = found.Delegate(new DummyVariableContext(), []);
        Assert.That(result, Is.EqualTo("japanese"));
    }

    [Test]
    public void Register_WithOverrideFalse_KeepsExistingEntry()
    {
        // Register en version
        DummyReplacerEntry enEntry = new((_, _) => "english", []);
        DummyVariableReplacers.Register(["name"], enEntry);

        // Register ja version without Override (default=false)
        DummyReplacerEntry jaEntry = new((_, _) => "japanese", []);
        DummyVariableReplacers.Register(["name"], jaEntry);

        // Should resolve to original en version (duplicate skipped)
        DummyVariableReplacers.Map["name"].TryFind([], out var found);
        string? result = found.Delegate(new DummyVariableContext(), []);
        Assert.That(result, Is.EqualTo("english"));
    }

    // --- Lang filtering simulation ---
    // In the game, YieldMethods filters by HasVariableReplacerAttribute.Lang.
    // We simulate this by conditionally registering based on "active language".

    [Test]
    public void LangFilter_EnReplacerLoaded_WhenActiveLangIsEn()
    {
        // Simulate LoadReplacers with activeLang="en"
        string activeLang = "en";
        RegisterWithLangFilter(activeLang);

        Assert.That(DummyVariableReplacers.Map.ContainsKey("langTest"), Is.True);
        DummyVariableReplacers.Map["langTest"].TryFind([], out var found);
        string? result = found.Delegate(new DummyVariableContext(), []);
        Assert.That(result, Is.EqualTo("en-value"));
    }

    [Test]
    public void LangFilter_JaReplacerNotLoaded_WhenActiveLangIsEn()
    {
        string activeLang = "en";
        RegisterWithLangFilter(activeLang);

        // ja-specific entry should not override
        DummyVariableReplacers.Map["langTest"].TryFind([], out var found);
        string? result = found.Delegate(new DummyVariableContext(), []);
        Assert.That(result, Is.Not.EqualTo("ja-value"));
    }

    [Test]
    public void LangFilter_JaReplacerOverrides_WhenActiveLangIsJa()
    {
        string activeLang = "ja";
        RegisterWithLangFilter(activeLang);

        DummyVariableReplacers.Map["langTest"].TryFind([], out var found);
        string? result = found.Delegate(new DummyVariableContext(), []);
        Assert.That(result, Is.EqualTo("ja-value"));
    }

    // --- EntryRack priority dispatch ---

    [Test]
    public void EntryRack_TypedEntry_MatchesByType()
    {
        DummyReplacerEntry stringEntry = new((_, _) => "string-match", [typeof(string)]);
        DummyReplacerEntry intEntry = new((_, _) => "int-match", [typeof(int)]);

        DummyVariableReplacers.Register(["typed"], stringEntry);
        DummyVariableReplacers.Register(["typed"], intEntry);

        DummyVariableReplacers.Map["typed"].TryFind(["hello"], out var found);
        string? result = found.Delegate(new DummyVariableContext(), ["hello"]);
        Assert.That(result, Is.EqualTo("string-match"));
    }

    [Test]
    public void EntryRack_DifferentArgCount_DispatchesCorrectly()
    {
        DummyReplacerEntry noArgEntry = new((_, _) => "no-arg", []);
        DummyReplacerEntry oneArgEntry = new((_, _) => "one-arg", [typeof(object)]);

        DummyVariableReplacers.Register(["dispatch"], noArgEntry);
        DummyVariableReplacers.Register(["dispatch"], oneArgEntry);

        DummyVariableReplacers.Map["dispatch"].TryFind([], out var foundNoArg);
        Assert.That(foundNoArg.Delegate(new DummyVariableContext(), []), Is.EqualTo("no-arg"));

        DummyVariableReplacers.Map["dispatch"].TryFind(["x"], out var foundOneArg);
        Assert.That(foundOneArg.Delegate(new DummyVariableContext(), ["x"]), Is.EqualTo("one-arg"));
    }

    // --- Reset ---

    [Test]
    public void Reset_ClearsAllState()
    {
        DummyVariableReplacers.Register("test", (_, _) => "x");
        DummyVariableReplacers.RegisterPost("post", (_, _) => null);
        DummyVariableReplacers.FinalizeInit();

        DummyVariableReplacers.Reset();

        Assert.That(DummyVariableReplacers.Map, Is.Empty);
        Assert.That(DummyVariableReplacers.PostMap, Is.Empty);
        Assert.That(DummyVariableReplacers.DefaultRack, Is.Null);
        Assert.That(DummyVariableReplacers.Initialized, Is.False);
    }

    /// <summary>
    /// Simulates lang-gated override registration order, NOT the reflection-based
    /// discovery itself. DummyTargets use manual registration (no reflection), so
    /// there is no attribute-driven <c>YieldMethods</c> path to exercise at L1.
    /// This helper validates that when registration order mimics
    /// <c>YieldMethods</c> filtering (base always loaded, ja only when active),
    /// the <c>EntryRack.TryAdd</c> Override mechanism produces correct results.
    /// Actual reflection-based discovery belongs in L2 integration testing.
    /// </summary>
    private static void RegisterWithLangFilter(string activeLang)
    {
        // Base game class: [HasVariableReplacer] (no Lang — always loaded)
        DummyReplacerEntry enEntry = new((_, _) => "en-value", []);
        DummyVariableReplacers.Register(["langTest"], enEntry);

        // Japanese class: [HasVariableReplacer(Lang="ja")] — only when activeLang=="ja"
        if (activeLang == "ja")
        {
            DummyReplacerEntry jaEntry = new((_, _) => "ja-value", []);
            DummyVariableReplacers.Register(["langTest"], jaEntry, @override: true);
        }
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
