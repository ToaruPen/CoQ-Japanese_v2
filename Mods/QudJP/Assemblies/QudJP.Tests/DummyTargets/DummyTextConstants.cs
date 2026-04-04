#pragma warning disable CA1707
#pragma warning disable CA1034

using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyTextConstants
{
    internal sealed class CrypticMachineData
    {
        public string Charset { get; set; } = "│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌";

        public int WordLengthMin { get; set; } = 3;

        public int WordLengthMax { get; set; } = 10;

        public int SentenceLengthMin { get; set; } = 3;

        public int SentenceLengthMax { get; set; } = 40;
    }

    internal sealed class GlyphData
    {
        public string Name { get; set; } = string.Empty;

        public string Glyph { get; set; } = string.Empty;

        public string Color { get; set; } = string.Empty;
    }

    public const string BOX_SINGLE_UP_DOWN_LEFT = "┤";
    public const string BOX_SINGLE_DOWN_LEFT = "┐";
    public const string BOX_SINGLE_UP_RIGHT = "└";
    public const string BOX_SINGLE_UP_LEFT = "┘";
    public const string BOX_SINGLE_DOWN_RIGHT = "┌";

    private static readonly GlyphData MissingGlyph = new()
    {
        Name = "Missing",
        Glyph = "⌧",
        Color = "R",
    };

    public Dictionary<string, List<string>> WeirdSets { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> WeirdReverse { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, HashSet<string>> WordLists { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, GlyphData> Glyphs { get; } = new(StringComparer.Ordinal);

    public CrypticMachineData CrypticMachineInfo { get; } = new();

    public string ObfuscatorCharset { get; set; } = "♣☼▬▲▼▓█▄▌▐▀°■";

    public void BuildWeirdReverse()
    {
        WeirdReverse.Clear();

        foreach (KeyValuePair<string, List<string>> weirdSet in WeirdSets)
        {
            foreach (string grapheme in weirdSet.Value)
            {
                WeirdReverse.TryAdd(grapheme, weirdSet.Key);
            }
        }
    }

    public GlyphData GetGlyph(string name)
    {
        return Glyphs.TryGetValue(name, out GlyphData? value) ? value : MissingGlyph;
    }

    public HashSet<string> GetWordList(string type)
    {
        if (WordLists.TryGetValue(type, out HashSet<string>? value))
        {
            return value;
        }

        return WordLists[type] = new HashSet<string>(StringComparer.OrdinalIgnoreCase);
    }
}

#pragma warning restore CA1034
#pragma warning restore CA1707
