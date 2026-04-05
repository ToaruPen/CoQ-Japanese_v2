#pragma warning disable CA1707

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.ReplaceBuilder from decompiled beta (212.17).
/// Fluent builder API for variable replacement: <c>Start().AddNoun().ToString()</c>.
/// <para>
/// Pooling is omitted (no concurrency in tests). Uses <see cref="StringBuilder"/>
/// in place of TextBuilder. Game-specific methods (EmitMessage, EmitFail) are omitted.
/// </para>
/// </summary>
internal sealed class DummyReplaceBuilder
{
    private const int FLAG_STRIP_COLORS = 1;
    private const int FLAG_THIRD_PERSON = 2;

    private readonly StringBuilder _intrinsic = new();
    private StringBuilder? _target;
    private readonly Dictionary<string, int> _aliases = [];
    private readonly List<object> _arguments = [];
    private int _flags;
    private string? _useColor;

    /// <summary>
    /// Optional target resolver injected for test flexibility.
    /// Passed through to <see cref="DummyGameText.Process"/>.
    /// </summary>
    public DummyGameText.TargetResolver? Resolver { get; set; }

    /// <summary>Whether ForceThirdPerson was called. Exposed for test assertions.</summary>
    public bool IsThirdPersonForced => (_flags & FLAG_THIRD_PERSON) != 0;

    // --- Start ---

    /// <summary>
    /// Faithful to ReplaceBuilder.Start(string) (line 106-111).
    /// Copies into intrinsic buffer, then uses intrinsic as target.
    /// </summary>
    public DummyReplaceBuilder Start(string text)
    {
        _intrinsic.Clear();
        _intrinsic.Append(text);
        _target = _intrinsic;
        _aliases.Clear();
        _arguments.Clear();
        _useColor = null;
        return this;
    }

    /// <summary>
    /// Faithful to ReplaceBuilder.Start(StringBuilder) (line 113-118).
    /// Copies content into intrinsic buffer, then uses intrinsic as target.
    /// Beta copies into intrinsic rather than using the caller's builder directly.
    /// </summary>
    public DummyReplaceBuilder Start(StringBuilder target)
    {
        _intrinsic.Clear();
        _intrinsic.Append(target);
        _target = _intrinsic;
        _aliases.Clear();
        _arguments.Clear();
        _useColor = null;
        return this;
    }

    // --- Color ---

    /// <summary>
    /// Faithful to ReplaceBuilder.Color (line 130-134).
    /// Stores color for post-process wrapping. Strips leading '&amp;'.
    /// </summary>
    public DummyReplaceBuilder Color(string? color)
    {
        _useColor = color?.TrimStart('&');
        return this;
    }

    // --- AddNoun ---

    /// <summary>
    /// Faithful to ReplaceBuilder.AddNoun(string, string, IPronounProvider, bool) (line 141-153).
    /// Creates a <see cref="DummyGenderedNoun"/> and adds to arguments.
    /// </summary>
    public DummyReplaceBuilder AddNoun(string? name, string? alias = null, string? pronounType = null, bool proper = false)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _arguments.Add(new DummyGenderedNoun(name, pronounType, proper));
            if (alias != null)
            {
                _aliases[alias] = _arguments.Count - 1;
            }
        }

        return this;
    }

    /// <summary>
    /// Faithful to ReplaceBuilder.AddNoun(string, bool, bool) (line 155-163).
    /// </summary>
    public DummyReplaceBuilder AddNoun(string? name, bool plural, bool proper = false)
    {
        if (!string.IsNullOrEmpty(name))
        {
            _arguments.Add(new DummyGenderedNoun(name, plural, proper));
        }

        return this;
    }

    // --- AddArgument ---

    /// <summary>
    /// Faithful to ReplaceBuilder.AddArgument (line 177-204).
    /// Null arguments are silently ignored when Silent=true.
    /// </summary>
    public DummyReplaceBuilder AddArgument(object? argument, string? alias = null, bool silent = false)
    {
        if (argument != null)
        {
            _arguments.Add(argument);
            if (alias != null)
            {
                _aliases[alias] = _arguments.Count - 1;
            }
        }

        return this;
    }

    /// <summary>
    /// Faithful to ReplaceBuilder.AddArguments&lt;T&gt;(IDictionary) (line 206-213).
    /// </summary>
    public DummyReplaceBuilder AddArguments<T>(IDictionary<string, T> arguments)
    {
        foreach (var kvp in arguments)
        {
            AddArgument(kvp.Value, kvp.Key);
        }

        return this;
    }

    // --- InsertArgument ---

    /// <summary>
    /// Faithful to ReplaceBuilder.InsertArgument (line 215-239).
    /// Inserts at index and shifts existing alias indices.
    /// </summary>
    public DummyReplaceBuilder InsertArgument(int index, object? argument, string? alias = null, bool silent = false)
    {
        if (index == _arguments.Count || argument == null)
        {
            return AddArgument(argument, alias, silent);
        }

        _arguments.Insert(index, argument);

        // Shift aliases at or after index
        List<string> keys = [.. _aliases.Keys];
        foreach (string key in keys)
        {
            if (_aliases[key] >= index)
            {
                _aliases[key]++;
            }
        }

        if (alias != null)
        {
            _aliases[alias] = index;
        }

        return this;
    }

    // --- AddGenerator ---

    /// <summary>
    /// Faithful to ReplaceBuilder.AddGenerator (line 246-249).
    /// </summary>
    public DummyReplaceBuilder AddGenerator(DummyArgumentGenerator generator, string? alias = null, bool silent = false)
    {
        return AddArgument(generator, alias, silent);
    }

    // --- AddAlias ---

    /// <summary>
    /// Faithful to ReplaceBuilder.AddAlias(string, int) (line 251-256).
    /// </summary>
    public DummyReplaceBuilder AddAlias(string alias, int index)
    {
        _aliases[alias] = index;
        return this;
    }

    // --- HasArgument ---

    /// <summary>
    /// Faithful to ReplaceBuilder.HasArgument (line 241-244).
    /// </summary>
    public bool HasArgument(object argument) => _arguments.Contains(argument);

    // --- Flags ---

    /// <summary>
    /// Faithful to ReplaceBuilder.StripColors (line 286-296).
    /// </summary>
    public DummyReplaceBuilder StripColors(bool value = true)
    {
        if (value)
        {
            _flags |= FLAG_STRIP_COLORS;
        }
        else
        {
            _flags &= ~FLAG_STRIP_COLORS;
        }

        return this;
    }

    /// <summary>
    /// Faithful to ReplaceBuilder.ForceThirdPerson (line 298-303).
    /// Sets FLAG_THIRD_PERSON. In decompiled, temporarily sets Grammar.AllowSecondPerson=false.
    /// </summary>
    public DummyReplaceBuilder ForceThirdPerson()
    {
        _flags |= FLAG_THIRD_PERSON;
        return this;
    }

    // --- Terminal methods ---

    /// <summary>
    /// Faithful to ReplaceBuilder.ToString (line 312-318).
    /// Calls Process(), returns result.
    /// </summary>
    public override string ToString()
    {
        Process();
        return _target?.ToString() ?? string.Empty;
    }

    /// <summary>
    /// Faithful to ReplaceBuilder.Process (line 383-409).
    /// Calls <see cref="DummyGameText.Process"/> with current state,
    /// then wraps in color tag if UseColor is set.
    /// </summary>
    private void Process()
    {
        if (_target == null)
        {
            return;
        }

        bool stripColors = (_flags & FLAG_STRIP_COLORS) != 0;
        DummyGameText.Process(_target, _arguments, _aliases, stripColors, Resolver);

        if (_useColor != null)
        {
            _target.Insert(0, "|");
            _target.Insert(0, _useColor);
            _target.Insert(0, "{{");
            _target.Append("}}");
        }
    }

    // --- Static factory (simplified, no pooling) ---

    /// <summary>
    /// Creates a new builder. Simplified from the pooled Get() (line 64-72).
    /// </summary>
    public static DummyReplaceBuilder Create() => new();
}

#pragma warning restore CA1707
