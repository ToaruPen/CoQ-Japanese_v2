#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of HistoryKit.HistoricEntitySnapshot for SpiceContext glue tests.
/// Provides scalar and list property lookup against the current entity snapshot.
/// </summary>
internal sealed class DummyHistoricEntitySnapshot
{
    public DummyHistoricEntity entity;

    public Dictionary<string, string> properties = new(StringComparer.Ordinal);

    public Dictionary<string, List<string>> listProperties = new(StringComparer.Ordinal);

    public DummyHistoricEntitySnapshot(DummyHistoricEntity sourceEntity)
    {
        entity = sourceEntity;
    }

    public string GetProperty(string name, string defaultValue = "unknown")
    {
        return properties.TryGetValue(name, out string? value) ? value : defaultValue;
    }

    public List<string> GetList(string name)
    {
        return listProperties.TryGetValue(name, out List<string>? value) ? value : [];
    }
}

#pragma warning restore CA1707
