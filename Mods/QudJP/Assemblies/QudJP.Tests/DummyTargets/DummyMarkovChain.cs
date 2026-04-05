#pragma warning disable CA1707
#pragma warning disable CA5394 // Random is used for Markov chain generation, not security

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.MarkovChain from decompiled beta (212.17).
/// Replaces <c>Stat.Random(min, max)</c> (inclusive both ends) with
/// <see cref="Random"/> parameter for deterministic testing.
/// </summary>
internal static class DummyMarkovChain
{
    /// <summary>
    /// Builds a Markov chain from a space-delimited corpus string.
    /// Sentence boundaries are detected by tokens ending with '.'.
    /// Faithful to MarkovChain.BuildChain (line 13-50).
    /// </summary>
    public static DummyMarkovChainData BuildChain(string corpus, int order)
    {
        DummyMarkovChainData data = new() { Order = order };
        string[] array = corpus.Split(' ');
        bool flag = false;
        for (int i = 0; i < array.Length - order; i++)
        {
            string text = array[i];
            text = text.TrimStart(' ');
            text = text.TrimEnd(' ');
            string text2 = (i != 0) ? array[i - 1] : " ";
            if (text2.Length == 0)
            {
                text2 = " ";
            }

            if (text2[Math.Max(text2.Length - 1, 0)] == '.')
            {
                flag = true;
            }

            for (int j = 1; j < order; j++)
            {
                text = text + " " + array[i + j];
            }

            if (flag)
            {
                data.OpeningWords.Add(text);
                flag = false;
            }

            if (data.Chain.TryGetValue(text, out var transitions))
            {
                transitions.Add(array[i + order]);
            }
            else
            {
                data.Chain[text] = new List<string> { array[i + order] };
            }
        }

        return data;
    }

    /// <summary>
    /// Appends additional corpus text to an existing chain.
    /// Faithful to MarkovChain.AppendCorpus (line 52-87).
    /// </summary>
    public static DummyMarkovChainData AppendCorpus(DummyMarkovChainData data, string corpus, bool addOpeningWords = true)
    {
        string[] array = corpus.Split(' ');
        bool flag = false;
        for (int i = 0; i < array.Length - data.Order; i++)
        {
            string text = array[i];
            text = text.TrimStart(' ');
            text = text.TrimEnd(' ');
            string text2 = (i != 0) ? array[i - 1] : " ";
            if (text2.Length == 0)
            {
                text2 = " ";
            }

            if (text2[Math.Max(text2.Length - 1, 0)] == '.')
            {
                flag = true;
            }

            for (int j = 1; j < data.Order; j++)
            {
                text = text + " " + array[i + j];
            }

            if (flag && addOpeningWords)
            {
                data.OpeningWords.Add(text);
                flag = false;
            }

            if (data.Chain.TryGetValue(text, out var transitions2))
            {
                transitions2.Add(array[i + data.Order]);
            }
            else
            {
                data.Chain[text] = new List<string> { array[i + data.Order] };
            }
        }

        return data;
    }

    /// <summary>
    /// Generates a sentence by walking the chain until a token containing '.' is found.
    /// Uses <paramref name="random"/> instead of <c>Stat.Random</c> for deterministic testing.
    /// Faithful to MarkovChain.GenerateSentence (line 133-175).
    /// </summary>
    public static string GenerateSentence(DummyMarkovChainData data, Random random, string? seed = null, int maxRecursion = 10)
    {
        if (string.IsNullOrEmpty(seed))
        {
            seed = data.OpeningWords[random.Next(data.OpeningWords.Count)];
        }

        List<string> list = new();
        string[] seedParts = seed.Split(' ');
        for (int i = 0; i < data.Order; i++)
        {
            list.Add(seedParts[i]);
        }

        for (int j = 0; j < 100; j++)
        {
            string text = list[j];
            for (int k = 1; k < data.Order; k++)
            {
                text = text + " " + list[j + k];
            }

            if (!data.Chain.TryGetValue(text, out var sentenceTransitions))
            {
                break;
            }

            string text2 = sentenceTransitions[random.Next(sentenceTransitions.Count)];
            list.Add(text2);
            if (text2.Contains('.', StringComparison.Ordinal))
            {
                return string.Join(" ", list) + " ";
            }
        }

        if (maxRecursion <= 0)
        {
            return string.Join(" ", list) + " ";
        }

        return GenerateSentence(data, random, maxRecursion: maxRecursion - 1);
    }

    /// <summary>
    /// Generates a short sentence with at most <paramref name="maxWords"/> words.
    /// Faithful to MarkovChain.GenerateShortSentence (line 177-188).
    /// </summary>
    public static string GenerateShortSentence(DummyMarkovChainData data, Random random, string? seed = null, int maxWords = 18)
    {
        int num = 50;
        string text;
        do
        {
            text = GenerateSentence(data, random, seed);
            num--;
        }
        while (text.Split(' ').Length > maxWords && num > 0);

        return text;
    }

    /// <summary>
    /// Generates a paragraph of 3-6 sentences.
    /// Faithful to MarkovChain.GenerateParagraph (line 190-199).
    /// </summary>
    public static string GenerateParagraph(DummyMarkovChainData data, Random random)
    {
        string text = "";
        int num = random.Next(3, 7); // Stat.Random(3, 6) → inclusive, Random.Next exclusive upper
        for (int i = 0; i < num; i++)
        {
            text += GenerateSentence(data, random);
        }

        return text + "\n\n";
    }

    /// <summary>
    /// Counts total transition entries across all chain keys.
    /// Faithful to MarkovChain.Count (line 264-272).
    /// </summary>
    public static int Count(DummyMarkovChainData data)
    {
        int num = 0;
        foreach (List<string> value in data.Chain.Values)
        {
            num += value.Count;
        }

        return num;
    }
}

#pragma warning restore CA5394
#pragma warning restore CA1707
