#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Corpus;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for DummyMarkovChain — faithful reproduction of XRL.MarkovChain.
/// Verifies chain building, sentence generation, serialization roundtrip,
/// and replacement logic using both English and Japanese corpus text.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class MarkovChainBuildTests
{
    // Simple English corpus: two sentences, morpheme-separated, period-terminated.
    private const string SimpleCorpus = "the quick brown fox jumps . the slow red fox sits . ";

    // Japanese corpus fragment: morpheme-segmented, period-terminated.
    private const string JapaneseCorpus =
        "喰らう者の墓所 の 門 は 閉ざさ れ た . " +
        "レシェフ は 渦動 の 呪い を 解く ため 旅 に 出 た . " +
        "スピンドル の 影 の 下 で 人々 は 記念碑 を 建て た . ";

    [Test]
    public void BuildChain_SetsCorrectOrder()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        Assert.That(data.Order, Is.EqualTo(2));
    }

    [Test]
    public void BuildChain_PopulatesChainDictionary()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        Assert.That(data.Chain, Is.Not.Empty);
    }

    [Test]
    public void BuildChain_FirstSentenceStartNotCapturedAsOpeningWord()
    {
        // The very first n-gram has no preceding period, so it is NOT an opening word.
        // Only subsequent sentence starts (after a '.') are captured.
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        Assert.That(data.OpeningWords, Does.Not.Contain("the quick"));
    }

    [Test]
    public void BuildChain_SecondSentenceStartCapturedAsOpeningWord()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        Assert.That(data.OpeningWords, Does.Contain("the slow"));
    }

    [Test]
    public void BuildChain_ChainContainsExpectedTransition()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        Assert.That(data.Chain.ContainsKey("quick brown"), Is.True);
        Assert.That(data.Chain["quick brown"], Does.Contain("fox"));
    }

    [Test]
    public void BuildChain_JapaneseCorpus_ProducesNonEmptyChain()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(JapaneseCorpus, 2);

        Assert.That(data.Chain, Is.Not.Empty);
        Assert.That(data.OpeningWords, Is.Not.Empty);
    }

    [Test]
    public void BuildChain_JapaneseCorpus_OpeningWordsContainJapanese()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(JapaneseCorpus, 2);

        Assert.That(data.OpeningWords, Has.Some.Matches<string>(
            w => CorpusNormalizer.ContainsJapaneseCharacters(w)));
    }

    [Test]
    public void GenerateSentence_ProducesTextEndingWithPeriod()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);
        Random random = new(42);

        string sentence = DummyMarkovChain.GenerateSentence(data, random);

        Assert.That(sentence.Trim(), Does.EndWith("."));
    }

    [Test]
    public void GenerateSentence_WithSeed_UsesProvidedSeed()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);
        Random random = new(42);

        string sentence = DummyMarkovChain.GenerateSentence(data, random, seed: "the slow");

        Assert.That(sentence, Does.StartWith("the slow"));
    }

    [Test]
    public void GenerateSentence_JapaneseChain_ContainsJapaneseCharacters()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(JapaneseCorpus, 2);
        Random random = new(42);

        string sentence = DummyMarkovChain.GenerateSentence(data, random);

        Assert.That(CorpusNormalizer.ContainsJapaneseCharacters(sentence), Is.True,
            $"Expected Japanese characters in: {sentence}");
    }

    [Test]
    public void GenerateParagraph_ContainsMultipleSentences()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);
        Random random = new(42);

        string paragraph = DummyMarkovChain.GenerateParagraph(data, random);

        // A paragraph has 3-6 sentences, each ending with "." and separated by spaces.
        int periodCount = paragraph.Count(c => c == '.');
        Assert.That(periodCount, Is.GreaterThanOrEqualTo(3));
        Assert.That(paragraph, Does.EndWith("\n\n"));
    }

    [Test]
    public void Count_ReturnsTransitionCount()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);

        int count = DummyMarkovChain.Count(data);

        Assert.That(count, Is.GreaterThan(0));
    }

    [Test]
    public void Serialization_RoundTrip_PreservesChain()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(SimpleCorpus, 2);
        int originalKeyCount = data.Chain.Count;
        int originalOpeningCount = data.OpeningWords.Count;

        // Serialize
        data.OnBeforeSerialize();
        Assert.That(data.SerializedKeys, Has.Count.EqualTo(originalKeyCount));
        Assert.That(data.SerializedValues, Has.Count.EqualTo(originalKeyCount));

        // Simulate deserialization: clear chain, then restore
        data.Chain.Clear();
        data.OnAfterDeserialize();

        Assert.That(data.Chain, Has.Count.EqualTo(originalKeyCount));
        Assert.That(data.OpeningWords, Has.Count.EqualTo(originalOpeningCount));
    }

    [Test]
    public void Serialization_ValuesUseSohSeparator()
    {
        DummyMarkovChainData data = new()
        {
            Order = 2,
            Chain = new Dictionary<string, List<string>>
            {
                ["key one"] = new List<string> { "a", "b", "c" },
            },
        };

        data.OnBeforeSerialize();

        Assert.That(data.SerializedValues[0], Is.EqualTo("a\u0001b\u0001c"));
    }

    [Test]
    public void Replace_SubstitutesInOpeningWordsAndChainValues()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain(
            "*Sultan1Name* is great . *Sultan1Name* is wise . ", 2);

        data.Replace("*Sultan1Name*", "Resheph");

        Assert.That(data.OpeningWords, Has.All.Not.Contains("*Sultan1Name*"));
        foreach (List<string> transitions in data.Chain.Values)
        {
            Assert.That(transitions, Has.All.Not.Contains("*Sultan1Name*"));
        }
    }

    [Test]
    public void Replace_RenamesChainKeys()
    {
        DummyMarkovChainData data = new()
        {
            Order = 2,
            Chain = new Dictionary<string, List<string>>
            {
                ["*Sultan1Name* is"] = new List<string> { "great" },
                ["is great"] = new List<string> { "." },
            },
        };

        data.Replace("*Sultan1Name*", "Resheph");

        Assert.That(data.Chain.ContainsKey("Resheph is"), Is.True);
        Assert.That(data.Chain.ContainsKey("*Sultan1Name* is"), Is.False);
    }

    [Test]
    public void Replace_Expansion_CreatesMultipleKeyVariants()
    {
        DummyMarkovChainData data = new()
        {
            Order = 2,
            OpeningWords = new List<string> { "*sultan* rules" },
            Chain = new Dictionary<string, List<string>>
            {
                ["*sultan* rules"] = new List<string> { "wisely" },
            },
        };

        data.Replace("*sultan*", new List<string> { "Resheph", "Erisheth" });

        Assert.That(data.OpeningWords, Does.Contain("Resheph rules"));
        Assert.That(data.OpeningWords, Does.Contain("Erisheth rules"));
        Assert.That(data.Chain.ContainsKey("Resheph rules"), Is.True);
        Assert.That(data.Chain.ContainsKey("Erisheth rules"), Is.True);
    }

    [Test]
    public void AppendCorpus_AddsTransitionsToExistingChain()
    {
        DummyMarkovChainData data = DummyMarkovChain.BuildChain("the cat sat . ", 2);
        int originalCount = DummyMarkovChain.Count(data);

        DummyMarkovChain.AppendCorpus(data, "the dog ran . ");
        int newCount = DummyMarkovChain.Count(data);

        Assert.That(newCount, Is.GreaterThan(originalCount));
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
