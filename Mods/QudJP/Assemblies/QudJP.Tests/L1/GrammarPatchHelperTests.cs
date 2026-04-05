#pragma warning disable CA1515
#pragma warning disable CA1707

using System.Text;
using QudJP.Patches;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for <see cref="GrammarPatchHelpers"/>.
/// Verifies the pure Japanese grammar helper behavior required for Phase 1.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class GrammarPatchHelperTests
{
    [Test]
    public void JaPluralizeResult_JapaneseActive_ReturnsInput()
    {
        Assert.That(GrammarPatchHelpers.JaPluralizeResult("sword", isJa: true), Is.EqualTo("sword"));
    }

    [Test]
    public void JaArticleResult_JapaneseActive_ReturnsInput()
    {
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: false, isJa: true), Is.EqualTo("sword"));
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: true, isJa: true), Is.EqualTo("sword"));
    }

    [Test]
    public void JaArticleAppend_JapaneseActive_AppendsRawWordAndReturnsTrue()
    {
        StringBuilder result = new();

        var handled = GrammarPatchHelpers.JaArticleAppend("sword", result, capitalize: true, isJa: true);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.True);
            Assert.That(result.ToString(), Is.EqualTo("sword"));
        });
    }

    [TestCase("sword", "swordの")]
    [TestCase("player", "playerの")]
    [TestCase("剣", "剣の")]
    public void JaMakePossessiveResult_JapaneseActive_AppendsNo(string input, string expected)
    {
        Assert.That(GrammarPatchHelpers.JaMakePossessiveResult(input, isJa: true), Is.EqualTo(expected));
    }

    [Test]
    public void JaMakeAndListResult_JapaneseActive_FormatsExpectedLists()
    {
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult([], serial: false, isJa: true), Is.EqualTo(string.Empty));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A"], serial: false, isJa: true), Is.EqualTo("A"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B"], serial: false, isJa: true), Is.EqualTo("AとB"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B", "C"], serial: false, isJa: true), Is.EqualTo("A、B、とC"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["剣", "盾"], serial: false, isJa: true), Is.EqualTo("剣と盾"));
    }

    [Test]
    public void JaMakeAndListResult_SerialTrue_SameOutputAsSerialFalse()
    {
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B"], serial: true, isJa: true), Is.EqualTo("AとB"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B", "C"], serial: true, isJa: true), Is.EqualTo("A、B、とC"));
    }

    [Test]
    public void JaMakeOrListResult_JapaneseActive_FormatsExpectedLists()
    {
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult([], serial: false, isJa: true), Is.EqualTo(string.Empty));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A"], serial: false, isJa: true), Is.EqualTo("A"));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B"], serial: false, isJa: true), Is.EqualTo("AまたはB"));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B", "C"], serial: false, isJa: true), Is.EqualTo("A、B、またはC"));
    }

    [Test]
    public void JaMakeOrListResult_SerialTrue_SameOutputAsSerialFalse()
    {
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B"], serial: true, isJa: true), Is.EqualTo("AまたはB"));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B", "C"], serial: true, isJa: true), Is.EqualTo("A、B、またはC"));
    }

    [Test]
    public void JaCaseAndVerbHelpers_JapaneseActive_ReturnUnchanged()
    {
        Assert.That(GrammarPatchHelpers.JaInitCapResult("sword", isJa: true), Is.EqualTo("sword"));
        Assert.That(GrammarPatchHelpers.JaInitLowerResult("Sword", isJa: true), Is.EqualTo("Sword"));
        Assert.That(GrammarPatchHelpers.JaThirdPersonResult("attack", prependSpace: true, isJa: true), Is.EqualTo("attack"));
        Assert.That(GrammarPatchHelpers.JaPastTenseOfResult("attack", isJa: true), Is.EqualTo("attack"));
    }

    [Test]
    public void AllHelpers_JapaneseInactive_ReturnNull()
    {
        Assert.That(GrammarPatchHelpers.JaPluralizeResult("sword", isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: false, isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakePossessiveResult("sword", isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B"], serial: false, isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B"], serial: false, isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaInitCapResult("sword", isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaInitLowerResult("Sword", isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaThirdPersonResult("attack", prependSpace: false, isJa: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaPastTenseOfResult("attack", isJa: false), Is.Null);
    }

    [Test]
    public void JaArticleAppend_JapaneseInactive_DoesNotAppendAndReturnsFalse()
    {
        StringBuilder result = new();

        var handled = GrammarPatchHelpers.JaArticleAppend("sword", result, capitalize: false, isJa: false);

        Assert.Multiple(() =>
        {
            Assert.That(handled, Is.False);
            Assert.That(result.ToString(), Is.Empty);
        });
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
