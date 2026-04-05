#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.GenderedNoun from decompiled beta (212.17).
/// Represents a noun with gender/pronoun information for variable replacement.
/// </summary>
internal readonly struct DummyGenderedNoun
{
    public string Name { get; }

    /// <summary>
    /// Pronoun descriptor name (e.g., "neuter", "plural", "he", "she").
    /// Replaces <c>IPronounProvider</c> with a simple string for test portability.
    /// In the game, defaults to <c>Gender.DefaultNeuter</c>.
    /// </summary>
    public string PronounType { get; }

    public bool Proper { get; }

    public DummyGenderedNoun(string name, string? pronounType = null, bool proper = false)
    {
        Name = name;
        PronounType = pronounType ?? "neuter";
        Proper = proper;
    }

    /// <summary>
    /// Constructor matching the <c>GenderedNoun(string, bool, bool)</c> overload.
    /// The bool form picks "plural" vs "neuter".
    /// </summary>
    public DummyGenderedNoun(string name, bool explicitPlural, bool proper = false)
        : this(name, explicitPlural ? "plural" : "neuter", proper)
    {
    }

    public override string ToString() => $"{Name} ({PronounType})";
}

#pragma warning restore CA1707
