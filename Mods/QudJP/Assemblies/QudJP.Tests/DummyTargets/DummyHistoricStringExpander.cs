#pragma warning disable CA1304
#pragma warning disable CA1311
#pragma warning disable CA1308
#pragma warning disable CA1707
#pragma warning disable CA1812
#pragma warning disable CA5394
#pragma warning disable S2094

using System.Collections;
using System.Globalization;
using System.Text;
using System.Text.RegularExpressions;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of the HistoricStringExpander format-conversion surface from
/// decompiled beta 212.17 <c>HistoryKit/HistoricStringExpander.cs</c> lines 22-35 and 472-587.
/// <para>
/// Game-only types are intentionally substituted for test portability: the original
/// <c>Cysharp.Text.Utf16ValueStringBuilder</c> buffers are reproduced with
/// <see cref="StringBuilder"/>, while preserving replacement order and observable output.
/// </para>
/// <para>
/// Phase 1 is limited to legacy format normalization only. Query execution methods from the
/// game surface are present as stubs for later phases and deliberately throw
/// <see cref="NotImplementedException"/>.
/// </para>
/// </summary>
internal static class DummyHistoricStringExpander
{
    /// <summary>
    /// Decompiled line 22. Converts legacy set-format markers like
    /// <c>&lt;$prof=spice.value&gt;</c> or <c>&lt;$prof=entity.@organizingPrincipleType&gt;</c>.
    /// <para>
    /// The game stores both spice and entity-prefixed set forms in this regex; the compiled
    /// pattern is kept byte-for-byte equivalent to preserve matching behavior.
    /// </para>
    /// </summary>
    private static readonly Regex SetFormatRegex = new("<(?<varname>\\$[^=]+)=(?<type>spice|entity\\.)(?<value>[^>]*)>", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 24. Rewrites legacy <c>&lt;spice.query&gt;</c> angle-bracket queries to
    /// <c>=spice:query=</c>.
    /// </summary>
    private static readonly Regex SpiceRegex = new("<spice\\.(?<query>[^>]*)>", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 26. Rewrites legacy <c>&lt;entity.query&gt;</c> markers to
    /// <c>=spice.entity:query=</c>.
    /// </summary>
    private static readonly Regex EntityRegex = new("<entity\\.(?<query>[^>]*)>", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 28. Rewrites legacy <c>&lt;^.query&gt;</c> markers to <c>=^:query=</c>.
    /// </summary>
    private static readonly Regex CaretRegex = new("<\\^\\.(?<query>[^>]*)>", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 30. Converts chained post-format suffixes like
    /// <c>.pluralize.capitalize=</c> into pipe format.
    /// </summary>
    private static readonly Regex PostRegex = new("(?<posts>(?:\\.(?:pluralize|capitalize|article))+)=", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 32. Matches legacy star variables such as <c>*dish*</c>.
    /// <para>
    /// Emote markup handling is intentionally performed outside the regex, matching the
    /// source-order behavior in lines 521-524.
    /// </para>
    /// </summary>
    private static readonly Regex StarVarsRegex = new("\\*(?<key>[^ \\*]+)\\*", RegexOptions.Compiled);

    /// <summary>
    /// Decompiled line 34. Characters replaced with hyphens during generic star-var conversion.
    /// The misspelling is preserved to stay source-faithful.
    /// </summary>
    private const string IllegalStarVarSigals = ".:|#";

    private static readonly HashSet<string> RenamedEntityKeys = new(StringComparer.Ordinal)
    {
        "elements",
        "organizingPrincipleType",
    };

    /// <summary>
    /// Faithful reproduction of <c>CheckDeprecatedSpiceFormats</c> (decompiled lines 472-535).
    /// <para>
    /// Replacement order is preserved exactly: set-format, spice angle queries, entity angle
    /// queries, caret queries, optional star-var rewriting, post suffix normalization, then the
    /// six fixed token replacements.
    /// </para>
    /// <para>
    /// Test-only substitution: the original game method used
    /// <c>Utf16ValueStringBuilder</c>; this dummy uses <see cref="StringBuilder"/> while
    /// keeping the same emitted strings.
    /// </para>
    /// </summary>
    public static string CheckDeprecatedSpiceFormats(string SpiceString)
    {
        if (SpiceString.IndexOfAny(['*', '<', '@', '$']) == -1)
        {
            return SpiceString;
        }

        StringBuilder sb = new();
        string input = SpiceString;

        input = SetFormatRegex.Replace(input, match =>
        {
            sb.Clear();
            sb.Append("=spice.set");
            if (match.Groups["type"].Value == "entity.")
            {
                sb.Append(".entity");
            }

            sb.Append(':');
            sb.Append(match.Groups["value"].Value);
            sb.Append(':');
            sb.Append(match.Groups["varname"].Value);
            sb.Append('=');
            return sb.ToString();
        });

        input = SpiceRegex.Replace(input, match =>
        {
            sb.Clear();
            sb.Append("=spice:");
            sb.Append(match.Groups["query"].Value);
            sb.Append('=');
            return sb.ToString();
        });

        input = EntityRegex.Replace(input, match =>
        {
            sb.Clear();
            sb.Append("=spice.entity:");
            sb.Append(match.Groups["query"].Value);
            sb.Append('=');
            return sb.ToString();
        });

        input = CaretRegex.Replace(input, match =>
        {
            sb.Clear();
            sb.Append("=^:");
            sb.Append(match.Groups["query"].Value);
            sb.Append('=');
            return sb.ToString();
        });

        if (!input.Contains("{{emote|", StringComparison.Ordinal))
        {
            input = StarVarsRegex.Replace(input, match => ReformatStarVars(match.Value));
        }

        input = PostRegex.Replace(
            input,
            match => string.Join("|", match.Groups["posts"].Value.Split('.')) + "=");

        return input.Replace("@item.a", "=item.a=", StringComparison.Ordinal)
            .Replace("@item.name", "=item.name|strip=", StringComparison.Ordinal)
            .Replace("$focus", "=focus=", StringComparison.Ordinal)
            .Replace("$markovTitle", "=MARKOVTITLE=", StringComparison.Ordinal)
            .Replace("$workshop", "=mapnote.text|initLower|colorSafe=", StringComparison.Ordinal)
            .Replace("$location", "=mapnote.location=", StringComparison.Ordinal);
    }

    /// <summary>
    /// Faithful reproduction of <c>ReformatStarVars</c> (decompiled lines 537-586).
    /// <para>
    /// Special star vars preserve the game's explicit replacements, including substitutions that
    /// target game query types such as <c>creature</c>, <c>adj</c>, <c>itemName</c>,
    /// <c>descriptor</c>, and the cooking spice tree.
    /// </para>
    /// <para>
    /// Generic conversion lowercases the first payload character using the current culture,
    /// matching the source method's <c>ToLower()</c> behavior for observable ASCII inputs.
    /// </para>
    /// </summary>
    public static string ReformatStarVars(string Stars)
    {
        if (Stars.Length < 3)
        {
            return Stars;
        }

        if (Stars[0] != '*' || Stars[^1] != '*')
        {
            return Stars;
        }

        switch (Stars)
        {
            case "*itemType*":
                return "$itemType";
            case "*element*":
                return "$element";
            case "*creatureNamePossessive*":
                return "=creature.a.name's|strip=";
            case "*adj.cap*":
                return "=adj|title=";
            case "*itemName.cap*":
                return "=itemName|title=";
            case "*dish*":
                return "=spice:cooking.recipeNames.categorizedFoods.$dishName.!random=";
            case "*descriptor.possessive*":
                return "=descriptor|'s=";
            default:
            {
                StringBuilder builder = new();
                builder.Append('=');
                for (int i = 1; i < Stars.Length - 1; i++)
                {
                    if (IllegalStarVarSigals.Contains(Stars[i], StringComparison.Ordinal))
                    {
                        builder.Append('-');
                    }
                    else if (i != 1)
                    {
                        builder.Append(Stars[i]);
                    }
                    else
                    {
                        builder.Append(Stars[i].ToString().ToLower());
                    }
                }

                builder.Append('=');
                return builder.ToString();
            }
        }
    }

    /// <summary>
    /// Faithful reproduction of the game's parameterless query surface
    /// (decompiled lines 97-100).
    /// </summary>
    public static string Expand(params string[] Strings)
    {
        return Expand((DummyHistory?)null, null, Strings);
    }

    /// <summary>
    /// Faithful reproduction of the history-based expand overload
    /// (decompiled lines 102-105).
    /// </summary>
    public static string Expand(DummyHistory? History = null, params string[] Strings)
    {
        return Expand(History, null, Strings);
    }

    /// <summary>
    /// Faithful reproduction of the entity-based expand overload
    /// (decompiled lines 107-110).
    /// </summary>
    public static string Expand(DummyHistoricEntitySnapshot? Entity = null, params string[] Strings)
    {
        return Expand(Entity?.entity._history, Entity, Strings);
    }

    /// <summary>
    /// Faithful reproduction of the full expand overload
    /// (decompiled lines 112-121).
    /// </summary>
    public static string Expand(DummyHistory? History = null, DummyHistoricEntitySnapshot? Entity = null, params string[] Strings)
    {
        DummySpiceContext argument = new()
        {
            Entity = Entity,
            History = History ?? Entity?.entity._history ?? new DummyHistory(0L, new Random(0)),
        };

        string[] parts = SplitParts(Strings);
        string rest = parts.Length > 1 ? string.Join('.', parts[1..]) : string.Empty;
        return DummyReplaceBuilder.Create()
            .Start("=" + parts[0] + ":" + rest + "=")
            .AddArgument(argument, "spice", silent: true)
            .ToString();
    }

    /// <summary>
    /// Faithful reproduction of the game delegated query expansion surface
    /// (decompiled lines 123-126).
    /// </summary>
    public static string DelegateExpandQuery(DummySpiceContext Context, params string[] Strings)
    {
        Context.Variables ??= new Dictionary<string, string>(StringComparer.Ordinal);
        Context.NodeVariables ??= new Dictionary<string, object>(StringComparer.Ordinal);
        return InternalExpandQuery(
            SplitParts(Strings),
            Context.Entity,
            Context.History,
            Context.Variables,
            Context.NodeVariables,
            Context.Random);
    }

    /// <summary>
    /// Phase 2/3 placeholder for the one-argument string expansion surface
    /// (decompiled lines 437-440).
    /// </summary>
    public static string ExpandString(string input, Random? Random = null) => throw new NotImplementedException();

    /// <summary>
    /// Phase 2/3 placeholder for the full string expansion overload
    /// (decompiled lines 443-470).
    /// </summary>
    public static string ExpandString(string input, DummyHistoricEntitySnapshot? entity, DummyHistory? history, Dictionary<string, string>? vars = null, Random? Random = null) => throw new NotImplementedException();

    /// <summary>
    /// Phase 2/3 placeholder for the game's internal query expansion surface
    /// (decompiled lines 128-435).
    /// </summary>
    internal static string InternalExpandQuery(string[] parts, DummyHistoricEntitySnapshot? entity, DummyHistory? history, IDictionary<string, string> vars, IDictionary<string, object> nodeVars, Random? R = null)
    {
        history ??= new DummyHistory(0L, new Random(0));
        R ??= history.r;

        if (parts.Length == 0)
        {
            return string.Empty;
        }

        string text = string.Join(".", parts);
        string? inlineAssignmentTarget = null;
        string? directLookupKey = parts.Length == 2 ? parts[1] : null;

        for (int i = 0; i < parts.Length; i++)
        {
            if (vars.TryGetValue(parts[i], out string? partValue))
            {
                parts[i] = partValue;
            }
        }

        if (parts.Length < 2)
        {
            string key = parts[0];
            int assignmentSeparator = key.IndexOf('=', StringComparison.Ordinal);
            if (assignmentSeparator > 0 && assignmentSeparator < key.Length - 1)
            {
                inlineAssignmentTarget = key[..assignmentSeparator];
                key = key[(assignmentSeparator + 1)..];
            }

            string result;
            if (nodeVars.TryGetValue(key, out object? nodeValue))
            {
                result = ApplyFinalVariableReplacement(nodeValue?.ToString() ?? string.Empty, vars);
            }
            else if (vars.TryGetValue(key, out string? variableValue))
            {
                result = ApplyFinalVariableReplacement(variableValue, vars);
            }
            else
            {
                result = ApplyFinalVariableReplacement(key, vars);
            }

            if (inlineAssignmentTarget != null)
            {
                vars[inlineAssignmentTarget] = result;
                return string.Empty;
            }

            return result;
        }

        string root = parts[0];
        string keyPath = string.Join(".", parts[1..]);

        if (root == "spice" || (root.Length > 0 && root[0] == '$'))
        {
            if (root == "spice" && nodeVars.TryGetValue(keyPath, out object? nodeValue))
            {
                return ApplyFinalVariableReplacement(nodeValue?.ToString() ?? string.Empty, vars);
            }

            if (root == "spice" && directLookupKey != null && vars.TryGetValue(directLookupKey, out string? variableValue))
            {
                return ApplyFinalVariableReplacement(variableValue, vars);
            }

            if (root == "spice" && parts.Length == 2 && parts[1] == "currentYear")
            {
                return history.currentYear.ToString(CultureInfo.InvariantCulture);
            }

            if (root == "spice" && parts.Length == 2 && parts[1] == "startingYear")
            {
                return history.startingYear.ToString(CultureInfo.InvariantCulture);
            }

            object? rootNode;
            if (root == "spice")
            {
                rootNode = DummyHistoricSpice.GetRoot();
            }
            else
            {
                nodeVars.TryGetValue(root, out rootNode);
            }

            return ApplyFinalVariableReplacement(
                ExpandStructuredPath(rootNode, parts[1..], entity, history, vars, nodeVars, R),
                vars);
        }

        if (root == "entity" || root.StartsWith("entity[", StringComparison.Ordinal))
        {
            return ApplyFinalVariableReplacement(
                ExpandDirectEntityPath(root, parts[1..], entity, history, R, text),
                vars);
        }

        if (nodeVars.TryGetValue(keyPath, out object? value))
        {
            return ApplyFinalVariableReplacement(value?.ToString() ?? string.Empty, vars);
        }

        return vars.TryGetValue(keyPath, out string? fallback)
            ? ApplyFinalVariableReplacement(fallback, vars)
            : string.Empty;
    }

    private static string[] SplitParts(string[] strings)
    {
        return strings.SelectMany(static s => s?.Split('.') ?? []).ToArray();
    }

    private static string ExpandStructuredPath(
        object? rootNode,
        string[] path,
        DummyHistoricEntitySnapshot? entity,
        DummyHistory history,
        IDictionary<string, string> vars,
        IDictionary<string, object> nodeVars,
        Random random)
    {
        object? current = rootNode;
        for (int i = 0; i < path.Length; i++)
        {
            string segment = path[i];
            bool isFinal = i == path.Length - 1;

            if (segment == "!random" || segment == "@random")
            {
                if (!TryResolveRandomSegment(current, random, isFinal, out object? nextNode, out string? randomValue))
                {
                    return string.Empty;
                }

                if (isFinal)
                {
                    return randomValue ?? string.Empty;
                }

                current = nextNode;
                continue;
            }

            if (segment.StartsWith('$'))
            {
                if (vars.TryGetValue(segment, out string? variableValue))
                {
                    segment = variableValue;
                }
                else if (nodeVars.TryGetValue(segment, out object? nodeValue))
                {
                    segment = nodeValue?.ToString() ?? string.Empty;
                }
            }
            else if (TryResolveEntitySegment(segment, entity, history, random, out string resolvedEntitySegment, out bool terminal))
            {
                if (terminal)
                {
                    return resolvedEntitySegment;
                }

                segment = resolvedEntitySegment;
            }

            if (!TryGetChildNode(current, segment, out current))
            {
                return string.Empty;
            }
        }

        return ConvertNodeToString(current);
    }

    private static string ExpandDirectEntityPath(
        string root,
        string[] path,
        DummyHistoricEntitySnapshot? entity,
        DummyHistory history,
        Random random,
        string text)
    {
        DummyHistoricEntitySnapshot? source = entity;
        if (root.StartsWith("entity[", StringComparison.Ordinal))
        {
            source = history.GetEntity(root.Split('[')[1].Replace("]", "", StringComparison.Ordinal))?.GetSnapshotAtYear(history.currentYear);
        }

        if (source == null)
        {
            return "<unknown entity>";
        }

        if (path.Length != 1)
        {
            return "<unknown format " + text + ">";
        }

        if (TryParseIndexedMember(path[0], out string key, out string indexToken))
        {
            if (!TryGetEntityList(source, key, out List<string>? list))
            {
                return "<undefined entity list " + key + ">";
            }

            if (list is { Count: 0 })
            {
                return "<empty entity list " + key + ">";
            }

            return TryGetEntityIndexedValue(source, key, indexToken, random, out string indexedValue)
                ? indexedValue
                : "<empty entity list " + key + ">";
        }

        return TryGetEntityPropertyValue(source, path[0], out string propertyValue)
            ? propertyValue
            : "unknown";
    }

    private static bool TryResolveRandomSegment(object? current, Random random, bool isFinal, out object? nextNode, out string? value)
    {
        nextNode = null;
        value = null;

        if (current is IDictionary<string, object> dictionary && dictionary.Count > 0)
        {
            KeyValuePair<string, object> selection = dictionary.ElementAt(random.Next(dictionary.Count));
            nextNode = selection.Value;
            value = selection.Key;
            return true;
        }

        if (current is IList list && list.Count > 0)
        {
            object? selection = list[random.Next(list.Count)];
            nextNode = selection;
            value = ConvertNodeToString(selection);
            return true;
        }

        if (isFinal && current != null)
        {
            nextNode = current;
            value = ConvertNodeToString(current);
            return true;
        }

        return false;
    }

    private static bool TryResolveEntitySegment(
        string segment,
        DummyHistoricEntitySnapshot? entity,
        DummyHistory history,
        Random random,
        out string value,
        out bool terminal)
    {
        value = string.Empty;
        terminal = false;

        int propertySeparator = segment.IndexOfAny(['@', '$']);
        if (propertySeparator <= 0)
        {
            return false;
        }

        string entitySelector = segment[..propertySeparator];
        if (entitySelector != "entity" && !entitySelector.StartsWith("entity[", StringComparison.Ordinal))
        {
            return false;
        }

        DummyHistoricEntitySnapshot? source = entity;
        if (entitySelector.StartsWith("entity[", StringComparison.Ordinal) &&
            !TryResolveNamedEntity(entitySelector, history, out source))
        {
            return false;
        }

        if (source == null)
        {
            return false;
        }

        string member = segment[(propertySeparator + 1)..];
        if (member.Length == 0)
        {
            return false;
        }

        bool atMember = segment[propertySeparator] == '@';
        if (TryParseIndexedMember(member, out string memberName, out string indexToken))
        {
            string lookupKey = atMember ? "@" + memberName : memberName;
            if (TryGetEntityList(source, lookupKey, out List<string>? list) && list != null)
            {
                if (list.Count == 0)
                {
                    value = "<empty entity list " + memberName + ">";
                    terminal = true;
                    return true;
                }

                return TryGetEntityIndexedValue(source, lookupKey, indexToken, random, out value);
            }

            if (TryGetEntityPropertyValue(source, lookupKey, out value))
            {
                return true;
            }

            value = "<undefined entity property " + lookupKey + ">";
            terminal = true;
            return true;
        }

        string propertyKey = atMember ? "@" + member : member;
        if (TryGetEntityPropertyValue(source, propertyKey, out value))
        {
            return true;
        }

        value = "<undefined entity property " + propertyKey + ">";
        terminal = true;
        return true;
    }

    private static bool TryResolveNamedEntity(string selector, DummyHistory history, out DummyHistoricEntitySnapshot? entity)
    {
        entity = null;
        int start = selector.IndexOf('[', StringComparison.Ordinal);
        int end = selector.IndexOf(']', StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            return false;
        }

        string name = selector[(start + 1)..end];
        entity = history.GetEntity(name)?.GetSnapshotAtYear(history.currentYear);
        return entity != null;
    }

    private static bool TryGetChildNode(object? current, string key, out object? next)
    {
        next = null;
        if (string.IsNullOrEmpty(key))
        {
            return false;
        }

        if (current is IDictionary<string, object> dictionary)
        {
            return dictionary.TryGetValue(key, out next);
        }

        if (current is IList list && int.TryParse(key, out int index) && index >= 0 && index < list.Count)
        {
            next = list[index];
            return true;
        }

        return false;
    }

    private static string ConvertNodeToString(object? node)
    {
        return node switch
        {
            null => string.Empty,
            string text => text,
            _ => node.ToString() ?? string.Empty,
        };
    }

    private static bool TryGetEntityIndexedValue(DummyHistoricEntitySnapshot entity, string key, string indexToken, Random random, out string value)
    {
        value = string.Empty;
        if (!TryGetEntityList(entity, key, out List<string>? list) || list is not { Count: > 0 })
        {
            return false;
        }

        int index;
        if (indexToken == "random")
        {
            index = random.Next(list.Count);
        }
        else if (!int.TryParse(indexToken, NumberStyles.Integer, CultureInfo.InvariantCulture, out index) ||
                 index < 0 ||
                 index >= list.Count)
        {
            return false;
        }

        value = list[index];
        return true;
    }

    private static bool TryGetEntityPropertyValue(DummyHistoricEntitySnapshot entity, string key, out string value)
    {
        if (TryResolveEntityKey(entity.properties, key, out string? actualKey) &&
            actualKey != null &&
            entity.properties.TryGetValue(actualKey, out string? propertyValue))
        {
            value = propertyValue;
            return true;
        }

        value = string.Empty;
        return false;
    }

    private static bool TryGetEntityList(DummyHistoricEntitySnapshot entity, string key, out List<string>? value)
    {
        if (TryResolveEntityKey(entity.listProperties, key, out string? actualKey) &&
            actualKey != null &&
            entity.listProperties.TryGetValue(actualKey, out List<string>? list))
        {
            value = list;
            return true;
        }

        value = null;
        return false;
    }

    private static bool TryResolveEntityKey<T>(Dictionary<string, T> map, string key, out string? actualKey)
    {
        if (map.ContainsKey(key))
        {
            actualKey = key;
            return true;
        }

        if (key.Length > 0 &&
            key[0] == '@' &&
            RenamedEntityKeys.Contains(key[1..]) &&
            map.ContainsKey(key[1..]))
        {
            actualKey = key[1..];
            return true;
        }

        string atKey = key.Length > 0 && key[0] == '@' ? key : "@" + key;
        if (RenamedEntityKeys.Contains(key) && map.ContainsKey(atKey))
        {
            actualKey = atKey;
            return true;
        }

        actualKey = null;
        return false;
    }

    private static bool TryParseIndexedMember(string member, out string key, out string indexToken)
    {
        key = string.Empty;
        indexToken = string.Empty;

        int start = member.IndexOf('[', StringComparison.Ordinal);
        int end = member.IndexOf(']', StringComparison.Ordinal);
        if (start < 0 || end <= start)
        {
            return false;
        }

        key = member[..start];
        indexToken = member[(start + 1)..end];
        return key.Length > 0 && indexToken.Length > 0;
    }

    private static string ApplyFinalVariableReplacement(string value, IDictionary<string, string> vars)
    {
        if (vars.Count == 0)
        {
            return value;
        }

        foreach ((string key, string replacement) in vars)
        {
            int attempts = 0;
            while (value.Contains(key, StringComparison.Ordinal) && ++attempts < 5)
            {
                value = value.Replace(key, replacement, StringComparison.Ordinal);
            }
        }

        return value;
    }
}

#pragma warning restore S2094
#pragma warning restore CA5394
#pragma warning restore CA1812
#pragma warning restore CA1707
#pragma warning restore CA1308
#pragma warning restore CA1311
#pragma warning restore CA1304
