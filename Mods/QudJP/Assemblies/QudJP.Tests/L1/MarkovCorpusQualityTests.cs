#pragma warning disable CA1515
#pragma warning disable CA1707
#pragma warning disable CA1812 // CorpusJson is instantiated by JsonSerializer

using System.Text.Json;
using QudJP.Corpus;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for the Japanese Markov corpus file (LibraryCorpus.ja.json).
/// Verifies sentence count, tokenization quality, lore term protection,
/// and that the corpus produces valid Markov chains.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class MarkovCorpusQualityTests
{
    private static readonly string CorpusPath = Path.GetFullPath(
        Path.Combine(TestContext.CurrentContext.TestDirectory, "..", "..", "..", "..", "..", "Localization", "Corpus", "LibraryCorpus.ja.json"));

    private string[] sentences = null!;

    [OneTimeSetUp]
    public void LoadCorpus()
    {
        Assert.That(File.Exists(CorpusPath), Is.True,
            $"Corpus file not found at: {CorpusPath}");

        var json = File.ReadAllText(CorpusPath);
        var doc = JsonSerializer.Deserialize<CorpusJson>(json);
        Assert.That(doc, Is.Not.Null, "Failed to deserialize corpus JSON");
        Assert.That(doc!.sentences, Is.Not.Null, "Corpus has no sentences array");

        sentences = doc.sentences;
    }

    [Test]
    public void JapaneseCorpusFile_HasExpandedSegmentedSourceMaterial()
    {
        // Sentence count
        Assert.That(sentences.Length, Is.InRange(7_000, 8_500),
            $"Expected 7,000-8,500 sentences, got {sentences.Length}");

        // Token count
        var totalTokens = sentences.Sum(s => s.Split(' ').Length);
        Assert.That(totalTokens, Is.GreaterThan(100_000),
            $"Expected >100,000 tokens, got {totalTokens}");

        // Average tokens per sentence
        var avgTokens = (double)totalTokens / sentences.Length;
        Assert.That(avgTokens, Is.InRange(15.0, 25.0),
            $"Expected average 15-25 tokens/sentence, got {avgTokens:F1}");

        // Unique bigrams
        var bigrams = new HashSet<string>();
        foreach (var sentence in sentences)
        {
            var tokens = sentence.Split(' ');
            for (var i = 0; i < tokens.Length - 1; i++)
            {
                bigrams.Add(tokens[i] + " " + tokens[i + 1]);
            }
        }

        Assert.That(bigrams.Count, Is.GreaterThan(60_000),
            $"Expected >60,000 unique bigrams, got {bigrams.Count}");

        // Unique sentence ratio
        var uniqueRatio = (double)new HashSet<string>(sentences).Count / sentences.Length;
        Assert.That(uniqueRatio, Is.GreaterThan(0.98),
            $"Expected >98% unique sentences, got {uniqueRatio:P1}");

        // All sentences are morpheme-segmented (contain space)
        Assert.That(sentences, Has.All.Matches<string>(s => s.Contains(' ', StringComparison.Ordinal)),
            "All sentences must be morpheme-segmented (contain spaces)");

        // All sentences end with '.' (ASCII period)
        Assert.That(sentences, Has.All.EndsWith("."),
            "All sentences must end with ASCII period");

        // No Japanese periods
        Assert.That(sentences, Has.All.Matches<string>(s => !s.Contains('\u3002', StringComparison.Ordinal)),
            "No sentences should contain Japanese period (\u3002)");

        // No double spaces
        Assert.That(sentences, Has.All.Matches<string>(s => !s.Contains("  ", StringComparison.Ordinal)),
            "No sentences should contain double spaces");
    }

    [Test]
    public void JapaneseCorpusFile_ProtectsLoreTermsAsSingleTokens()
    {
        string[] protectedTerms =
        [
            "\u55B0\u3089\u3046\u8005",                 // 喰らう者
            "\u55B0\u3089\u3046\u8005\u306E\u5893\u6240", // 喰らう者の墓所
            "\u30B9\u30EB\u30BF\u30F3",                 // スルタン
            "\u30AF\u30ED\u30FC\u30E0",                 // クローム
            "\u30EC\u30B7\u30A7\u30D5",                 // レシェフ
            "\u30B9\u30D4\u30F3\u30C9\u30EB",           // スピンドル
            "\u30B8\u30E7\u30C3\u30D1",                 // ジョッパ
            "\u30B4\u30EB\u30B4\u30BF",                 // ゴルゴタ
            "\u30D0\u30E9\u30B5\u30E9\u30E0",           // バラサラム
            "\u30C1\u30E3\u30F4\u30A1",                 // チャヴァ
            "\u30AF\u30C3\u30C9",                       // クッド
            "\u30D9\u30C6\u30EB",                       // ベテル
            "\u516D\u65E5\u306E\u30B9\u30C6\u30A3\u30EB\u30C8", // 六日のスティルト
        ];

        foreach (var term in protectedTerms)
        {
            var containingBySentence = sentences
                .Where(s => s.Contains(term, StringComparison.Ordinal))
                .ToList();

            Assert.That(containingBySentence, Is.Not.Empty,
                $"Protected term '{term}' not found in any sentence");

            foreach (var sentence in containingBySentence)
            {
                var tokens = sentence.Split(' ');
                var found = tokens.Any(t => t.Contains(term, StringComparison.Ordinal));
                Assert.That(found, Is.True,
                    $"Term '{term}' should appear as a single token in: {sentence}");
            }
        }
    }

    [Test]
    public void JapaneseCorpusFile_ProducesValidMarkovChain()
    {
        // Join all sentences into a single corpus string
        var corpusText = string.Join(" ", sentences);

        var data = DummyMarkovChain.BuildChain(corpusText, 2);

        Assert.That(data.Chain, Is.Not.Empty, "Chain should not be empty");
        Assert.That(data.OpeningWords, Is.Not.Empty, "OpeningWords should not be empty");

        // Generate a sentence and verify it contains Japanese
        var random = new Random(42);
        var sentence = DummyMarkovChain.GenerateSentence(data, random);

        Assert.That(CorpusNormalizer.ContainsJapaneseCharacters(sentence), Is.True,
            $"Generated sentence should contain Japanese characters: {sentence}");
        Assert.That(sentence.Trim(), Does.EndWith("."),
            "Generated sentence should end with a period");
    }

    /// <summary>Minimal POCO for System.Text.Json deserialization.</summary>
    private sealed class CorpusJson
    {
        public string[] sentences { get; set; } = [];
    }
}

#pragma warning restore CA1812
#pragma warning restore CA1515
#pragma warning restore CA1707
