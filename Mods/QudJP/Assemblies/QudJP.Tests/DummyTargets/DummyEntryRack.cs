#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.World.Text.Delegates.EntryRack from decompiled beta (212.17).
/// A priority-sorted list of <see cref="DummyReplacerEntry"/> with type-based multi-dispatch.
/// Extends a simple resizable array (reproduced inline as <see cref="List{T}"/>).
/// </summary>
internal sealed class DummyEntryRack
{
    public string? Key;

    private readonly List<DummyReplacerEntry> _items = [];

    public int Length => _items.Count;

    public DummyReplacerEntry this[int index] => _items[index];

    public DummyEntryRack()
    {
    }

    public DummyEntryRack(string key)
    {
        Key = key;
    }

    /// <summary>
    /// Faithful to EntryRack.CalculatePriority (line 19-33).
    /// Sealed/value types get int.MaxValue (exact match only).
    /// Non-sealed reference types get min subclass depth across all params.
    /// </summary>
    private static int CalculatePriority(DummyReplacerEntry entry)
    {
        Type[] parameters = entry.Parameters;
        int num = int.MaxValue;
        for (int i = 0; i < parameters.Length; i++)
        {
            Type type = parameters[i];
            if (!type.IsValueType && !type.IsSealed)
            {
                num = Math.Min(num, GetSubclassDepth(type));
            }
        }

        return num;
    }

    /// <summary>
    /// Simplified reproduction of the extension method GetSubclassDepthWithGenerics.
    /// Counts inheritance depth from the type to <see cref="object"/>.
    /// </summary>
    private static int GetSubclassDepth(Type type)
    {
        int depth = 0;
        Type? current = type;
        while (current != null && current != typeof(object))
        {
            depth++;
            current = current.BaseType;
        }

        return depth;
    }

    /// <summary>
    /// Faithful to EntryRack.TryAdd (line 35-79).
    /// Checks for an existing entry with identical parameter types.
    /// If no match: inserts at priority-sorted position.
    /// If match found and Override=true: replaces. If Override=false: returns false.
    /// </summary>
    public bool TryAdd(DummyReplacerEntry entry, bool @override)
    {
        int existingIndex = -1;
        Type[] parameters = entry.Parameters;

        for (int i = 0; i < _items.Count; i++)
        {
            Type[] existingParams = _items[i].Parameters;
            if (parameters.Length != existingParams.Length)
            {
                continue;
            }

            bool match = true;
            for (int j = 0; j < parameters.Length; j++)
            {
                if (parameters[j] != existingParams[j])
                {
                    match = false;
                    break;
                }
            }

            if (match)
            {
                existingIndex = i;
                break;
            }
        }

        if (existingIndex == -1)
        {
            int insertAt = _items.Count;
            int priority = CalculatePriority(entry);
            entry.Priority = priority;

            // Higher priority values go later; lower values go earlier.
            // Walk backward to find insertion point: insert before items with
            // strictly lower priority (matching decompiled: while Items[num3-1].Priority < num4).
            while (insertAt > 0 && _items[insertAt - 1].Priority < priority)
            {
                insertAt--;
            }

            _items.Insert(insertAt, entry);
            return true;
        }

        if (@override)
        {
            _items[existingIndex] = entry;
            return true;
        }

        return false;
    }

    /// <summary>
    /// Faithful to EntryRack.TryFind (line 82-126).
    /// Matches arguments by count and type. Priority==int.MaxValue uses exact type equality;
    /// otherwise uses IsAssignableFrom (inheritance-aware).
    /// Returns first match in priority order.
    /// </summary>
    public bool TryFind(List<object> arguments, out DummyReplacerEntry entry)
    {
        for (int i = 0; i < _items.Count; i++)
        {
            DummyReplacerEntry candidate = _items[i];
            Type[] parameters = candidate.Parameters;
            int paramCount = parameters.Length;

            if (arguments.Count != paramCount)
            {
                continue;
            }

            bool match = true;
            if (candidate.Priority == int.MaxValue)
            {
                // Exact type match for sealed/value types
                for (int j = 0; j < paramCount; j++)
                {
                    if (arguments[j]?.GetType() != parameters[j])
                    {
                        match = false;
                        break;
                    }
                }
            }
            else
            {
                // Inheritance-aware match
                for (int k = 0; k < paramCount; k++)
                {
                    Type? argType = arguments[k]?.GetType();
                    if (!parameters[k].IsAssignableFrom(argType))
                    {
                        match = false;
                        break;
                    }
                }
            }

            if (match)
            {
                entry = candidate;
                return true;
            }
        }

        entry = null!;
        return false;
    }
}

#pragma warning restore CA1707
