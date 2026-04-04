#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class StringsLoaderDataTests
{
    [Test]
    public void HandleStringEntry_NoContext_StoresInGlobal()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry(null, "id", "value", null);

        Assert.That(loader.Strings["id"], Is.EqualTo("value"));
    }

    [Test]
    public void HandleStringEntry_WithContext_StoresInContextAndGlobal()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", "value", null);

        Assert.Multiple(() =>
        {
            Assert.That(loader.ContextStrings("ctx")["id"], Is.EqualTo("value"));
            Assert.That(loader.Strings["id"], Is.EqualTo("value"));
        });
    }

    [Test]
    public void HandleStringEntry_DuplicateID_GlobalKeepsFirst()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx1", "id", "first", null);
        loader.HandleStringEntry("ctx2", "id", "second", null);

        Assert.That(loader.Strings["id"], Is.EqualTo("first"));
    }

    [Test]
    public void HandleStringEntry_DuplicateID_SameContext_Overwrites()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", "first", null);
        loader.HandleStringEntry("ctx", "id", "second", null);

        Assert.That(loader.ContextStrings("ctx")["id"], Is.EqualTo("second"));
    }

    [Test]
    public void HandleStringEntry_WithOrderAdjust_StoresOrder()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", "value", 5);

        Assert.That(loader.ContextOrders("ctx")["id"], Is.EqualTo(5));
    }

    [Test]
    public void HandleStringEntry_OrderAdjust_StoresInSharedGlobalDictionaryForNullOrEmptyContext()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", "value", 7);
        loader.HandleStringEntry(string.Empty, "empty", "value", 9);
        loader.HandleStringEntry(null, "null", "value", 11);

        Assert.Multiple(() =>
        {
            Assert.That(loader.OrderAdjust["id"], Is.EqualTo(7));
            Assert.That(loader.OrderAdjust["empty"], Is.EqualTo(9));
            Assert.That(loader.OrderAdjust["null"], Is.EqualTo(11));
        });
    }

    [Test]
    public void HandleStringEntry_NoOrderAdjust_NotStored()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", "value", null);

        Assert.That(loader.ContextOrders("ctx").ContainsKey("id"), Is.False);
        Assert.That(loader.OrderAdjust.ContainsKey("id"), Is.False);
    }

    [Test]
    public void HandleStringEntry_EmptyValue_AllowsEmptyString()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", "id", string.Empty, null);

        Assert.That(loader.ContextStrings("ctx")["id"], Is.Empty);
        Assert.That(loader.Strings["id"], Is.Empty);
    }

    [Test]
    public void ContextStrings_NullOrEmpty_ReturnsSameGlobalInstance()
    {
        DummyStringsLoader loader = new();

        Dictionary<string, string> nullContext = loader.ContextStrings(null);
        Dictionary<string, string> emptyContext = loader.ContextStrings(string.Empty);

        Assert.That(nullContext, Is.SameAs(loader.Strings));
        Assert.That(emptyContext, Is.SameAs(loader.Strings));
    }

    [Test]
    public void ContextStrings_NewContext_CreatesAndCachesMap()
    {
        DummyStringsLoader loader = new();

        Dictionary<string, string> first = loader.ContextStrings("ctx");
        Dictionary<string, string> second = loader.ContextStrings("ctx");

        Assert.That(first, Is.SameAs(second));
        Assert.That(loader.CStrings["ctx"], Is.SameAs(first));
    }

    [Test]
    public void ContextOrders_NullOrEmpty_ReturnsSameGlobalInstance()
    {
        DummyStringsLoader loader = new();

        Dictionary<string, int> nullContext = loader.ContextOrders(null);
        Dictionary<string, int> emptyContext = loader.ContextOrders(string.Empty);

        Assert.That(nullContext, Is.SameAs(loader.OrderAdjust));
        Assert.That(emptyContext, Is.SameAs(loader.OrderAdjust));
    }

    [Test]
    public void ContextOrders_NewContext_CreatesAndCachesMap()
    {
        DummyStringsLoader loader = new();

        Dictionary<string, int> first = loader.ContextOrders("ctx");
        Dictionary<string, int> second = loader.ContextOrders("ctx");

        Assert.That(first, Is.SameAs(second));
        Assert.That(loader.COrders["ctx"], Is.SameAs(first));
    }

    [Test]
    public void HandleStringEntry_EmptyID_SkipsGlobalTryAdd()
    {
        DummyStringsLoader loader = new();

        loader.HandleStringEntry("ctx", string.Empty, "value", null);

        Assert.That(loader.Strings.ContainsKey(string.Empty), Is.False);
        Assert.That(loader.ContextStrings("ctx")[string.Empty], Is.EqualTo("value"));
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
