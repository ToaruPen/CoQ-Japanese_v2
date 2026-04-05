#pragma warning disable CA1707

using System.Runtime.CompilerServices;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful minimum of HistoryKitExtensions for ReplaceBuilder/SpiceContext glue.
/// </summary>
internal static class DummyHistoryKitExtensions
{
    private static readonly ConditionalWeakTable<DummyReplaceBuilder, DummySpiceContext> SpiceContexts = new();

    private static DummySpiceContext GetSpiceContext(DummyReplaceBuilder builder)
    {
        if (SpiceContexts.TryGetValue(builder, out DummySpiceContext? existing) && builder.HasArgument(existing))
        {
            return existing;
        }

        if (existing != null)
        {
            SpiceContexts.Remove(builder);
        }

        DummySpiceContext context = new();
        SpiceContexts.Add(builder, context);
        if (!builder.HasArgument(context))
        {
            builder.AddArgument(context, "spice", silent: true);
        }

        return context;
    }

    public static DummyReplaceBuilder AddSpiceVariable(this DummyReplaceBuilder builder, string key, string value)
    {
        if (key.Length > 2 && key[0] == '*' && key[^1] == '*')
        {
            string text = DummyHistoricStringExpander.ReformatStarVars(key);
            if (text.Length > 1 && text[0] == '=' && text[^1] == '=')
            {
                if (TryGetEmbeddedVariable(text, out string embeddedVariable))
                {
                    GetSpiceContext(builder).SetVariable(embeddedVariable, value);
                    return builder;
                }

                builder.AddArgument(value, text[1..^1]);
                return builder;
            }

            if (text.Length > 0 && text[0] == '$')
            {
                GetSpiceContext(builder).SetVariable(text, value);
                return builder;
            }
        }

        if (key.Length > 2 && key[0] == '=' && key[^1] == '=')
        {
            builder.AddArgument(value, key[1..^1]);
            return builder;
        }

        GetSpiceContext(builder).SetVariable(key, value);
        return builder;
    }

    private static bool TryGetEmbeddedVariable(string text, out string variable)
    {
        int index = text.IndexOf('$', StringComparison.Ordinal);
        if (index == -1)
        {
            variable = string.Empty;
            return false;
        }

        int end = index + 1;
        while (end < text.Length && (char.IsLetterOrDigit(text[end]) || text[end] == '_'))
        {
            end++;
        }

        if (end == index + 1)
        {
            variable = string.Empty;
            return false;
        }

        variable = text[index..end];
        return true;
    }

    public static DummyReplaceBuilder AddSpiceEntity(this DummyReplaceBuilder builder, DummyHistoricEntitySnapshot entity)
    {
        DummySpiceContext spiceContext = GetSpiceContext(builder);
        spiceContext.Entity = entity;
        spiceContext.History = entity.entity._history;
        return builder;
    }

    public static DummyReplaceBuilder AddSpiceEntity(this DummyReplaceBuilder builder, DummyHistoricEntity entity)
    {
        DummySpiceContext spiceContext = GetSpiceContext(builder);
        spiceContext.Entity = entity.GetCurrentSnapshot();
        spiceContext.History = entity._history;
        return builder;
    }

    public static DummyReplaceBuilder AddSpiceHistory(this DummyReplaceBuilder builder, DummyHistory history)
    {
        GetSpiceContext(builder).History = history;
        return builder;
    }
}

#pragma warning restore CA1707
