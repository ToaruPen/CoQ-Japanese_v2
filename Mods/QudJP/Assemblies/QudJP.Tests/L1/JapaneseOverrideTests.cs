#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for Japanese dummy override registrations used to model
/// language-specific replacer and postprocessor behavior.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class JapaneseOverrideTests
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

    [Test]
    public void SecondToThirdPerson_IsRegisteredAfterJapaneseRegistration()
    {
        DummyJapanesePostProcessors.RegisterAll();

        Assert.That(DummyVariableReplacers.PostMap, Does.ContainKey("secondToThirdPerson"));
    }

    [Test]
    public void SecondToThirdPerson_DirectCall_ReturnsNullAndLeavesValueUnchanged()
    {
        DummyVariableContext ctx = new();
        ctx.Value.Append("You attack");

        string? result = DummyJapanesePostProcessors.SecondToThirdPerson(ctx, []);

        Assert.That(result, Is.Null);
        Assert.That(ctx.Value.ToString(), Is.EqualTo("You attack"));
    }

    [Test]
    public void Adjectify_IsRegisteredInMapAfterBaseAndJapaneseOverrideRegistration()
    {
        DummyVariableReplacers.Register(
            ["adjectify"],
            new DummyReplacerEntry((_, args) => $"{args[0]}-english", [typeof(string)]));

        DummyJapaneseStringReplacers.RegisterAll();

        Assert.That(DummyVariableReplacers.Map, Does.ContainKey("adjectify"));
    }

    [Test]
    public void Adjectify_Override_ReturnsInputUnchanged()
    {
        DummyVariableReplacers.Register(
            ["adjectify"],
            new DummyReplacerEntry((_, args) => $"{args[0]}-english", [typeof(string)]));
        DummyJapaneseStringReplacers.RegisterAll();

        DummyVariableContext ctx = new();
        ctx.Value.Append("ignored");

        bool found = DummyVariableReplacers.Map["adjectify"].TryFind(["swift"], out DummyReplacerEntry entry);

        Assert.That(found, Is.True);
        Assert.That(entry.Delegate(ctx, ["swift"]), Is.EqualTo("swift"));
    }

    [Test]
    public void Process_AdjectifyOverride_KeepsInputUnchanged()
    {
        DummyVariableReplacers.Register(
            ["adjectify"],
            new DummyReplacerEntry((_, args) => $"{args[0]}-english", [typeof(string)]));
        DummyJapaneseStringReplacers.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=adjectify#subject=", arguments: ["swift"]);

        Assert.That(result, Is.EqualTo("swift"));
    }

    [Test]
    public void Process_AdjectifyWithoutJapaneseOverride_UsesEnglishBehavior()
    {
        DummyVariableReplacers.Register(
            ["adjectify"],
            new DummyReplacerEntry((_, args) => $"{args[0]}-english", [typeof(string)]));
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=adjectify#subject=", arguments: ["swift"]);

        Assert.That(result, Is.EqualTo("swift-english"));
    }

    [Test]
    public void Process_SecondToThirdPersonOverride_KeepsEnglishTextUnchanged()
    {
        DummyVariableReplacers.Register("line", (_, _) => "You attack");
        DummyVariableReplacers.RegisterPost(
            ["secondToThirdPerson"],
            new DummyReplacerEntry(
                (ctx, _) =>
                {
                    ctx.Value.Clear();
                    ctx.Value.Append("He attacks");
                    return null;
                },
                []));

        DummyEnglishPostProcessors.RegisterAll();
        DummyJapanesePostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=line|secondToThirdPerson=");

        Assert.That(result, Is.EqualTo("You attack"));
    }

    [Test]
    public void Process_SecondToThirdPersonWithoutJapaneseOverride_UsesEnglishBehavior()
    {
        DummyVariableReplacers.Register("line", (_, _) => "You attack");
        DummyVariableReplacers.RegisterPost(
            ["secondToThirdPerson"],
            new DummyReplacerEntry(
                (ctx, _) =>
                {
                    ctx.Value.Clear();
                    ctx.Value.Append("He attacks");
                    return null;
                },
                []));

        DummyEnglishPostProcessors.RegisterAll();
        DummyVariableReplacers.FinalizeInit();

        string result = DummyGameText.Process("=line|secondToThirdPerson=");

        Assert.That(result, Is.EqualTo("He attacks"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
