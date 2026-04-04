#pragma warning disable CA1515
#pragma warning disable CA1707

using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

[Category("L1")]
[TestFixture]
public sealed class MarkupTests
{
    [Test]
    public void Color_WrapsTextInMarkup()
    {
        Assert.That(DummyMarkup.Color("g", "leaf"), Is.EqualTo("{{g|leaf}}"));
    }

    [Test]
    public void Transform_HandlesSimpleAndNestedColorMarkup()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DummyMarkup.Transform("plain text"), Is.EqualTo("plain text"));
            Assert.That(DummyMarkup.Transform("{{y|keeps starting y}}"), Is.EqualTo("&ykeeps starting y"));
            Assert.That(DummyMarkup.Transform("small {{g|mossy}} tube"), Is.EqualTo("&ysmall &gmossy&y tube"));
            Assert.That(DummyMarkup.Transform("{{|small {{g|mossy}} tube}}"), Is.EqualTo("small &gmossy&y tube"));
            Assert.That(DummyMarkup.Transform("{{c|&Kwant black}}"), Is.EqualTo("&Kwant black"));
            Assert.That(DummyMarkup.Transform("{{y|want grey {{Y|white}} grey &Kblack}}"), Is.EqualTo("&ywant grey &Ywhite&y grey &Kblack"));
        });
    }

    [Test]
    public void Transform_UnclosedMarkup_RendersThroughEndOfString()
    {
        Assert.That(DummyMarkup.Transform("{{g|hi"), Is.EqualTo("&ghi"));
    }

    [Test]
    public void Strip_RemovesMarkupAndEmbeddedColorCodes()
    {
        Assert.Multiple(() =>
        {
            Assert.That(DummyMarkup.Strip(string.Empty), Is.EqualTo(string.Empty));
            Assert.That(DummyMarkup.Strip("&&^^"), Is.EqualTo("&^"));
            Assert.That(DummyMarkup.Strip("small {{g|mossy}} tube"), Is.EqualTo("small mossy tube"));
            Assert.That(DummyMarkup.Strip("{{|small {{g|mossy}} tube}}"), Is.EqualTo("small mossy tube"));
            Assert.That(DummyMarkup.Strip("{{g|hi"), Is.EqualTo("hi"));
            Assert.That(DummyMarkup.Strip("{{y|want grey {{Y|white}} grey &Kblack}}"), Is.EqualTo("want grey white grey black"));
            Assert.That(DummyMarkup.Strip("{{g|漢字}}"), Is.EqualTo("漢字"));
        });
    }
}

#pragma warning restore CA1515
#pragma warning restore CA1707
