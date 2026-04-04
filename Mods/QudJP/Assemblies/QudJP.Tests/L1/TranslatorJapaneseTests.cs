#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class TranslatorJapaneseTests
{
    private DummyTranslatorJapanese translator = null!;

    [SetUp]
    public void SetUp() => translator = new DummyTranslatorJapanese();

    [TestCase(0, "零")]
    [TestCase(1, "一")]
    [TestCase(3, "三")]
    [TestCase(10, "十")]
    [TestCase(12, "十二")]
    [TestCase(20, "二十")]
    [TestCase(42, "四十二")]
    [TestCase(100, "百")]
    [TestCase(101, "百一")]
    [TestCase(1000, "千")]
    [TestCase(10000, "一万")]
    [TestCase(100000000, "一億")]
    [TestCase(1000000000000, "一兆")]
    [TestCase(1234567, "百二十三万四千五百六十七")]
    [TestCase(-5, "マイナス五")]
    public void Cardinal_ReturnsJapaneseBaseline(long value, string expected) => Assert.That(translator.Cardinal(value), Is.EqualTo(expected));

    [TestCase(1, "第一")]
    [TestCase(2, "第二")]
    [TestCase(3, "第三")]
    [TestCase(12, "第十二")]
    [TestCase(42, "第四十二")]
    [TestCase(100, "第百")]
    public void Ordinal_ReturnsJapaneseBaseline(long value, string expected) => Assert.That(translator.Ordinal(value), Is.EqualTo(expected));

    [TestCase(1, "第1")]
    [TestCase(2, "第2")]
    [TestCase(42, "第42")]
    [TestCase(100, "第100")]
    public void OrdinalWithDigits_ReturnsJapaneseDigits(long value, string expected) => Assert.That(translator.OrdinalWithDigits(value), Is.EqualTo(expected));

    [TestCase(0, "零")]
    [TestCase(1, "一")]
    [TestCase(42, "四十二")]
    public void CardinalNo_ReturnsExpectedJapaneseForm(long value, string expected) => Assert.That(translator.CardinalNo(value), Is.EqualTo(expected));

    [TestCase(0, "一度もない")]
    [TestCase(1, "1回")]
    [TestCase(2, "2回")]
    [TestCase(3, "3回")]
    [TestCase(42, "42回")]
    public void Multiplicative_ReturnsExpectedJapaneseForm(long value, string expected) => Assert.That(translator.Multiplicative(value), Is.EqualTo(expected));

    [Test]
    public void MakeCommaList_UsesJapaneseSeparators()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.MakeCommaList([]), Is.Empty);
            Assert.That(translator.MakeCommaList(["a"]), Is.EqualTo("a"));
            Assert.That(translator.MakeCommaList(["a", "b"]), Is.EqualTo("a、b"));
            Assert.That(translator.MakeCommaList(["a", "b", "c"]), Is.EqualTo("a、b、c"));
        });
    }

    [Test]
    public void MakeAndList_UsesJapaneseConjunctionRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.MakeAndList([]), Is.Empty);
            Assert.That(translator.MakeAndList(["a"]), Is.EqualTo("a"));
            Assert.That(translator.MakeAndList(["a", "b"]), Is.EqualTo("aとb"));
            Assert.That(translator.MakeAndList(["a", "b", "c"]), Is.EqualTo("a、b、c"));
            Assert.That(translator.MakeAndList(["a", "b", "c"], serialComma: false), Is.EqualTo("a、b、c"));
        });
    }

    [Test]
    public void ExtractArticle_LeavesJapaneseTextUnchanged()
    {
        string name = "勇者";

        translator.ExtractArticle(ref name, out string article);

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo("勇者"));
            Assert.That(article, Is.Empty);
        });
    }

    [Test]
    public void NextPossibleLineBreakIndex_BreaksAtCharacterBoundary()
    {
        translator.NextPossibleLineBreakIndex("こんにちは".AsSpan(), 0, out int breakBeforeIndex, out bool replaceIfBroken);

        Assert.Multiple(() =>
        {
            Assert.That(breakBeforeIndex, Is.EqualTo(1));
            Assert.That(replaceIfBroken, Is.False);
        });
    }

    [Test]
    public void GetCultureInfo_ReturnsJapaneseCulture()
    {
        Assert.That(translator.GetCultureInfo().Name, Is.EqualTo("ja-JP"));
    }

    [Test]
    public void DefiniteAndIndefiniteArticles_ReturnEmptyStrings()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.DefiniteArticle("勇者"), Is.Empty);
            Assert.That(translator.IndefiniteArticle("勇者"), Is.Empty);
        });
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
