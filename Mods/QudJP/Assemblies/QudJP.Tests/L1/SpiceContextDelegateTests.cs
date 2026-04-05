#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for SpiceContext glue added in HistoricStringExpander phase 2.
/// Covers dummy HistoryKit surfaces, delegate registration, and builder integration.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class SpiceContextDelegateTests
{
    [SetUp]
    public void SetUp()
    {
        DummyVariableReplacers.Reset();
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry(
                (_, arguments) => arguments.Length > 0 ? arguments[0]?.ToString() : null,
                [typeof(object)]));
        DummySpiceContextDelegates.Register();
        DummyVariableReplacers.FinalizeInit();
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
    }

    [Test]
    public void DummySpiceContext_ConstructedFromHistory_ProxiesRandom()
    {
        DummyHistory history = new(0L, new Random(0))
        {
            currentYear = 4242L,
        };

        DummySpiceContext context = new(history);
        Random replacement = new(7);

        Assert.That(context.History, Is.SameAs(history));
        Assert.That(context.Random, Is.SameAs(history.r));

        context.Random = replacement;

        Assert.That(history.r, Is.SameAs(replacement));
        Assert.That(context.Random, Is.SameAs(replacement));
    }

    [Test]
    public void DummySpiceContext_Setters_CreateStoresLazily()
    {
        DummySpiceContext context = new();

        context.SetVariable("title", "ignored");
        context.SetVariable("$title", "sultan");
        context.SetNodeVariable("$saved", "Resheph");

        Assert.That(context.Variables, Is.Not.Null);
        Assert.That(context.Variables, Does.ContainKey("$title"));
        Assert.That(context.Variables, Does.Not.ContainKey("title"));
        Assert.That(context.NodeVariables, Is.Not.Null);
        Assert.That(context.NodeVariables!["$saved"], Is.EqualTo("Resheph"));
    }

    [Test]
    public void ToString_WithParameter_RoutesThroughDelegateExpandQueryAndGameTextProcess()
    {
        DummyVariableContext context = new();
        context.Parameters.Add("greeting");
        context.Invocation.Arguments = ["salutations"];
        context.Invocation.Aliases = new Dictionary<string, int>(StringComparer.Ordinal)
        {
            ["name"] = 0,
        };

        DummySpiceContext spice = new(new DummyHistory(0L, new Random(0)));
        spice.SetNodeVariable("greeting", "=name=");

        string result = DummySpiceContextDelegates.ToString(context, spice);

        Assert.That(result, Is.EqualTo("salutations"));
    }

    [Test]
    public void AddSpiceEntity_AddsAliasUsableByBuilder()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history) { id = "sultan" };
        entity.CurrentSnapshot.properties["name"] = "Resheph";

        string result = DummyReplaceBuilder.Create()
            .Start("=spice.entity:name=")
            .AddSpiceEntity(entity.CurrentSnapshot)
            .ToString();

        Assert.That(result, Is.EqualTo("Resheph"));
    }

    [Test]
    public void AddSpiceHistory_AddsAliasUsableByBuilder()
    {
        DummyHistory history = new(0L, new Random(0))
        {
            currentYear = 77L,
        };

        string result = DummyReplaceBuilder.Create()
            .Start("=spice:currentYear=")
            .AddSpiceHistory(history)
            .ToString();

        Assert.That(result, Is.EqualTo("77"));
    }

    [Test]
    public void AddSpiceVariable_AugmentsExistingSpiceContextVariables()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = DummyReplaceBuilder.Create()
            .Start("=spice:$title=")
            .AddSpiceHistory(history)
            .AddSpiceVariable("$title", "Sultan")
            .ToString();

        Assert.That(result, Is.EqualTo("Sultan"));
    }

    [Test]
    public void HistoricStringExpander_ExpandHistoryQuery_RoutesThroughBuilderGlue()
    {
        DummyHistory history = new(0L, new Random(0))
        {
            currentYear = 77L,
        };

        string result = DummyHistoricStringExpander.Expand(history, "spice.currentYear");

        Assert.That(result, Is.EqualTo("77"));
    }

    [Test]
    public void HistoricStringExpander_ExpandWithoutHistory_UsesDefaultHistory()
    {
        string result = DummyHistoricStringExpander.Expand("spice.startingYear");

        Assert.That(result, Is.EqualTo("0"));
    }

    [Test]
    public void HistoricStringExpander_ExpandEntityQuery_UsesEntityHistory()
    {
        DummyHistory history = new(0L, new Random(0))
        {
            currentYear = 33L,
        };
        DummyHistoricEntity entity = new(history) { id = "sultan" };

        string result = DummyHistoricStringExpander.Expand(entity.CurrentSnapshot, "spice.currentYear");

        Assert.That(result, Is.EqualTo("33"));
    }

    [Test]
    public void HistoricStringExpander_ExpandWithHistoryAndEntity_PrefersExplicitHistory()
    {
        DummyHistory entityHistory = new(0L, new Random(0))
        {
            currentYear = 11L,
        };
        DummyHistory explicitHistory = new(0L, new Random(1))
        {
            currentYear = 22L,
        };
        DummyHistoricEntity entity = new(entityHistory) { id = "sultan" };

        string result = DummyHistoricStringExpander.Expand(explicitHistory, entity.CurrentSnapshot, "spice.currentYear");

        Assert.That(result, Is.EqualTo("22"));
    }

    [Test]
    public void AddSpiceVariable_StarWrappedDollarVariable_RewritesAndStoresVariable()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = DummyReplaceBuilder.Create()
            .Start("=spice:$itemType=")
            .AddSpiceHistory(history)
            .AddSpiceVariable("*itemType*", "relic")
            .ToString();

        Assert.That(result, Is.EqualTo("relic"));
    }

    [Test]
    public void AddSpiceVariable_DishStarVariable_StoresEmbeddedDishName()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = DummyReplaceBuilder.Create()
            .Start("=spice:$dishName=")
            .AddSpiceHistory(history)
            .AddSpiceVariable("*dish*", "stew")
            .ToString();

        Assert.That(result, Is.EqualTo("stew"));
    }

    [Test]
    public void AddSpiceHistory_ReusedBuilder_StartCreatesFreshSpiceContext()
    {
        DummyReplaceBuilder builder = DummyReplaceBuilder.Create();
        DummyHistory first = new(0L, new Random(0))
        {
            currentYear = 11L,
        };
        DummyHistory second = new(0L, new Random(1))
        {
            currentYear = 22L,
        };

        string firstResult = builder
            .Start("=spice:currentYear=")
            .AddSpiceHistory(first)
            .ToString();

        string secondResult = builder
            .Start("=spice:currentYear=")
            .AddSpiceHistory(second)
            .ToString();

        Assert.Multiple(() =>
        {
            Assert.That(firstResult, Is.EqualTo("11"));
            Assert.That(secondResult, Is.EqualTo("22"));
        });
    }

    [Test]
    public void Register_AddsExpectedKeys()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("toString"));
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("entity"));
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("set"));
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("set.entity"));
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("nameItemAdjectiveRoot"));
            Assert.That(DummyVariableReplacers.Map, Does.ContainKey("nameItemNounRoot"));
        });
    }

    [Test]
    public void Set_StoresNodeVariable()
    {
        DummyHistory history = new(0L, new Random(0))
        {
            currentYear = 101L,
        };
        DummySpiceContext spice = new(history);
        DummyVariableContext context = new();
        context.Parameters.Add("currentYear");
        context.Parameters.Add("$year");

        string result = DummySpiceContextDelegates.Set(context, spice);

        Assert.That(result, Is.Empty);
        Assert.That(spice.NodeVariables, Is.Not.Null);
        Assert.That(spice.NodeVariables!["$year"], Is.EqualTo("101"));
    }

    [Test]
    public void SetEntity_StoresNodeVariable()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history) { id = "sultan" };
        entity.CurrentSnapshot.properties["name"] = "Resheph";

        DummySpiceContext spice = new(entity.CurrentSnapshot);
        DummyVariableContext context = new();
        context.Parameters.Add("name");
        context.Parameters.Add("$entityName");

        string result = DummySpiceContextDelegates.SetEntity(context, spice);

        Assert.That(result, Is.Empty);
        Assert.That(spice.NodeVariables, Is.Not.Null);
        Assert.That(spice.NodeVariables!["$entityName"], Is.EqualTo("Resheph"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
