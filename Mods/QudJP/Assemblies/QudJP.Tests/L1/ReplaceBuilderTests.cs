#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for DummyReplaceBuilder — faithful reproduction of
/// XRL.World.Text.ReplaceBuilder fluent API (212.17).
/// Tests cover: Start, AddNoun, AddArgument, AddAlias, InsertArgument,
/// StripColors, ForceThirdPerson, Color wrapping, and ToString pipeline.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class ReplaceBuilderTests
{
    [SetUp]
    public void SetUp()
    {
        DummyVariableReplacers.Reset();

        // "toString" — default rack, returns argument.ToString()
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry(
                (ctx, args) => args.Length > 0 ? args[0]?.ToString() : null,
                [typeof(object)]));

        // "greeting" — simple replacer
        DummyVariableReplacers.Register("greeting", (_, _) => "hello");

        // "name" — returns Default (Capitalization-aware)
        DummyVariableReplacers.Register(
            ["name"],
            new DummyReplacerEntry(
                (ctx, _) => ctx.Default ?? "thing",
                [],
                @default: "thing"));

        DummyPostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
    }

    // --- Start + ToString ---

    [Test]
    public void Start_WithString_ProcessesAndReturns()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("Say =greeting=!")
            .ToString();

        Assert.That(result, Is.EqualTo("Say hello!"));
    }

    [Test]
    public void Start_PlainText_ReturnsUnchanged()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("No variables here")
            .ToString();

        Assert.That(result, Is.EqualTo("No variables here"));
    }

    // --- AddNoun ---

    [Test]
    public void AddNoun_AddsGenderedNounAsSubject()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject=")
            .AddNoun("snapjaw scavenger")
            .ToString();

        // subject → index 0 → DummyGenderedNoun.ToString() → "snapjaw scavenger (neuter)"
        Assert.That(result, Is.EqualTo("snapjaw scavenger (neuter)"));
    }

    [Test]
    public void AddNoun_PluralFlag_SetsPronounType()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject=")
            .AddNoun("seekers", plural: true)
            .ToString();

        Assert.That(result, Is.EqualTo("seekers (plural)"));
    }

    [Test]
    public void AddNoun_NullName_SkipsArgument()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject=")
            .AddNoun(null)
            .AddNoun("fallback")
            .ToString();

        // null is skipped, "fallback" becomes index 0 (subject)
        Assert.That(result, Is.EqualTo("fallback (neuter)"));
    }

    [Test]
    public void AddNoun_WithAlias_RegistersAlias()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=attacker=")
            .AddNoun("snapjaw", alias: "attacker")
            .ToString();

        Assert.That(result, Is.EqualTo("snapjaw (neuter)"));
    }

    // --- AddArgument ---

    [Test]
    public void AddArgument_StringArgument_ResolvableAsSubject()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject=")
            .AddArgument("water")
            .ToString();

        Assert.That(result, Is.EqualTo("water"));
    }

    [Test]
    public void AddArgument_WithAlias_RegistersAlias()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=item=")
            .AddArgument("dagger", alias: "item")
            .ToString();

        Assert.That(result, Is.EqualTo("dagger"));
    }

    [Test]
    public void AddArgument_NullArgument_SkipsQuietly()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject=")
            .AddArgument(null, silent: true)
            .AddArgument("backup")
            .ToString();

        Assert.That(result, Is.EqualTo("backup"));
    }

    // --- AddAlias ---

    [Test]
    public void AddAlias_BindsNameToIndex()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=target=")
            .AddArgument("first")
            .AddArgument("second")
            .AddAlias("target", 1)
            .ToString();

        Assert.That(result, Is.EqualTo("second"));
    }

    // --- InsertArgument ---

    [Test]
    public void InsertArgument_ShiftsExistingAliases()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=target=")
            .AddArgument("original", alias: "target")
            .InsertArgument(0, "inserted")
            .ToString();

        // "target" was at index 0, insert shifts it to index 1
        Assert.That(result, Is.EqualTo("original"));
    }

    // --- StripColors ---

    [Test]
    public void StripColors_RemovesMarkupFromOutput()
    {
        // Register a replacer that returns colored text
        DummyVariableReplacers.Register("colored", (_, _) => "{{r|red text}}");

        string result = DummyReplaceBuilder.Create()
            .Start("=colored=")
            .StripColors()
            .ToString();

        Assert.That(result, Is.EqualTo("red text"));
    }

    // --- Color wrapping ---

    [Test]
    public void Color_WrapsOutputInColorTag()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=greeting=")
            .Color("G")
            .ToString();

        Assert.That(result, Is.EqualTo("{{G|hello}}"));
    }

    [Test]
    public void Color_StripsAmpersandPrefix()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=greeting=")
            .Color("&G")
            .ToString();

        Assert.That(result, Is.EqualTo("{{G|hello}}"));
    }

    // --- ForceThirdPerson ---

    [Test]
    public void ForceThirdPerson_SetsFlag()
    {
        DummyReplaceBuilder builder = DummyReplaceBuilder.Create()
            .Start("text")
            .ForceThirdPerson();

        Assert.That(builder.IsThirdPersonForced, Is.True);
    }

    // --- HasArgument ---

    [Test]
    public void HasArgument_ReturnsTrueForAddedArgument()
    {
        object arg = "test";
        DummyReplaceBuilder builder = DummyReplaceBuilder.Create()
            .Start("text")
            .AddArgument(arg);

        Assert.That(builder.HasArgument(arg), Is.True);
    }

    [Test]
    public void HasArgument_ReturnsFalseForMissingArgument()
    {
        DummyReplaceBuilder builder = DummyReplaceBuilder.Create()
            .Start("text");

        Assert.That(builder.HasArgument("missing"), Is.False);
    }

    // --- Chaining: subject + object ---

    [Test]
    public void Chaining_SubjectAndObject_BothResolvable()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=subject= hits =object=")
            .AddNoun("warrior")
            .AddNoun("goblin")
            .ToString();

        Assert.That(result, Is.EqualTo("warrior (neuter) hits goblin (neuter)"));
    }

    // --- Pipe postprocessor through builder ---

    [Test]
    public void Integration_PostProcessorThroughBuilder()
    {
        string result = DummyReplaceBuilder.Create()
            .Start("=greeting|capitalize=")
            .ToString();

        Assert.That(result, Is.EqualTo("Hello"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
