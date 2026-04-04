#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class StringsLookupTests
{
    [Test]
    public void TryGetString_GlobalHit_ReturnsValueAndTrue()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(null, "hello", "world", null));

        bool found = strings.TryGetString(null, "hello", out string? result);

        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void TryGetString_Miss_ReturnsFalseAndPassthroughID()
    {
        DummyStrings strings = CreateStringsWith(static _ => { });

        bool found = strings.TryGetString(null, "missing", out string? result);

        Assert.That(found, Is.False);
        Assert.That(result, Is.EqualTo("missing"));
    }

    [Test]
    public void TryGetString_ContextHit_ReturnsContextValue()
    {
        DummyStrings strings = CreateStringsWith(loader =>
        {
            loader.HandleStringEntry(null, "id", "global", null);
            loader.HandleStringEntry("ctx", "id", "context", null);
        });

        bool found = strings.TryGetString("ctx", "id", out string? result);

        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo("context"));
    }

    [Test]
    public void TryGetString_ContextMiss_FallsBackToGlobal()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(null, "id", "global", null));

        bool found = strings.TryGetString("ctx", "id", out string? result);

        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo("global"));
    }

    [Test]
    public void TryGetString_ContextAndGlobalMiss_ReturnsID()
    {
        DummyStrings strings = CreateStringsWith(static _ => { });

        bool found = strings.TryGetString("ctx", "missing", out string? result);

        Assert.That(found, Is.False);
        Assert.That(result, Is.EqualTo("missing"));
    }

    [Test]
    public void TryGetString_NullContext_TreatedAsGlobal()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(null, "id", "value", null));

        bool found = strings.TryGetString(null, "id", out string? result);

        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void TryGetString_EmptyContext_TreatedAsGlobal()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(string.Empty, "id", "value", null));

        bool found = strings.TryGetString(string.Empty, "id", out string? result);

        Assert.That(found, Is.True);
        Assert.That(result, Is.EqualTo("value"));
    }

    [Test]
    public void S_SingleArg_ReturnsLookedUpValue()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(null, "id", "value", null));

        Assert.That(strings._S("id"), Is.EqualTo("value"));
    }

    [Test]
    public void S_SingleArg_MissReturnsID()
    {
        DummyStrings strings = CreateStringsWith(static _ => { });

        Assert.That(strings._S("missing"), Is.EqualTo("missing"));
    }

    [Test]
    public void S_ContextArg_ReturnsContextValue()
    {
        DummyStrings strings = CreateStringsWith(loader =>
        {
            loader.HandleStringEntry(null, "id", "global", null);
            loader.HandleStringEntry("ctx", "id", "context", null);
        });

        Assert.That(strings._S("ctx", "id"), Is.EqualTo("context"));
    }

    [Test]
    public void S_DebugEnabled_ReturnsPrefixedID()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry(null, "id", "value", null));
        strings.DebugEnabled = true;

        Assert.That(strings._S("id"), Is.EqualTo("_S:id"));
    }

    [Test]
    public void S_DebugEnabled_Context_ReturnsPrefixedContextAndID()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry("ctx", "id", "value", null));
        strings.DebugEnabled = true;

        Assert.That(strings._S("ctx", "id"), Is.EqualTo("_S:ctx:id"));
    }

    [Test]
    public void S_WithOrder_ReturnsOrderAdjust()
    {
        DummyStrings strings = CreateStringsWith(loader =>
        {
            loader.HandleStringEntry(null, "id", "global", 2);
            loader.HandleStringEntry("ctx", "id", "context", 8);
        });

        string result = strings._S("ctx", "id", 99, out int orderOut);

        Assert.That(result, Is.EqualTo("context"));
        Assert.That(orderOut, Is.EqualTo(8));
    }

    [Test]
    public void S_WithOrder_MissReturnsDefault()
    {
        DummyStrings strings = CreateStringsWith(static _ => { });

        string result = strings._S("ctx", "missing", 42, out int orderOut);

        Assert.That(result, Is.EqualTo("missing"));
        Assert.That(orderOut, Is.EqualTo(42));
    }

    [Test]
    public void S_WithOrder_DebugEnabled_ReturnsPrefixAndZeroOrder()
    {
        DummyStrings strings = CreateStringsWith(loader => loader.HandleStringEntry("ctx", "id", "value", 9));
        strings.DebugEnabled = true;

        string result = strings._S("ctx", "id", 99, out int orderOut);

        Assert.That(result, Is.EqualTo("_S:ctx:id"));
        Assert.That(orderOut, Is.Zero);
    }

    private static DummyStrings CreateStringsWith(Action<DummyStringsLoader> arrange)
    {
        DummyStringsLoader loader = new();
        arrange(loader);
        return new DummyStrings(loader);
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
