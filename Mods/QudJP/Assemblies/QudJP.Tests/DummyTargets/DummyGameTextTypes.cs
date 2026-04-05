#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.TargetType from decompiled beta (212.17).
/// </summary>
internal enum DummyTargetType
{
    None,
    Subject,
    Object,
    Player,
}

/// <summary>
/// Faithful reproduction of XRL.World.Text.Delegates.Replacer delegate from decompiled beta (212.17).
/// Signature: <c>string Replacer(VariableContext Context, object[] Arguments)</c>.
/// </summary>
internal delegate string? DummyReplacer(DummyVariableContext context, object[] arguments);

/// <summary>
/// Faithful reproduction of XRL.World.Text.ArgumentGenerator delegate from decompiled beta (212.17).
/// Lazily evaluated when first accessed during TryParseArgument.
/// </summary>
internal delegate object DummyArgumentGenerator();

#pragma warning restore CA1707
