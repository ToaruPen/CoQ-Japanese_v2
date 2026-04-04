using System.Collections.Generic;

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyStringsLoader
{
    public Dictionary<string, string> Strings { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, Dictionary<string, string>> CStrings { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, int> OrderAdjust { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, Dictionary<string, int>> COrders { get; } = new(StringComparer.Ordinal);

    public Dictionary<string, string> ContextStrings(string? context)
    {
        if (string.IsNullOrEmpty(context))
        {
            return Strings;
        }

        if (!CStrings.TryGetValue(context, out Dictionary<string, string>? value))
        {
            value = new Dictionary<string, string>(StringComparer.Ordinal);
            CStrings.Add(context, value);
        }

        return value;
    }

    public Dictionary<string, int> ContextOrders(string? context)
    {
        if (string.IsNullOrEmpty(context))
        {
            return OrderAdjust;
        }

        if (!COrders.TryGetValue(context, out Dictionary<string, int>? value))
        {
            value = new Dictionary<string, int>(StringComparer.Ordinal);
            COrders.Add(context, value);
        }

        return value;
    }

    public void HandleStringEntry(string? context, string id, string value, int? orderAdjust)
    {
        ContextStrings(context)[id] = value;

        if (!string.IsNullOrEmpty(id))
        {
            Strings.TryAdd(id, value);
        }

        if (orderAdjust.HasValue)
        {
            ContextOrders(context)[id] = orderAdjust.Value;
        }

        if (orderAdjust.HasValue && !string.IsNullOrEmpty(context))
        {
            OrderAdjust[id] = orderAdjust.Value;
        }
    }
}
