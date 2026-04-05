#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of XRL.World.Text.Delegates.SpiceContext from beta 212.17.
/// Provides history/entity state plus lazily created variable maps.
/// </summary>
internal sealed class DummySpiceContext
{
    public DummyHistory? History;

    public DummyHistoricEntitySnapshot? Entity;

    public Dictionary<string, string>? Variables;

    public Dictionary<string, object>? NodeVariables;

    public Random Random
    {
        get
        {
            if (History == null)
            {
                History = new DummyHistory(0L, new Random(0));
            }

            return History.r;
        }

        set
        {
            if (History == null)
            {
                History = new DummyHistory(0L, value);
            }
            else
            {
                History.r = value;
            }
        }
    }

    public DummySpiceContext()
    {
    }

    public DummySpiceContext(DummyHistoricEntitySnapshot entity)
        : this()
    {
        Entity = entity;
        History = entity.entity._history;
    }

    public DummySpiceContext(DummyHistoricEntity entity)
        : this(entity.GetCurrentSnapshot())
    {
    }

    public DummySpiceContext(DummyHistory history)
        : this()
    {
        History = history;
    }

    public void SetNodeVariable(string key, object value)
    {
        NodeVariables ??= new Dictionary<string, object>(StringComparer.Ordinal);
        NodeVariables[key] = value;
    }

    public void SetVariable(string key, string value)
    {
        if (key.Length == 0 || key[0] != '$')
        {
            return;
        }

        Variables ??= new Dictionary<string, string>(StringComparer.Ordinal);
        Variables[key] = value;
    }

    public override string ToString()
    {
        return $"SpiceContext(Entity: {Entity}, Variables: {Variables?.Count ?? 0})";
    }
}

#pragma warning restore CA1707
