#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of SpiceContextDelegates from beta 212.17.
/// Manual registration replaces attribute discovery for deterministic L1 tests.
/// </summary>
internal static class DummySpiceContextDelegates
{
    public static string ToString(DummyVariableContext context, DummySpiceContext spice)
    {
        if (context.Parameters.Count == 0)
        {
            return "spice";
        }

        DummyGameTextInvocation invocation = context.Invocation;
        context.Value.Append(DummyHistoricStringExpander.DelegateExpandQuery(spice, "spice", context.Parameters[0]));
        DummyGameText.Process(context.Value, invocation.Arguments, invocation.Aliases);
        return context.Value.ToString();
    }

    public static string ToString(DummyVariableContext context, DummySpiceContext spice, string query)
    {
        DummyGameTextInvocation invocation = context.Invocation;
        context.Value.Append(DummyHistoricStringExpander.DelegateExpandQuery(spice, "spice", query));
        DummyGameText.Process(context.Value, invocation.Arguments, invocation.Aliases);
        return context.Value.ToString();
    }

    public static string Entity(DummyVariableContext context, DummySpiceContext spice)
    {
        if (context.Parameters.Count == 0)
        {
            return "spice";
        }

        DummyGameTextInvocation invocation = context.Invocation;
        context.Value.Append(DummyHistoricStringExpander.DelegateExpandQuery(spice, "entity", context.Parameters[0]));
        DummyGameText.Process(context.Value, invocation.Arguments, invocation.Aliases);
        return context.Value.ToString();
    }

    public static string Set(DummyVariableContext context, DummySpiceContext spice)
    {
        if (context.Parameters.Count < 2)
        {
            return "spice";
        }

        context.Value.Append(DummyHistoricStringExpander.DelegateExpandQuery(spice, "spice", context.Parameters[0]));
        spice.SetNodeVariable(context.Parameters[1], context.Value.ToString());
        return string.Empty;
    }

    public static string SetEntity(DummyVariableContext context, DummySpiceContext spice)
    {
        if (context.Parameters.Count < 2)
        {
            return "spice";
        }

        context.Value.Append(DummyHistoricStringExpander.DelegateExpandQuery(spice, "entity", context.Parameters[0]));
        spice.SetNodeVariable(context.Parameters[1], context.Value.ToString());
        return string.Empty;
    }

    public static string nameItemAdjectiveRoot(DummyVariableContext context, DummySpiceContext spice, string adjective)
    {
        throw new NotImplementedException();
    }

    public static string nameItemNounRoot(DummyVariableContext context, DummySpiceContext spice, string noun)
    {
        throw new NotImplementedException();
    }

    public static void Register()
    {
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry((context, arguments) => ToString(context, (DummySpiceContext)arguments[0]), [typeof(DummySpiceContext)]));
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry((context, arguments) => ToString(context, (DummySpiceContext)arguments[0], (string)arguments[1]), [typeof(DummySpiceContext), typeof(string)]));
        DummyVariableReplacers.Register(
            ["entity"],
            new DummyReplacerEntry((context, arguments) => Entity(context, (DummySpiceContext)arguments[0]), [typeof(DummySpiceContext)]));
        DummyVariableReplacers.Register(
            ["set"],
            new DummyReplacerEntry((context, arguments) => Set(context, (DummySpiceContext)arguments[0]), [typeof(DummySpiceContext)]));
        DummyVariableReplacers.Register(
            ["set.entity"],
            new DummyReplacerEntry((context, arguments) => SetEntity(context, (DummySpiceContext)arguments[0]), [typeof(DummySpiceContext)]));
        DummyVariableReplacers.Register(
            ["nameItemAdjectiveRoot"],
            new DummyReplacerEntry((context, arguments) => nameItemAdjectiveRoot(context, (DummySpiceContext)arguments[0], (string)arguments[1]), [typeof(DummySpiceContext), typeof(string)]));
        DummyVariableReplacers.Register(
            ["nameItemNounRoot"],
            new DummyReplacerEntry((context, arguments) => nameItemNounRoot(context, (DummySpiceContext)arguments[0], (string)arguments[1]), [typeof(DummySpiceContext), typeof(string)]));
    }
}

#pragma warning restore CA1707
