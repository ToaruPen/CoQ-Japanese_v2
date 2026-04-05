#pragma warning disable CA1707
#pragma warning disable CA1308 // Lower() is faithful to decompiled source using ToLowerInvariant

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of language-independent postprocessors from
/// XRL.World.Text.Delegates.PostProcessors (decompiled beta 212.17).
/// <para>
/// All postprocessors mutate <see cref="DummyVariableContext.Value"/> in-place
/// and return <c>null</c> (convention: void in decompiled, null from delegate shim).
/// </para>
/// <para>
/// Language-dependent postprocessors (pluralize, article, possessive, etc.)
/// are deferred to Phase 10d/10e.
/// </para>
/// </summary>
internal static class DummyPostProcessors
{
    /// <summary>
    /// Uppercases the first character of Value.
    /// Keys: "capitalize", "initUpper".
    /// Faithful to PostProcessors.Capitalize (line ~20).
    /// </summary>
    public static string? Capitalize(DummyVariableContext ctx, object[] _)
    {
        InitUpper(ctx.Value);
        return null;
    }

    /// <summary>
    /// Uppercases the first character of each line in Value.
    /// Key: "capEachLine".
    /// Faithful to PostProcessors.CapEachLine.
    /// </summary>
    public static string? CapEachLine(DummyVariableContext ctx, object[] _)
    {
        InitUpperEachLine(ctx.Value);
        return null;
    }

    /// <summary>
    /// Lowercases all characters in Value.
    /// Key: "lower".
    /// Faithful to PostProcessors.Lower.
    /// </summary>
    public static string? Lower(DummyVariableContext ctx, object[] _)
    {
        string text = ctx.Value.ToString().ToLowerInvariant();
        ctx.Value.Clear();
        ctx.Value.Append(text);
        return null;
    }

    /// <summary>
    /// Uppercases all characters in Value.
    /// Key: "upper".
    /// Faithful to PostProcessors.Upper.
    /// </summary>
    public static string? Upper(DummyVariableContext ctx, object[] _)
    {
        string text = ctx.Value.ToString().ToUpperInvariant();
        ctx.Value.Clear();
        ctx.Value.Append(text);
        return null;
    }

    /// <summary>
    /// Lowercases the first character of Value.
    /// Key: "initLower".
    /// Faithful to PostProcessors.InitLower (simplified: skips player name check).
    /// </summary>
    public static string? InitLower(DummyVariableContext ctx, object[] _)
    {
        InitLowerChar(ctx.Value);
        return null;
    }

    /// <summary>
    /// Strips color markup from Value.
    /// Key: "strip".
    /// Faithful to PostProcessors.Strip → Markup.Strip(Context.Value).
    /// </summary>
    public static string? Strip(DummyVariableContext ctx, object[] _)
    {
        string stripped = DummyColorUtility.StripFormatting(ctx.Value.ToString());
        ctx.Value.Clear();
        ctx.Value.Append(stripped);
        return null;
    }

    /// <summary>
    /// Wraps Value in color markup: <c>{{color|text}}</c>.
    /// Uses parameter or typed argument for color name.
    /// Key: "color".
    /// Faithful to PostProcessors.Color.
    /// </summary>
    public static string? Color(DummyVariableContext ctx, object[] args)
    {
        if (ctx.Value.Length == 0)
        {
            return null;
        }

        // Determine color: typed arg first, then first parameter
        string? color = (args.Length > 0 ? args[0] as string : null)
                        ?? (ctx.Parameters.Count > 0 ? ctx.Parameters[0] : null);
        if (color == null)
        {
            return null;
        }

        ctx.Value.Insert(0, "{{" + color + "|");
        ctx.Value.Append("}}");
        return null;
    }

    /// <summary>
    /// Wraps Value in <c>{{rules|text}}</c>.
    /// Key: "rules".
    /// Faithful to PostProcessors.Rules → Color(Context, "rules").
    /// </summary>
    public static string? Rules(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0)
        {
            ctx.Value.Insert(0, "{{rules|");
            ctx.Value.Append("}}");
        }

        return null;
    }

    /// <summary>
    /// Wraps Value in <c>{{|text}}</c> — guards against leaking old color formats.
    /// Key: "colorSafe".
    /// Faithful to PostProcessors.ColorSafe → Color(Context, "").
    /// </summary>
    public static string? ColorSafe(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0)
        {
            ctx.Value.Insert(0, "{{|");
            ctx.Value.Append("}}");
        }

        return null;
    }

    /// <summary>
    /// Appends a space after Value, unless empty, ends in whitespace, or ends in hyphen.
    /// Key: "spaceAfter".
    /// Faithful to PostProcessors.SpaceAfter.
    /// </summary>
    public static string? SpaceAfter(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0)
        {
            char c = ctx.Value[^1];
            if (c != '-' && !char.IsWhiteSpace(c))
            {
                ctx.Value.Append(' ');
            }
        }

        return null;
    }

    /// <summary>
    /// Appends a space after Value, unless empty or ends in whitespace.
    /// Unlike SpaceAfter, will add space after hyphens.
    /// Key: "spaceAfterEvenIfHyphen".
    /// Faithful to PostProcessors.SpaceAfterEvenIfHyphen.
    /// </summary>
    public static string? SpaceAfterEvenIfHyphen(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0 && !char.IsWhiteSpace(ctx.Value[^1]))
        {
            ctx.Value.Append(' ');
        }

        return null;
    }

    /// <summary>
    /// Appends a colon after Value, unless empty or ends in whitespace.
    /// Key: "colonAfter".
    /// Faithful to PostProcessors.ColonAfter.
    /// </summary>
    public static string? ColonAfter(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0 && !char.IsWhiteSpace(ctx.Value[^1]))
        {
            ctx.Value.Append(':');
        }

        return null;
    }

    /// <summary>
    /// Prepends a space before Value, unless empty or starts with whitespace.
    /// Key: "spaceBefore".
    /// Faithful to PostProcessors.SpaceBefore.
    /// </summary>
    public static string? SpaceBefore(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length != 0 && !char.IsWhiteSpace(ctx.Value[0]))
        {
            ctx.Value.Insert(0, ' ');
        }

        return null;
    }

    /// <summary>
    /// Removes all leading and trailing whitespace.
    /// Key: "trim".
    /// Faithful to PostProcessors.Trim.
    /// </summary>
    public static string? Trim(DummyVariableContext ctx, object[] _)
    {
        string trimmed = ctx.Value.ToString().Trim();
        ctx.Value.Clear();
        ctx.Value.Append(trimmed);
        return null;
    }

    /// <summary>
    /// Replaces double spaces with single spaces (single-pass, no loop).
    /// Key: "slim".
    /// Faithful to PostProcessors.Slim — beta calls Replace once without looping,
    /// so runs of 3+ spaces are intentionally NOT fully collapsed.
    /// </summary>
    public static string? Slim(DummyVariableContext ctx, object[] _)
    {
        ctx.Value.Replace("  ", " ");
        return null;
    }

    /// <summary>
    /// Removes trailing period if present.
    /// Key: "removeLastPeriod".
    /// Faithful to PostProcessors.RemoveLastPeriod.
    /// </summary>
    public static string? RemoveLastPeriod(DummyVariableContext ctx, object[] _)
    {
        if (ctx.Value.Length > 0 && ctx.Value[^1] == '.')
        {
            ctx.Value.Remove(ctx.Value.Length - 1, 1);
        }

        return null;
    }

    /// <summary>
    /// Truncates Value at the first comma.
    /// Key: "beforeComma".
    /// Faithful to PostProcessors.BeforeComma.
    /// </summary>
    public static string? BeforeComma(DummyVariableContext ctx, object[] _)
    {
        string text = ctx.Value.ToString();
        int idx = text.IndexOf(',', StringComparison.Ordinal);
        if (idx > 0)
        {
            ctx.Value.Remove(idx, ctx.Value.Length - idx);
        }

        return null;
    }

    // --- Helper methods reproducing TextBuilder extensions ---

    /// <summary>Uppercase the first character of a StringBuilder.</summary>
    private static void InitUpper(StringBuilder sb)
    {
        if (sb.Length > 0 && char.IsLower(sb[0]))
        {
            sb[0] = char.ToUpperInvariant(sb[0]);
        }
    }

    /// <summary>Lowercase the first character of a StringBuilder.</summary>
    private static void InitLowerChar(StringBuilder sb)
    {
        if (sb.Length > 0 && char.IsUpper(sb[0]))
        {
            sb[0] = char.ToLowerInvariant(sb[0]);
        }
    }

    /// <summary>Uppercase the first character of each line.</summary>
    private static void InitUpperEachLine(StringBuilder sb)
    {
        bool lineStart = true;
        for (int i = 0; i < sb.Length; i++)
        {
            if (lineStart && char.IsLetter(sb[i]))
            {
                sb[i] = char.ToUpperInvariant(sb[i]);
                lineStart = false;
            }
            else if (sb[i] == '\n')
            {
                lineStart = true;
            }
        }
    }

    /// <summary>
    /// Registers all language-independent postprocessors into <see cref="DummyVariableReplacers"/>.
    /// Call this during test setup to populate the PostMap.
    /// </summary>
    public static void RegisterAll()
    {
        DummyVariableReplacers.RegisterPost(["capitalize", "initUpper"], new DummyReplacerEntry(Capitalize, []));
        DummyVariableReplacers.RegisterPost("capEachLine", CapEachLine);
        DummyVariableReplacers.RegisterPost("lower", Lower);
        DummyVariableReplacers.RegisterPost("upper", Upper);
        DummyVariableReplacers.RegisterPost("initLower", InitLower);
        DummyVariableReplacers.RegisterPost("strip", Strip);
        DummyVariableReplacers.RegisterPost(["color"], new DummyReplacerEntry(Color, []));
        DummyVariableReplacers.RegisterPost(["color"], new DummyReplacerEntry(Color, [typeof(string)]));
        DummyVariableReplacers.RegisterPost("rules", Rules);
        DummyVariableReplacers.RegisterPost("colorSafe", ColorSafe);
        DummyVariableReplacers.RegisterPost("spaceAfter", SpaceAfter);
        DummyVariableReplacers.RegisterPost("spaceAfterEvenIfHyphen", SpaceAfterEvenIfHyphen);
        DummyVariableReplacers.RegisterPost("colonAfter", ColonAfter);
        DummyVariableReplacers.RegisterPost("spaceBefore", SpaceBefore);
        DummyVariableReplacers.RegisterPost("trim", Trim);
        DummyVariableReplacers.RegisterPost("slim", Slim);
        DummyVariableReplacers.RegisterPost("removeLastPeriod", RemoveLastPeriod);
        DummyVariableReplacers.RegisterPost("beforeComma", BeforeComma);
    }
}

#pragma warning restore CA1308
#pragma warning restore CA1707
