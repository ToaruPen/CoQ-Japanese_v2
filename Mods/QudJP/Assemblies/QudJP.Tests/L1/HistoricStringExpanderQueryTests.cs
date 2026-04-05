#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class HistoricStringExpanderQueryTests
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
        DummyPostProcessors.RegisterAll();
        DummyEnglishPostProcessors.RegisterAll();
        DummySpiceContextDelegates.Register();
        DummyVariableReplacers.FinalizeInit();
        DummyHistoricSpice.Reset();
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
        DummyHistoricSpice.Reset();
    }

    [Test]
    public void SimpleSpiceQuery_UsesDeterministicFixtureRandom()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = Render("=spice:jewelry.!random=", history);

        Assert.That(result, Is.EqualTo("tiara"));
    }

    [Test]
    public void CookingQuery_UsesDeterministicFixtureRandom()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = Render("=spice:cooking.ate.!random=", history);

        Assert.That(result, Is.EqualTo("You eat the meal."));
    }

    [Test]
    public void EntityPropertyExpand_ResolvesEntityRandomListIntoSpiceTree()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.listProperties["elements"] = ["glass"];

        string result = Render("trapped in =spice:elements.entity@elements[random].nouns.!random|article=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("trapped in a mirror"));
    }

    [Test]
    public void EntityPropertyExpand_MultiElementGoldenCase_MatchesDecompiledHistoryTests()
    {
        DummyHistoricSpice.Init(new Dictionary<string, object>(StringComparer.Ordinal)
        {
            ["elements"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["glass"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["glass"] = string.Empty,
                    },
                },
                ["ice"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["hourglass"] = string.Empty,
                    },
                },
                ["time"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["unused time noun"] = string.Empty,
                        ["moment in time chosen arbitrarily"] = string.Empty,
                        ["hourglass"] = string.Empty,
                        ["atomic clock"] = string.Empty,
                    },
                },
            },
        });

        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.listProperties["elements"] = ["glass", "ice", "time"];

        string result = Render(
            "=spice:elements.entity@elements[random].nouns.!random|article=, =spice:elements.entity@elements[random].nouns.!random|article=, =spice:elements.entity@elements[random].nouns.!random|article=, =spice:elements.entity@elements[random].nouns.!random|article=",
            history,
            entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("an atomic clock, an hourglass, a glass, a moment in time chosen arbitrarily"));
    }

    [Test]
    public void EntityPropertyAndVarsExpand_ProcessesNestedTokens()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.properties["possessivePronoun"] = "her";

        string result = Render(
            "=spice:history.gospels.LostItem.LateSultanate.!random=",
            history,
            entity.CurrentSnapshot,
            ("*item*", "silly item name"));

        Assert.That(result, Is.EqualTo("lost her prized silly item name while the townsfolk were busy restoring the glass light bulb"));
    }

    [Test]
    public void VariableOnlyExpand_UsesInjectedStarVariable()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = Render(
            "=spice:villages.mayor.reasonIBecame.!random=",
            history,
            null,
            ("*Activity*", "writing localization code"));

        Assert.That(result, Is.EqualTo("For years I spent my time writing localization code. I came upon this village and its inhabitants. They asked me to employ my magisterial skills and lead the village."));
    }

    [Test]
    public void SetAndExpand_ReusesCapturedNodeVariable()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = Render(
            "=spice.set:professions.@random:$randomProfession==spice:professions.$randomProfession.plural= met at =spice:professions.$randomProfession.guildhall|article=",
            history);

        Assert.That(result, Is.EqualTo("gladiators met at an arena"));
    }

    [Test]
    public void ExpandApi_MatchesDelegatePath()
    {
        DummyHistory history = new(0L, new Random(0));

        string expandResult = DummyHistoricStringExpander.Expand(history, "spice.jewelry.!random");
        string delegateResult = Render("=spice:jewelry.!random=", history);

        Assert.That(expandResult, Is.EqualTo(delegateResult));
    }

    [Test]
    public void OldAndNewFormats_ProduceSameProfessionExpansion()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.properties["organizingPrincipleType"] = "gladiator";

        const string legacy = "<$prof=entity.@organizingPrincipleType><spice.professions.$prof.plural>";
        const string rewritten = "=spice.set.entity:@organizingPrincipleType:$prof==spice:professions.$prof.plural=";

        string normalized = DummyHistoricStringExpander.CheckDeprecatedSpiceFormats(legacy);
        string result = Render(normalized, history, entity.CurrentSnapshot);

        Assert.Multiple(() =>
        {
            Assert.That(normalized, Is.EqualTo(rewritten));
            Assert.That(result, Is.EqualTo("gladiators"));
        });
    }

    [Test]
    public void SeededDeterminism_RepeatsAcrossFreshRuns()
    {
        string first = Render("=spice:elements.!random=", new DummyHistory(0L, new Random(0)));
        string second = Render("=spice:elements.!random=", new DummyHistory(0L, new Random(0)));

        Assert.Multiple(() =>
        {
            Assert.That(first, Is.EqualTo(second));
            Assert.That(first, Is.Not.Empty);
        });
    }

    [Test]
    public void MissingPath_ReturnsEmptyString()
    {
        DummyHistory history = new(0L, new Random(0));

        string result = Render("=spice:missing.branch.value=", history);

        Assert.That(result, Is.Empty);
    }

    [Test]
    public void DirectEntityRoot_WithoutEntity_ReturnsUnknownEntityMarker()
    {
        string result = Render("=spice.entity:name=", new DummyHistory(0L, new Random(0)));

        Assert.That(result, Is.EqualTo("<unknown entity>"));
    }

    [Test]
    public void DirectEntityRoot_WithExtraSegments_ReturnsUnknownFormatMarker()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.listProperties["elements"] = ["glass"];

        string result = Render("=spice.entity:elements.!random=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("<unknown format entity.elements.!random>"));
    }

    [Test]
    public void DirectEntityRoot_WithUndefinedList_ReturnsUndefinedListMarker()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);

        string result = Render("=spice.entity:elements[random]=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("<undefined entity list elements>"));
    }

    [Test]
    public void DirectEntityRoot_WithEmptyList_ReturnsEmptyListMarker()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.listProperties["elements"] = [];

        string result = Render("=spice.entity:elements[random]=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("<empty entity list elements>"));
    }

    [Test]
    public void DirectEntityRoot_WithMissingProperty_UsesUnknownDefault()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);

        string result = Render("=spice.entity:missingProperty=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("unknown"));
    }

    [Test]
    public void StructuredEntitySegment_WithMissingAtProperty_ReturnsUndefinedPropertyMarker()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);

        string result = Render("=spice:professions.entity@missing.plural=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("<undefined entity property @missing>"));
    }

    [Test]
    public void StructuredEntitySegment_WithIndexedAtProperty_FallsBackToScalarProperty()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.properties["organizingPrincipleType"] = "gladiator";

        string result = Render("=spice:professions.entity@organizingPrincipleType[random].plural=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("gladiators"));
    }

    [Test]
    public void StructuredEntitySegment_PropNotInRenamedEntityKeys_ReturnsUndefinedMarker()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity entity = new(history);
        entity.CurrentSnapshot.properties["possessivePronoun"] = "her";

        string result = Render("=spice:professions.entity@possessivePronoun.plural=", history, entity.CurrentSnapshot);

        Assert.That(result, Is.EqualTo("<undefined entity property @possessivePronoun>"));
    }

    [Test]
    public void StructuredEntitySegment_NamedEntityLookup_UsesRequestedPhase3Behavior()
    {
        DummyHistory history = new(0L, new Random(0));
        DummyHistoricEntity mentor = new(history)
        {
            id = "mentor",
        };
        mentor.CurrentSnapshot.properties["organizingPrincipleType"] = "gladiator";

        string result = Render("=spice:professions.entity[mentor]@organizingPrincipleType.plural=", history);

        Assert.That(result, Is.EqualTo("gladiators"));
    }

    [Test]
    public void InternalExpandQuery_SinglePartAssignment_StoresIntoVars()
    {
        Dictionary<string, string> vars = new(StringComparer.Ordinal)
        {
            ["$value"] = "gladiator",
        };
        Dictionary<string, object> nodeVars = new(StringComparer.Ordinal);

        string result = DummyHistoricStringExpander.InternalExpandQuery(
            ["$saved=$value"],
            null,
            new DummyHistory(0L, new Random(0)),
            vars,
            nodeVars);

        Assert.Multiple(() =>
        {
            Assert.That(result, Is.Empty);
            Assert.That(vars["$saved"], Is.EqualTo("gladiator"));
        });
    }

    [Test]
    public void InternalExpandQuery_DollarRoot_TraversesNodeVariableSubtree()
    {
        Dictionary<string, object> nodeVars = new(StringComparer.Ordinal)
        {
            ["$root"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["branch"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["leaf"] = "resolved",
                },
            },
        };

        string result = DummyHistoricStringExpander.InternalExpandQuery(
            ["$root", "branch", "leaf"],
            null,
            new DummyHistory(0L, new Random(0)),
            new Dictionary<string, string>(StringComparer.Ordinal),
            nodeVars);

        Assert.That(result, Is.EqualTo("resolved"));
    }

    private static string Render(
        string template,
        DummyHistory history,
        DummyHistoricEntitySnapshot? entity = null,
        params (string key, string value)[] variables)
    {
        DummyReplaceBuilder builder = DummyReplaceBuilder.Create()
            .Start(template)
            .AddSpiceHistory(history);

        if (entity != null)
        {
            builder.AddSpiceEntity(entity);
        }

        foreach ((string key, string value) in variables)
        {
            builder.AddSpiceVariable(key, value);
        }

        return builder.ToString();
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
