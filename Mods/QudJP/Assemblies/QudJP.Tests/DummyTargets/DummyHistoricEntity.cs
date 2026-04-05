#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of HistoryKit.HistoricEntity for SpiceContext glue tests.
/// Holds a back-reference to history and exposes the current snapshot.
/// </summary>
internal sealed class DummyHistoricEntity
{
    private string _id = string.Empty;
    private string _name = string.Empty;

    public string id
    {
        get => _id;
        set
        {
            string previousId = _id;
            _id = value;
            _history.UpdateEntityId(this, previousId, value);
        }
    }

    public string name
    {
        get => _name;
        set
        {
            _name = value;
            if (CurrentSnapshot.properties.ContainsKey("name"))
            {
                CurrentSnapshot.properties["name"] = value;
            }
            else if (!string.IsNullOrEmpty(value))
            {
                CurrentSnapshot.properties.TryAdd("name", value);
            }
        }
    }

    public DummyHistory _history;

    public DummyHistoricEntitySnapshot CurrentSnapshot { get; set; }

    public DummyHistoricEntity(DummyHistory history, string? name = null)
    {
        _history = history;
        CurrentSnapshot = new DummyHistoricEntitySnapshot(this);
        this.name = name ?? string.Empty;
        history.RegisterEntity(this);
    }

    public DummyHistoricEntitySnapshot GetCurrentSnapshot() => CurrentSnapshot;

    public DummyHistoricEntitySnapshot GetSnapshotAtYear(long year)
    {
        _ = year;
        return CurrentSnapshot;
    }
}

#pragma warning restore CA1707
