#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Japanese-specific string replacer overrides for the variable expansion system.
/// These simulate <c>[HasVariableReplacer(Lang="ja", Override=true)]</c> registrations.
/// </summary>
internal static class DummyJapaneseStringReplacers
{
    /// <summary>
    /// Japanese adjectify: no-op.
    /// Returns the input argument unchanged.
    /// </summary>
    public static string Adjectify(DummyVariableContext ctx, object[] arguments)
    {
        return arguments.Length > 0
            ? arguments[0]?.ToString() ?? string.Empty
            : ctx.Value.ToString();
    }

    /// <summary>
    /// Registers all Japanese-specific string replacer overrides into
    /// <see cref="DummyVariableReplacers"/> with Override=true.
    /// </summary>
    public static void RegisterAll()
    {
        DummyVariableReplacers.Register(
            ["adjectify"],
            new DummyReplacerEntry(Adjectify, [typeof(string)]),
            @override: true);
    }
}

#pragma warning restore CA1707
