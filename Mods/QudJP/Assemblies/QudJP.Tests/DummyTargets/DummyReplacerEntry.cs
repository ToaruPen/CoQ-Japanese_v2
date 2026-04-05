#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.Delegates.ReplacerEntry from decompiled beta (212.17).
/// Holds a single replacer/postprocessor registration: delegate, parameter types, default args, and flags.
/// </summary>
internal sealed class DummyReplacerEntry
{
    public DummyReplacer Delegate;

    public Type[] Parameters;

    public object[] Arguments;

    public string? Default;

    public int Flags;

    public int Priority = -1;

    public bool Capitalize;

    public DummyReplacerEntry(
        DummyReplacer @delegate,
        Type[] parameters,
        object[]? arguments = null,
        string? @default = null,
        int flags = 0,
        bool capitalize = false)
    {
        Delegate = @delegate;
        Parameters = parameters;
        Arguments = arguments ?? [];
        Default = @default;
        Flags = flags;
        Capitalize = capitalize;
    }
}

#pragma warning restore CA1707
