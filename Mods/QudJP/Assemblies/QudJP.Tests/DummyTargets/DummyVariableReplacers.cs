#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.Delegates.VariableReplacers from decompiled beta (212.17).
/// Static registry of replacer and postprocessor delegates, keyed by name.
/// <para>
/// In the game, <c>LoadReplacers()</c> scans all types via reflection.
/// This test version provides manual registration via <see cref="Register"/> and
/// <see cref="RegisterPost"/> for deterministic testing without reflection.
/// </para>
/// </summary>
internal static class DummyVariableReplacers
{
    /// <summary>Replacer map: key → EntryRack. Faithful to VariableReplacers.Map.</summary>
    public static Dictionary<string, DummyEntryRack> Map { get; private set; } = [];

    /// <summary>PostProcessor map: key → EntryRack. Faithful to VariableReplacers.PostMap.</summary>
    public static Dictionary<string, DummyEntryRack> PostMap { get; private set; } = [];

    /// <summary>
    /// Default rack used when a variable's key section resolves to an argument (not a named replacer).
    /// Set to Map["toString"] after registration, matching VariableReplacers.DefaultRack (line 58).
    /// </summary>
    public static DummyEntryRack? DefaultRack { get; set; }

    /// <summary>Faithful to VariableReplacers.Initialized.</summary>
    public static bool Initialized { get; set; }

    /// <summary>
    /// Registers a replacer entry under the given keys in <see cref="Map"/>.
    /// Simplified version of the reflection-based LoadReplacers registration loop.
    /// </summary>
    public static void Register(string[] keys, DummyReplacerEntry entry, bool @override = false)
    {
        foreach (string key in keys)
        {
            if (!Map.TryGetValue(key, out var rack))
            {
                rack = new DummyEntryRack(key);
                Map[key] = rack;
            }

            rack.TryAdd(entry, @override);
        }
    }

    /// <summary>
    /// Convenience: register a simple replacer (no typed arguments beyond VariableContext).
    /// </summary>
    public static void Register(string key, DummyReplacer @delegate, string? @default = null, bool capitalize = false)
    {
        Register(
            [key],
            new DummyReplacerEntry(@delegate, [], @default: @default, capitalize: capitalize));
    }

    /// <summary>
    /// Registers a postprocessor entry under the given keys in <see cref="PostMap"/>.
    /// </summary>
    public static void RegisterPost(string[] keys, DummyReplacerEntry entry, bool @override = false)
    {
        foreach (string key in keys)
        {
            if (!PostMap.TryGetValue(key, out var rack))
            {
                rack = new DummyEntryRack(key);
                PostMap[key] = rack;
            }

            rack.TryAdd(entry, @override);
        }
    }

    /// <summary>
    /// Convenience: register a simple postprocessor (no typed arguments beyond VariableContext).
    /// </summary>
    public static void RegisterPost(string key, DummyReplacer @delegate, string? @default = null, bool capitalize = false)
    {
        RegisterPost(
            [key],
            new DummyReplacerEntry(@delegate, [], @default: @default, capitalize: capitalize));
    }

    /// <summary>
    /// Faithful to VariableReplacers.Reset() ([ModSensitiveCacheInit]).
    /// Clears all maps and sets Initialized=false.
    /// </summary>
    public static void Reset()
    {
        Map.Clear();
        PostMap.Clear();
        DefaultRack = null;
        Initialized = false;
    }

    /// <summary>
    /// Finalizes initialization by setting DefaultRack = Map["toString"].
    /// Call after registering all replacers.
    /// Faithful to the end of LoadReplacers (line 571).
    /// </summary>
    public static void FinalizeInit()
    {
        if (Map.TryGetValue("toString", out var rack))
        {
            DefaultRack = rack;
        }

        Initialized = true;
    }
}

#pragma warning restore CA1707
