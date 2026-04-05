#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 golden tests for English-specific (language-dependent) postprocessors.
/// Golden values taken from decompiled [VariablePostProcessorExample] attributes
/// and Grammar method behavior in beta 212.17.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class EnglishPostProcessorGoldenTests
{
    private DummyVariableContext _ctx = null!;

    [SetUp]
    public void SetUp()
    {
        _ctx = new DummyVariableContext();
    }

    private void SetValue(string text)
    {
        _ctx.Value.Clear();
        _ctx.Value.Append(text);
    }

    private string GetValue() => _ctx.Value.ToString();

    // --- Pluralize golden tests (from decompiled [VariablePostProcessorExample]) ---

    // Core suffix rules
    [TestCase("box", "boxes")]          // -x → +es
    [TestCase("fox", "foxes")]          // -x → +es
    [TestCase("bush", "bushes")]        // -sh → +es
    [TestCase("church", "churches")]    // -ch → +es
    [TestCase("boss", "bosses")]        // -ss → +es
    [TestCase("quiz", "quizzes")]       // vowel+z → double z + es
    [TestCase("buzz", "buzzes")]        // -z → +es
    [TestCase("baby", "babies")]        // consonant+y → -ies
    [TestCase("day", "days")]           // vowel+y → +s
    [TestCase("tomato", "tomatoes")]    // -o (not after vowel/b) → +es
    [TestCase("tattoo", "tattoos")]     // -oo → +s
    [TestCase("cat", "cats")]           // regular → +s
    [TestCase("dog", "dogs")]           // regular → +s
    // Special suffix rules
    [TestCase("elf", "elves")]          // -elf → -elves
    [TestCase("wolf", "wolves")]        // -olf → -olves
    [TestCase("dwarf", "dwarves")]      // -arf → -arves
    [TestCase("half", "halves")]        // -alf → -alves
    [TestCase("knife", "knives")]       // -ife → -ives
    [TestCase("swordsman", "swordsmen")] // -man → -men
    public void Pluralize_SuffixRules(string input, string expected)
    {
        SetValue(input);
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    [Test]
    public void Pluralize_HumanIsRegular()
    {
        // "human" is explicitly excluded from -man → -men rule
        SetValue("human");
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("humans"));
    }

    [Test]
    public void Pluralize_MultiWord_PluralizesLastWord()
    {
        SetValue("iron sword");
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("iron swords"));
    }

    [Test]
    public void Pluralize_PrepositionPhrase_PluralizesBeforePreposition()
    {
        // "sword of flame" → pluralize "sword" only (preposition phrase preserved)
        SetValue("sword of flame");
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("swords of flame"));
    }

    [Test]
    public void Pluralize_EmptyString_ReturnsEmpty()
    {
        SetValue("");
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(""));
    }

    [Test]
    public void Pluralize_VariableRef_PrependsPluralizeTag()
    {
        SetValue("=name=");
        DummyEnglishPostProcessors.Pluralize(_ctx, []);
        Assert.That(GetValue(), Does.StartWith("=pluralize="));
    }

    // --- Article golden tests (from decompiled [VariablePostProcessorExample]) ---

    [TestCase("snapjaw", false, "a snapjaw")]
    [TestCase("apple", false, "an apple")]
    [TestCase("snapjaw", true, "A snapjaw")]
    [TestCase("apple", true, "An apple")]
    public void Article_GoldenTests(string input, bool capitalize, string expected)
    {
        SetValue(input);
        _ctx.Capitalize = capitalize;
        DummyEnglishPostProcessors.Article(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    [Test]
    public void Article_UniqueUsesA()
    {
        // "unique" starts with vowel but is an article exception
        SetValue("unique item");
        _ctx.Capitalize = false;
        DummyEnglishPostProcessors.Article(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("a unique item"));
    }

    [Test]
    public void Article_HonestUsesAn()
    {
        // "honest" starts with consonant but is an article exception
        SetValue("honest trader");
        _ctx.Capitalize = false;
        DummyEnglishPostProcessors.Article(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("an honest trader"));
    }

    // --- Possessive golden tests ---

    [TestCase("fox", "fox's")]
    [TestCase("James", "James'")]
    [TestCase("you", "your")]
    [TestCase("You", "Your")]
    public void Possessive_GoldenTests(string input, string expected)
    {
        SetValue(input);
        DummyEnglishPostProcessors.Possessive(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    [Test]
    public void Possessive_PreservesTrailingColorMarkup()
    {
        SetValue("fox}}");
        DummyEnglishPostProcessors.Possessive(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("fox's}}"));
    }

    // --- Title golden test (from decompiled [VariablePostProcessorExample]) ---

    [Test]
    public void Title_GoldenTest()
    {
        SetValue("q girl the quetzal");
        DummyEnglishPostProcessors.Title(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("Q Girl the Quetzal"));
    }

    // --- TitleCaseWithArticle golden test ---

    [Test]
    public void TitleCaseWithArticle_GoldenTest()
    {
        SetValue("an awesome thing");
        DummyEnglishPostProcessors.TitleCaseWithArticle(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("an Awesome Thing"));
    }

    // --- InitLowerIfArticle golden tests (from decompiled) ---

    [TestCase("Barathrum the Old", "Barathrum the Old")]  // No article prefix → unchanged
    [TestCase("A location", "a location")]                 // Starts with "A " → lowercase
    [TestCase("An egg", "an egg")]                         // Starts with "An " → lowercase
    [TestCase("The cave", "the cave")]                     // Starts with "The " → lowercase
    public void InitLowerIfArticle_GoldenTests(string input, string expected)
    {
        SetValue(input);
        DummyEnglishPostProcessors.InitLowerIfArticle(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    // --- TrimLeadingThe ---

    [TestCase("the cave", "cave")]
    [TestCase("The Library", "Library")]
    [TestCase("there", "there")]  // "ther" doesn't match "the " — too short
    public void TrimLeadingThe_GoldenTests(string input, string expected)
    {
        SetValue(input);
        DummyEnglishPostProcessors.TrimLeadingThe(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo(expected));
    }

    // --- ScanForAn ---

    [Test]
    public void ScanForAn_ConvertsAToAnBeforeVowel()
    {
        SetValue("a apple and a banana");
        DummyEnglishPostProcessors.ScanForAn(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("an apple and a banana"));
    }

    // --- MakeHedge ---

    [Test]
    public void MakeHedge_ReplacesPlantAndTreeWithHedge()
    {
        SetValue("oak tree");
        DummyEnglishPostProcessors.MakeHedge(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("oak hedge"));
    }

    [Test]
    public void MakeHedge_RemovesBothPlantAndTree()
    {
        SetValue("wild plant");
        DummyEnglishPostProcessors.MakeHedge(_ctx, []);
        Assert.That(GetValue(), Is.EqualTo("wild hedge"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
