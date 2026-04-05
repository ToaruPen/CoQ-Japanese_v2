#pragma warning disable CA1707

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.MarkovChainData from decompiled beta (212.17).
/// Reproduces chain data structure, serialization roundtrip (keys/values with \u0001 separator),
/// and single/expansion replacement logic.
/// </summary>
internal sealed class DummyMarkovChainData
{
    public int Order;

    public List<string> OpeningWords = new();

    /// <summary>Serialization buffer: populated by <see cref="OnBeforeSerialize"/>.</summary>
    public List<string> SerializedKeys = new();

    /// <summary>Serialization buffer: populated by <see cref="OnBeforeSerialize"/>.</summary>
    public List<string> SerializedValues = new();

    public Dictionary<string, List<string>> Chain = new();

    /// <summary>
    /// Flattens <see cref="Chain"/> into parallel <see cref="SerializedKeys"/>/<see cref="SerializedValues"/> lists.
    /// Values within each transition list are joined with '\u0001' (SOH).
    /// Faithful to MarkovChainData.OnBeforeSerialize (line 23-42).
    /// </summary>
    public void OnBeforeSerialize()
    {
        SerializedKeys.Clear();
        SerializedValues.Clear();
        StringBuilder stringBuilder = new();
        foreach (KeyValuePair<string, List<string>> item in Chain)
        {
            SerializedKeys.Add(item.Key);
            stringBuilder.Length = 0;
            for (int i = 0; i < item.Value.Count; i++)
            {
                if (i != 0)
                {
                    stringBuilder.Append('\u0001');
                }

                stringBuilder.Append(item.Value[i]);
            }

            SerializedValues.Add(stringBuilder.ToString());
        }
    }

    /// <summary>
    /// Rebuilds <see cref="Chain"/> from <see cref="SerializedKeys"/>/<see cref="SerializedValues"/>.
    /// Faithful to MarkovChainData.OnAfterDeserialize (line 44-57).
    /// </summary>
    public void OnAfterDeserialize()
    {
        Chain.Clear();
        if (SerializedKeys.Count != SerializedValues.Count)
        {
            throw new InvalidOperationException(
                $"there are {SerializedKeys.Count} keys and {SerializedValues.Count} values after deserialization.");
        }

        for (int i = 0; i < SerializedKeys.Count; i++)
        {
            Chain.Add(SerializedKeys[i], new List<string>(SerializedValues[i].Split('\u0001')));
        }

        SerializedKeys.Clear();
        SerializedValues.Clear();
    }

    /// <summary>
    /// Single replacement in OpeningWords and Chain keys/values.
    /// Faithful to MarkovChainData.Replace(string, string) (line 59-83).
    /// </summary>
    public void Replace(string old, string replacement)
    {
        for (int num = OpeningWords.Count - 1; num >= 0; num--)
        {
            OpeningWords[num] = OpeningWords[num].Replace(old, replacement, StringComparison.Ordinal);
        }

        List<string> keysToRename = new();
        foreach (KeyValuePair<string, List<string>> item in Chain)
        {
            if (item.Key.Contains(old, StringComparison.Ordinal))
            {
                keysToRename.Add(item.Key);
            }

            for (int i = 0; i < item.Value.Count; i++)
            {
                item.Value[i] = item.Value[i].Replace(old, replacement, StringComparison.Ordinal);
            }
        }

        foreach (string key in keysToRename)
        {
            List<string> value = Chain[key];
            Chain.Remove(key);
            Chain[key.Replace(old, replacement, StringComparison.Ordinal)] = value;
        }
    }

    /// <summary>
    /// Expansion replacement: each occurrence generates N new entries.
    /// Faithful to MarkovChainData.Replace(string, IList&lt;string&gt;) (line 86-111).
    /// </summary>
    public void Replace(string old, IList<string> replacements)
    {
        if (replacements == null || replacements.Count == 0)
        {
            return;
        }

        Expand(OpeningWords, old, replacements);
        List<string> keysToRename = new();
        foreach (KeyValuePair<string, List<string>> item in Chain)
        {
            if (item.Key.Contains(old, StringComparison.Ordinal))
            {
                keysToRename.Add(item.Key);
            }

            Expand(item.Value, old, replacements);
        }

        foreach (string key in keysToRename)
        {
            List<string> value = Chain[key];
            Chain.Remove(key);
            foreach (string item in replacements)
            {
                Chain[key.Replace(old, item, StringComparison.Ordinal)] = value;
            }
        }
    }

    /// <summary>
    /// Faithful to MarkovChainData.Expand (line 113-127).
    /// For each item containing <paramref name="old"/>, replaces with first replacement in-place
    /// and inserts remaining replacements after it.
    /// </summary>
    private static void Expand(List<string> items, string old, IList<string> replacements)
    {
        for (int num = items.Count - 1; num >= 0; num--)
        {
            string text = items[num];
            if (text.Contains(old, StringComparison.Ordinal))
            {
                items[num] = text.Replace(old, replacements[0], StringComparison.Ordinal);
                for (int i = 1; i < replacements.Count; i++)
                {
                    items.Insert(num + i, text.Replace(old, replacements[i], StringComparison.Ordinal));
                }
            }
        }
    }
}

#pragma warning restore CA1707
