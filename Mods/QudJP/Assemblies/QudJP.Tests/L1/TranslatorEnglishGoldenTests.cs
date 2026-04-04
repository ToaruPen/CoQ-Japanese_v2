#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class TranslatorEnglishGoldenTests
{
    private DummyTranslatorEnglish translator = null!;

    [SetUp]
    public void SetUp() => translator = new DummyTranslatorEnglish();

    [TestCase(0, "zero")]
    [TestCase(1, "one")]
    [TestCase(3, "three")]
    [TestCase(10, "ten")]
    [TestCase(12, "twelve")]
    [TestCase(13, "thirteen")]
    [TestCase(19, "nineteen")]
    [TestCase(20, "twenty")]
    [TestCase(21, "twenty-one")]
    [TestCase(42, "forty-two")]
    [TestCase(100, "one hundred")]
    [TestCase(101, "one hundred one")]
    [TestCase(1000, "one thousand")]
    [TestCase(1001, "one thousand one")]
    [TestCase(1234567, "one million two hundred thirty-four thousand five hundred sixty-seven")]
    [TestCase(-5, "negative five")]
    public void Cardinal_ReturnsEnglishBaseline(long value, string expected) => Assert.That(translator.Cardinal(value), Is.EqualTo(expected));

    [TestCase(0, "zeroth")]
    [TestCase(1, "first")]
    [TestCase(2, "second")]
    [TestCase(3, "third")]
    [TestCase(5, "fifth")]
    [TestCase(8, "eighth")]
    [TestCase(9, "ninth")]
    [TestCase(12, "twelfth")]
    [TestCase(20, "twentieth")]
    [TestCase(21, "twenty-first")]
    [TestCase(42, "forty-second")]
    [TestCase(100, "one hundredth")]
    [TestCase(1000, "one thousandth")]
    public void Ordinal_ReturnsEnglishBaseline(long value, string expected) => Assert.That(translator.Ordinal(value), Is.EqualTo(expected));

    [TestCase(0, "0th")]
    [TestCase(1, "1st")]
    [TestCase(2, "2nd")]
    [TestCase(3, "3rd")]
    [TestCase(4, "4th")]
    [TestCase(11, "11th")]
    [TestCase(12, "12th")]
    [TestCase(13, "13th")]
    [TestCase(21, "21st")]
    [TestCase(111, "111th")]
    [TestCase(121, "121st")]
    public void OrdinalWithDigits_ReturnsEnglishSuffixes(long value, string expected) => Assert.That(translator.OrdinalWithDigits(value), Is.EqualTo(expected));

    [TestCase(0, "no")]
    [TestCase(1, "one")]
    [TestCase(42, "forty-two")]
    public void CardinalNo_ReturnsExpectedEnglishForm(long value, string expected) => Assert.That(translator.CardinalNo(value), Is.EqualTo(expected));

    [TestCase(0, "zero times")]
    [TestCase(1, "once")]
    [TestCase(2, "twice")]
    [TestCase(3, "three times")]
    [TestCase(4, "four times")]
    [TestCase(12, "twelve times")]
    [TestCase(42, "forty-two times")]
    [TestCase(1000, "one thousand times")]
    public void Multiplicative_ReturnsExpectedEnglishForm(long value, string expected) => Assert.That(translator.Multiplicative(value), Is.EqualTo(expected));

    [Test]
    public void MakeCommaList_UsesEnglishSeparators()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.MakeCommaList([]), Is.Empty);
            Assert.That(translator.MakeCommaList(["apple"]), Is.EqualTo("apple"));
            Assert.That(translator.MakeCommaList(["apple", "pear"]), Is.EqualTo("apple, pear"));
            Assert.That(translator.MakeCommaList(["apple", "pear", "plum"]), Is.EqualTo("apple, pear, plum"));
            Assert.That(translator.MakeCommaList(["red, blue", "green", "gold"]), Is.EqualTo("red, blue; green; gold"));
        });
    }

    [Test]
    public void MakeAndList_UsesEnglishSeparatorsAndSerialCommaRules()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.MakeAndList([]), Is.Empty);
            Assert.That(translator.MakeAndList(["apple"]), Is.EqualTo("apple"));
            Assert.That(translator.MakeAndList(["apple", "pear"]), Is.EqualTo("apple and pear"));
            Assert.That(translator.MakeAndList(["apple", "pear", "plum"]), Is.EqualTo("apple, pear, and plum"));
            Assert.That(translator.MakeAndList(["apple", "pear", "plum"], serialComma: false), Is.EqualTo("apple, pear and plum"));
            Assert.That(translator.MakeAndList(["red, blue", "green", "gold"]), Is.EqualTo("red, blue; green; and gold"));
        });
    }

    [Test]
    public void ExtractArticle_StripsKnownEnglishArticles()
    {
        Assert.Multiple(() =>
        {
            AssertArticle("the sword", "sword", "the");
            AssertArticle("a sword", "sword", "a");
            AssertArticle("an axe", "axe", "a");
            AssertArticle("some water", "water", "some");
            AssertArticle("sword", "sword", string.Empty);
        });
    }

    [Test]
    public void NextPossibleLineBreakIndex_FindsWhitespaceOrFallsBackToEnd()
    {
        translator.NextPossibleLineBreakIndex("hello world".AsSpan(), 0, out int helloBreakIndex, out bool helloReplaceIfBroken);
        translator.NextPossibleLineBreakIndex("nospaces".AsSpan(), 0, out int noSpaceBreakIndex, out bool noSpaceReplaceIfBroken);

        Assert.Multiple(() =>
        {
            Assert.That(helloBreakIndex, Is.EqualTo(5));
            Assert.That(helloReplaceIfBroken, Is.True);
            Assert.That(noSpaceBreakIndex, Is.EqualTo(8));
            Assert.That(noSpaceReplaceIfBroken, Is.False);
        });
    }

    [Test]
    public void GetCultureInfo_ReturnsEnglishUnitedStates()
    {
        Assert.That(translator.GetCultureInfo().Name, Is.EqualTo("en-US"));
    }

    [Test]
    public void IsWordCharacter_MatchesEnglishBaseline()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.IsWordCharacter('a'), Is.True);
            Assert.That(translator.IsWordCharacter('5'), Is.True);
            Assert.That(translator.IsWordCharacter('-'), Is.True);
            Assert.That(translator.IsWordCharacter(' '), Is.False);
            Assert.That(translator.IsWordCharacter('.'), Is.False);
        });
    }

    [Test]
    public void ToLowerAndToUpper_UseEnglishCasing()
    {
        Assert.Multiple(() =>
        {
            Assert.That(translator.ToLower('A'), Is.EqualTo('a'));
            Assert.That(translator.ToUpper('a'), Is.EqualTo('A'));
        });
    }

    private static void AssertArticle(string input, string expectedName, string expectedArticle)
    {
        string name = input;
        DummyTranslatorEnglish translator = new();

        translator.ExtractArticle(ref name, out string article);

        Assert.Multiple(() =>
        {
            Assert.That(name, Is.EqualTo(expectedName));
            Assert.That(article, Is.EqualTo(expectedArticle));
        });
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
