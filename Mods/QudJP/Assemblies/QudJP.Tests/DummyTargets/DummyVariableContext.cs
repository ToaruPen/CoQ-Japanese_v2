#pragma warning disable CA1707

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.Delegates.VariableContext from decompiled beta (212.17).
/// Decouples parameters from delegate signature for mod compatibility.
/// Uses <see cref="StringBuilder"/> in place of game's TextBuilder/CompositedTextBuilder.
/// </summary>
internal sealed class DummyVariableContext
{
    /// <summary>
    /// The raw arguments supplied to game text processing.
    /// Faithful to VariableContext.Invocation (line 11).
    /// </summary>
    public readonly DummyGameTextInvocation Invocation = new();

    /// <summary>
    /// A string builder which is either empty (replacers) or contains the currently
    /// processing value (post processor). If the replacer returns null/void, this will
    /// be used to construct a return value.
    /// Faithful to VariableContext.Value (CompositedTextBuilder(128) in decompiled).
    /// </summary>
    public readonly StringBuilder Value = new(128);

    /// <summary>A list of text provided parameters (from ':' sections).</summary>
    public List<string> Parameters = [];

    /// <summary>The special target type, useful in sentences where special logic is applied for subjects and objects.</summary>
    public DummyTargetType Type;

    /// <summary>An optional default string, defined in attribute.</summary>
    public string? Default;

    /// <summary>Whether the output should be capitalized.</summary>
    public bool Capitalize;

    /// <summary>Attribute flags, mod field.</summary>
    public int Flags;

    /// <summary>
    /// A map of arbitrary state which is only cleared after the variable is finished processing.
    /// Faithful to VariableContext.State (StringMap&lt;object&gt; in decompiled).
    /// </summary>
    private readonly Dictionary<string, object> _state = [];

    public void SetState(string key, object value)
    {
        _state[key] = value;
    }

    public T GetState<T>(string key)
    {
        TryGetState<T>(key, out var value);
        return value!;
    }

    public T GetState<T>(string key, T defaultValue)
    {
        if (!TryGetState<T>(key, out var value))
        {
            return defaultValue;
        }

        return value!;
    }

    public bool TryGetState<T>(string key, out T? value)
    {
        if (_state.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void ClearState()
    {
        _state.Clear();
    }
}

/// <summary>
/// Faithful reproduction of XRL.GameText.Invocation from decompiled beta (212.17, line 148-222).
/// Holds per-process-call state: the message being built, supplied arguments, and aliases.
/// </summary>
internal sealed class DummyGameTextInvocation
{
    public StringBuilder? Message;

    public List<object>? Arguments;

    public Dictionary<string, int>? Aliases;

    /// <summary>
    /// A map of arbitrary state which is only cleared after all processing is finished.
    /// Faithful to Invocation.State (StringMap&lt;object&gt;).
    /// </summary>
    private readonly Dictionary<string, object> _state = [];

    public object? Subject => (Arguments != null && Arguments.Count > 0) ? Arguments[0] : null;

    public object? Object => (Arguments != null && Arguments.Count > 1) ? Arguments[1] : null;

    public void SetState(string key, object value)
    {
        _state[key] = value;
    }

    public T GetState<T>(string key)
    {
        TryGetState<T>(key, out var value);
        return value!;
    }

    public T GetState<T>(string key, T defaultValue)
    {
        if (!TryGetState<T>(key, out var value))
        {
            return defaultValue;
        }

        return value!;
    }

    public bool TryGetState<T>(string key, out T? value)
    {
        if (_state.TryGetValue(key, out var raw) && raw is T typed)
        {
            value = typed;
            return true;
        }

        value = default;
        return false;
    }

    public void Clear()
    {
        Message = null;
        Arguments = null;
        Aliases = null;
        _state.Clear();
    }
}

#pragma warning restore CA1707
