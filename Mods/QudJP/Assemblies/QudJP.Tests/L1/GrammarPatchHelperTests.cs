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
    [SetUp]
    public void SetUp()
    {
        GrammarPatchHelpers.IsJapaneseActive = true;
    }

    [TearDown]
    public void TearDown()
    {
        GrammarPatchHelpers.IsJapaneseActive = false;
    }

    [Test]
    public void JaPluralizeResult_JapaneseActive_ReturnsInput()
    {
        Assert.That(GrammarPatchHelpers.JaPluralizeResult("sword"), Is.EqualTo("sword"));
    }

    [Test]
    public void JaArticleResult_JapaneseActive_ReturnsInput()
    {
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: false), Is.EqualTo("sword"));
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: true), Is.EqualTo("sword"));
    }

    [Test]
    public void JaArticleAppend_JapaneseActive_AppendsRawWordOnly()
    {
        StringBuilder result = new();

        GrammarPatchHelpers.JaArticleAppend("sword", result, capitalize: true);

        Assert.That(result.ToString(), Is.EqualTo("sword"));
    }

    [TestCase("sword", "swordの")]
    [TestCase("player", "playerの")]
    [TestCase("剣", "剣の")]
    public void JaMakePossessiveResult_JapaneseActive_AppendsNo(string input, string expected)
    {
        Assert.That(GrammarPatchHelpers.JaMakePossessiveResult(input), Is.EqualTo(expected));
    }

    [Test]
    public void JaMakeAndListResult_JapaneseActive_FormatsExpectedLists()
    {
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult([], serial: false), Is.EqualTo(string.Empty));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A"], serial: false), Is.EqualTo("A"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B"], serial: false), Is.EqualTo("AとB"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B", "C"], serial: false), Is.EqualTo("A、B、とC"));
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["剣", "盾"], serial: false), Is.EqualTo("剣と盾"));
    }

    [Test]
    public void JaMakeOrListResult_JapaneseActive_FormatsExpectedLists()
    {
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult([], serial: false), Is.EqualTo(string.Empty));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A"], serial: false), Is.EqualTo("A"));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B"], serial: false), Is.EqualTo("AまたはB"));
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B", "C"], serial: false), Is.EqualTo("A、B、またはC"));
    }

    [Test]
    public void JaCaseAndVerbHelpers_JapaneseActive_ReturnUnchanged()
    {
        Assert.That(GrammarPatchHelpers.JaInitCapResult("sword"), Is.EqualTo("sword"));
        Assert.That(GrammarPatchHelpers.JaInitLowerResult("Sword"), Is.EqualTo("Sword"));
        Assert.That(GrammarPatchHelpers.JaThirdPersonResult("attack", prependSpace: true), Is.EqualTo("attack"));
        Assert.That(GrammarPatchHelpers.JaPastTenseOfResult("attack"), Is.EqualTo("attack"));
    }

    [Test]
    public void StringHelpers_JapaneseInactive_ReturnNull()
    {
        GrammarPatchHelpers.IsJapaneseActive = false;

        Assert.That(GrammarPatchHelpers.JaPluralizeResult("sword"), Is.Null);
        Assert.That(GrammarPatchHelpers.JaArticleResult("sword", capitalize: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakePossessiveResult("sword"), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakeAndListResult(["A", "B"], serial: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaMakeOrListResult(["A", "B"], serial: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaInitCapResult("sword"), Is.Null);
        Assert.That(GrammarPatchHelpers.JaInitLowerResult("Sword"), Is.Null);
        Assert.That(GrammarPatchHelpers.JaThirdPersonResult("attack", prependSpace: false), Is.Null);
        Assert.That(GrammarPatchHelpers.JaPastTenseOfResult("attack"), Is.Null);
    }

    [Test]
    public void JaArticleAppend_JapaneseInactive_DoesNotAppend()
    {
        GrammarPatchHelpers.IsJapaneseActive = false;
        StringBuilder result = new();

        GrammarPatchHelpers.JaArticleAppend("sword", result, capitalize: false);

        Assert.That(result.ToString(), Is.Empty);
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
