#pragma warning disable CA1062
#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Corpus;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for CorpusNormalizer — production code in src/Corpus/.
/// Tests NormalizeSentence, ShouldUseJapaneseCorpus, and ContainsJapaneseCharacters.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class CorpusNormalizerTests
{
    // --- NormalizeSentence ---

    [Test]
    public void NormalizeSentence_ConvertsJapanesePeriodToAscii()
    {
        string result = CorpusNormalizer.NormalizeSentence("テスト 文章。");

        Assert.That(result, Is.EqualTo("テスト 文章."));
    }

    [Test]
    public void NormalizeSentence_AppendsPeriodIfMissing()
    {
        string result = CorpusNormalizer.NormalizeSentence("テスト 文章");

        Assert.That(result, Is.EqualTo("テスト 文章."));
    }

    [Test]
    public void NormalizeSentence_PreservesExistingPeriod()
    {
        string result = CorpusNormalizer.NormalizeSentence("テスト 文章.");

        Assert.That(result, Is.EqualTo("テスト 文章."));
    }

    [Test]
    public void NormalizeSentence_CollapsesWhitespace()
    {
        string result = CorpusNormalizer.NormalizeSentence("テスト  文章   です.");

        Assert.That(result, Is.EqualTo("テスト 文章 です."));
    }

    [Test]
    public void NormalizeSentence_TrimsLeadingAndTrailingWhitespace()
    {
        string result = CorpusNormalizer.NormalizeSentence("  テスト 文章.  ");

        Assert.That(result, Is.EqualTo("テスト 文章."));
    }

    [TestCase("")]
    [TestCase("   ")]
    [TestCase(null)]
    public void NormalizeSentence_EmptyOrWhitespace_ReturnsEmpty(string? input)
    {
        string result = CorpusNormalizer.NormalizeSentence(input!);

        Assert.That(result, Is.EqualTo(string.Empty));
    }

    [Test]
    public void NormalizeSentence_TabsAndNewlines_CollapsedToSingleSpace()
    {
        string result = CorpusNormalizer.NormalizeSentence("テスト\t文章\nです.");

        Assert.That(result, Is.EqualTo("テスト 文章 です."));
    }

    // --- ShouldUseJapaneseCorpus ---

    [Test]
    public void ShouldUseJapaneseCorpus_LibraryCorpus_ReturnsTrue()
    {
        Assert.That(CorpusNormalizer.ShouldUseJapaneseCorpus("LibraryCorpus.json"), Is.True);
    }

    [TestCase("OtherCorpus.json")]
    [TestCase("librarycorpus.json")]
    [TestCase("LibraryCorpus.JSON")]
    [TestCase("")]
    public void ShouldUseJapaneseCorpus_OtherCorpusNames_ReturnsFalse(string corpusName)
    {
        Assert.That(CorpusNormalizer.ShouldUseJapaneseCorpus(corpusName), Is.False);
    }

    // --- ContainsJapaneseCharacters ---

    [TestCase("あいうえお", Description = "Hiragana")]
    [TestCase("カタカナ", Description = "Katakana")]
    [TestCase("漢字", Description = "Kanji")]
    [TestCase("mixed テスト text", Description = "Mixed")]
    public void ContainsJapaneseCharacters_WithJapanese_ReturnsTrue(string text)
    {
        Assert.That(CorpusNormalizer.ContainsJapaneseCharacters(text), Is.True);
    }

    [TestCase("hello world")]
    [TestCase("12345")]
    [TestCase("!@#$%")]
    [TestCase("")]
    public void ContainsJapaneseCharacters_WithoutJapanese_ReturnsFalse(string text)
    {
        Assert.That(CorpusNormalizer.ContainsJapaneseCharacters(text), Is.False);
    }
}

#pragma warning restore CA1062
#pragma warning restore CA1515
#pragma warning restore CA1707
