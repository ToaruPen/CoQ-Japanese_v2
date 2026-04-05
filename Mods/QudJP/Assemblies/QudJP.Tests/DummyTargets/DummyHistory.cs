#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of HistoryKit.History for SpiceContext glue tests.
/// Keeps deterministic random, current year, and entity lookup.
/// </summary>
internal sealed class DummyHistory
{
    private readonly Dictionary<string, DummyHistoricEntity> _entityById = new(StringComparer.Ordinal);
    private Random? _r;

    public List<DummyHistoricEntity> entities = [];

    public long startingYear;

    public long currentYear;

    public Random r
    {
        get => _r ??= new Random(0);
        set => _r = value;
    }

    public DummyHistory(long startingYear, Random? random = null)
    {
        _r = random;
        this.startingYear = startingYear;
        currentYear = startingYear;
    }

    public DummyHistoricEntity? GetEntity(string name)
    {
        if (_entityById.TryGetValue(name, out DummyHistoricEntity? entity))
        {
            return entity;
        }

        foreach (DummyHistoricEntity item in entities)
        {
            if (item.name == name)
            {
                return item;
            }
        }

        return null;
    }

    internal void RegisterEntity(DummyHistoricEntity entity)
    {
        if (!entities.Contains(entity))
        {
            entities.Add(entity);
        }

        if (!string.IsNullOrEmpty(entity.id))
        {
            _entityById[entity.id] = entity;
        }
    }

    internal void UpdateEntityId(DummyHistoricEntity entity, string? previousId, string? nextId)
    {
        if (!string.IsNullOrEmpty(previousId))
        {
            _entityById.Remove(previousId);
        }

        if (!string.IsNullOrEmpty(nextId))
        {
            _entityById[nextId] = entity;
        }
    }
}

#pragma warning restore CA1707
