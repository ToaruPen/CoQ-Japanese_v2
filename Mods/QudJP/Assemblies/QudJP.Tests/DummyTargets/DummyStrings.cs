#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

internal sealed class DummyStrings
{
    private readonly DummyStringsLoader _loader;

    public DummyStrings(DummyStringsLoader loader)
    {
        _loader = loader;
    }

    public bool DebugEnabled { get; set; }

    public bool DebugMisses { get; set; }

    public string ActiveLanguage { get; set; } = "en";

    public bool TryGetString(string? context, string id, out string result)
    {
        if (!string.IsNullOrEmpty(context))
        {
            Dictionary<string, string> contextStrings = _loader.ContextStrings(context);
            if (contextStrings.TryGetValue(id, out string? contextResult))
            {
                result = contextResult;
                return true;
            }
        }

        if (_loader.ContextStrings(null).TryGetValue(id, out string? globalResult))
        {
            result = globalResult;
            return true;
        }

        result = id;
        return false;
    }

    public bool TryGetOrder(string? context, string id, out int result)
    {
        if (!string.IsNullOrEmpty(context))
        {
            Dictionary<string, int> contextOrders = _loader.ContextOrders(context);
            if (contextOrders.TryGetValue(id, out int contextResult))
            {
                result = contextResult;
                return true;
            }
        }

        if (_loader.ContextOrders(null).TryGetValue(id, out int globalResult))
        {
            result = globalResult;
            return true;
        }

        result = 0;
        return false;
    }

    public string _S(string id)
    {
        if (DebugEnabled)
        {
            return "_S:" + id;
        }

        TryGetString(null, id, out string result);
        return result;
    }

    public string _S(string? context, string id)
    {
        if (DebugEnabled)
        {
            return "_S:" + context + ":" + id;
        }

        TryGetString(context, id, out string result);
        return result;
    }

    public string _S(string? context, string id, int defaultOrder, out int orderOut)
    {
        orderOut = 0;

        if (DebugEnabled)
        {
            return "_S:" + context + ":" + id;
        }

        if (TryGetOrder(context, id, out int result))
        {
            orderOut = result;
        }
        else
        {
            orderOut = defaultOrder;
        }

        TryGetString(context, id, out string stringResult);
        return stringResult;
    }
}

#pragma warning restore CA1707
