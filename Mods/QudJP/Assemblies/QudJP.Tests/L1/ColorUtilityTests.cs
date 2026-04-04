#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class ColorUtilityTests
{
    [Test]
    public void StripFormatting_RemovesCodesAndMarkup_PreservingPrintableContent()
    {
        string stripped = DummyColorUtility.StripFormatting("start &rred ^Bsky {{g|leaf}} && ^^ end");

        Assert.That(stripped, Is.EqualTo("start red sky leaf & ^ end"));
    }

    [Test]
    public void HasFormatting_DistinguishesRealFormattingFromEscapes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DummyColorUtility.HasFormatting(null), Is.False);
            Assert.That(DummyColorUtility.HasFormatting(string.Empty), Is.False);
            Assert.That(DummyColorUtility.HasFormatting("plain text"), Is.False);
            Assert.That(DummyColorUtility.HasFormatting("&&^^"), Is.False);
            Assert.That(DummyColorUtility.HasFormatting("trail&"), Is.False);
            Assert.That(DummyColorUtility.HasFormatting("trail^"), Is.False);
            Assert.That(DummyColorUtility.HasFormatting("&yhello"), Is.True);
            Assert.That(DummyColorUtility.HasFormatting("^Khello"), Is.True);
            Assert.That(DummyColorUtility.HasFormatting("{{g|leaf}}"), Is.True);
        });
    }

    [Test]
    public void LengthExceptFormatting_CountsPrintableGraphemes()
    {
        int length = DummyColorUtility.LengthExceptFormatting("{{g|漢A字}}&&^^");

        Assert.That(length, Is.EqualTo(5));
    }

    [Test]
    public void ClipExceptFormatting_ClipsByGraphemeAndAutoClosesOpenMarkup()
    {
        string? clipped = DummyColorUtility.ClipExceptFormatting("{{y|ab{{g|漢字}}cd}}", 3);

        Assert.That(clipped, Is.EqualTo("{{y|ab{{g|漢}}}}"));
    }

    [Test]
    public void EscapeFormatting_DoublesAmpersandsAndCarets()
    {
        string? escaped = DummyColorUtility.EscapeFormatting("A&B^C&&^^");

        Assert.That(escaped, Is.EqualTo("A&&B^^C&&&&^^^^"));
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
