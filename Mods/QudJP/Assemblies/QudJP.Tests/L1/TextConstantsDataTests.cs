#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class TextConstantsDataTests
{
    [Test]
    public void WeirdReverse_BuiltFromWeirdSets()
    {
        DummyTextConstants constants = new();
        constants.WeirdSets.Add("setA", ["α", "β"]);

        constants.BuildWeirdReverse();

        Assert.Multiple(() =>
        {
            Assert.That(constants.WeirdReverse["α"], Is.EqualTo("setA"));
            Assert.That(constants.WeirdReverse["β"], Is.EqualTo("setA"));
        });
    }

    [Test]
    public void WeirdReverse_TryAddSemantics_FirstWins()
    {
        DummyTextConstants constants = new();
        constants.WeirdSets.Add("first", ["α"]);
        constants.WeirdSets.Add("second", ["α", "β"]);

        constants.BuildWeirdReverse();

        Assert.Multiple(() =>
        {
            Assert.That(constants.WeirdReverse["α"], Is.EqualTo("first"));
            Assert.That(constants.WeirdReverse["β"], Is.EqualTo("second"));
        });
    }

    [Test]
    public void GetGlyph_KnownName_ReturnsData()
    {
        DummyTextConstants constants = new();
        constants.Glyphs["Known"] = new DummyTextConstants.GlyphData
        {
            Name = "Known",
            Glyph = "K",
            Color = "g",
        };

        DummyTextConstants.GlyphData glyph = constants.GetGlyph("Known");

        Assert.Multiple(() =>
        {
            Assert.That(glyph.Name, Is.EqualTo("Known"));
            Assert.That(glyph.Glyph, Is.EqualTo("K"));
            Assert.That(glyph.Color, Is.EqualTo("g"));
        });
    }

    [Test]
    public void GetGlyph_Unknown_ReturnsMissingGlyph()
    {
        DummyTextConstants constants = new();

        DummyTextConstants.GlyphData glyph = constants.GetGlyph("Unknown");

        Assert.Multiple(() =>
        {
            Assert.That(glyph.Name, Is.EqualTo("Missing"));
            Assert.That(glyph.Glyph, Is.EqualTo("⌧"));
            Assert.That(glyph.Color, Is.EqualTo("R"));
        });
    }

    [Test]
    public void GetWordList_NewType_ReturnsEmptySet()
    {
        DummyTextConstants constants = new();
        HashSet<string> wordList = constants.GetWordList("Nouns");

        Assert.That(wordList, Is.Empty);
    }

    [Test]
    public void GetWordList_SameType_ReturnsCachedInstance()
    {
        DummyTextConstants constants = new();
        HashSet<string> first = constants.GetWordList("Nouns");
        HashSet<string> second = constants.GetWordList("Nouns");

        Assert.That(second, Is.SameAs(first));
    }

    [Test]
    public void CrypticMachineData_Defaults()
    {
        DummyTextConstants constants = new();

        Assert.Multiple(() =>
        {
            Assert.That(constants.CrypticMachineInfo.Charset, Is.EqualTo("│┤╡╢╖╕╣║╗╝╜╛┐└┴┬├─┼╞╟╚╔╩╦╠═╬╧╨╤╥╙╘╒╓╫╪┘┌"));
            Assert.That(constants.CrypticMachineInfo.WordLengthMin, Is.EqualTo(3));
            Assert.That(constants.CrypticMachineInfo.WordLengthMax, Is.EqualTo(10));
            Assert.That(constants.CrypticMachineInfo.SentenceLengthMin, Is.EqualTo(3));
            Assert.That(constants.CrypticMachineInfo.SentenceLengthMax, Is.EqualTo(40));
        });
    }

    [Test]
    public void ObfuscatorCharset_Default()
    {
        DummyTextConstants constants = new();

        Assert.That(constants.ObfuscatorCharset, Is.EqualTo("♣☼▬▲▼▓█▄▌▐▀°■"));
    }

    [Test]
    public void StaticGlyphConstants_Values()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DummyTextConstants.BOX_SINGLE_UP_DOWN_LEFT, Is.EqualTo("┤"));
            Assert.That(DummyTextConstants.BOX_SINGLE_DOWN_LEFT, Is.EqualTo("┐"));
            Assert.That(DummyTextConstants.BOX_SINGLE_UP_RIGHT, Is.EqualTo("└"));
            Assert.That(DummyTextConstants.BOX_SINGLE_UP_LEFT, Is.EqualTo("┘"));
            Assert.That(DummyTextConstants.BOX_SINGLE_DOWN_RIGHT, Is.EqualTo("┌"));
        });
    }

    [Test]
    public void WeirdSet_EmptyInput_ProducesEmptyReverse()
    {
        DummyTextConstants constants = new();

        constants.BuildWeirdReverse();

        Assert.That(constants.WeirdReverse, Is.Empty);
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
