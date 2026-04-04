#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

[AttributeUsage(AttributeTargets.Class, AllowMultiple = false, Inherited = false)]
internal sealed class DummyLanguageProviderAttribute(string code) : Attribute
{
    public string Code { get; } = code;
}

#pragma warning restore CA1707
