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

    [TestCase(0, "0")]
    [TestCase(1, "1")]
    [TestCase(3, "3")]
    [TestCase(10, "10")]
    [TestCase(42, "42")]
    [TestCase(100, "100")]
    [TestCase(1234567, "1234567")]
    [TestCase(-5, "-5")]
    public void Cardinal_ReturnsArabicDigits(long value, string expected) => Assert.That(translator.Cardinal(value), Is.EqualTo(expected));

    [TestCase(0, "第〇")]
    [TestCase(1, "第一")]
    [TestCase(2, "第二")]
    [TestCase(3, "第三")]
    [TestCase(12, "第十二")]
    [TestCase(42, "第四十二")]
    [TestCase(100, "第百")]
    [TestCase(1_0000_0000_0000_0000L, "第一京")]
    [TestCase(-1_0000_0000_0000_0000L, "第マイナス一京")]
    public void Ordinal_ReturnsKanjiNumerals(long value, string expected) => Assert.That(translator.Ordinal(value), Is.EqualTo(expected));

    [TestCase(1, "1")]
    [TestCase(2, "2")]
    [TestCase(42, "42")]
    [TestCase(100, "100")]
    public void OrdinalWithDigits_ReturnsPlainDigits(long value, string expected) => Assert.That(translator.OrdinalWithDigits(value), Is.EqualTo(expected));

    [TestCase(0, "0")]
    [TestCase(1, "1")]
    [TestCase(42, "42")]
    public void CardinalNo_ReturnsArabicDigits(long value, string expected) => Assert.That(translator.CardinalNo(value), Is.EqualTo(expected));

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
    public void NextPossibleLineBreakIndex_EdgeCases()
    {
        Assert.Multiple(() =>
        {
            // 空文字列: breakBeforeIndex は 0
            translator.NextPossibleLineBreakIndex("".AsSpan(), 0, out int idx1, out bool replace1);
            Assert.That(idx1, Is.EqualTo(0));
            Assert.That(replace1, Is.False);

            // startIndex が末尾（length - 1）: breakBeforeIndex は length
            translator.NextPossibleLineBreakIndex("あ".AsSpan(), 0, out int idx2, out bool replace2);
            Assert.That(idx2, Is.EqualTo(1));
            Assert.That(replace2, Is.False);

            // 負の startIndex: 0 として正規化され次の文字位置を返す
            translator.NextPossibleLineBreakIndex("あい".AsSpan(), -1, out int idx3, out bool replace3);
            Assert.That(idx3, Is.EqualTo(1));
            Assert.That(replace3, Is.False);
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
