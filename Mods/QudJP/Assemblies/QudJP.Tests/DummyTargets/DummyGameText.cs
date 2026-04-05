#pragma warning disable CA1707
#pragma warning disable CA1034 // Nested types should not be visible

using System.Text;

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Faithful reproduction of XRL.GameText from decompiled beta (212.17).
/// Char-by-char state machine parser for <c>=key|postprocessor=</c> variable expansion.
/// <para>
/// Replaces <c>TextBuilder</c> with <see cref="StringBuilder"/>,
/// <c>Rack&lt;T&gt;</c> with <see cref="List{T}"/>,
/// <c>StringMap&lt;T&gt;</c> with <see cref="Dictionary{TKey, TValue}"/>.
/// Game-dependent singletons (The.Player, The.Game, SpiceContext, etc.) are replaced
/// with injectable argument factories via <see cref="TargetResolver"/>.
/// </para>
/// </summary>
internal static class DummyGameText
{
    // Section type constants (faithful to GameText.cs line 233-239)
    private const int SECTION_KEY = 0;
    private const int SECTION_POST_KEY = 1;
    private const int SECTION_PARAMETER = 2;
    private const int SECTION_ARGUMENT = 3;
    private const int SECTION_KEY_ARGUMENT = 4;

    // Target sentinel constants (faithful to GameText.cs line 241-259)
    internal const int TARGET_UNINITIALIZED = -1;
    internal const int TARGET_SUBJECT = -2;
    internal const int TARGET_OBJECT = -3;
    internal const int TARGET_PLAYER = -4;
    internal const int TARGET_SPICE = -5;
    internal const int TARGET_GAME = -6;
    internal const int TARGET_ACTIVE_ZONE = -7;
    internal const int TARGET_NOW = -8;
    internal const int TARGET_NONE = int.MinValue;

    /// <summary>
    /// Default target name → sentinel value map.
    /// Faithful to GameText.DefaultTargets (line 262-273).
    /// </summary>
    public static readonly Dictionary<string, int> DefaultTargets = new()
    {
        ["player"] = TARGET_PLAYER,
        ["spice"] = TARGET_SPICE,
        ["game"] = TARGET_GAME,
        ["activezone"] = TARGET_ACTIVE_ZONE,
        ["subject"] = TARGET_SUBJECT,
        ["pronouns"] = TARGET_SUBJECT,
        ["objpronouns"] = TARGET_OBJECT,
        ["object"] = TARGET_OBJECT,
        ["now"] = TARGET_NOW,
    };

    /// <summary>
    /// Delegate for resolving special target sentinels (player, game, spice, etc.)
    /// to concrete objects. Replaces game singletons (The.Player, The.Game, etc.)
    /// with injectable test values.
    /// </summary>
    /// <param name="sentinel">The sentinel value (TARGET_PLAYER, TARGET_GAME, etc.)</param>
    /// <param name="aliases">Current alias map, may be mutated for spice/now targets.</param>
    /// <param name="arguments">Current arguments list, may be mutated for spice/now targets.</param>
    /// <param name="argument">Resolved object to use.</param>
    /// <returns>True if the sentinel was resolved.</returns>
    internal delegate bool TargetResolver(
        int sentinel,
        Dictionary<string, int> aliases,
        List<object> arguments,
        out object? argument);

    /// <summary>
    /// Default target resolver that handles subject/object index mapping
    /// but rejects game-specific singletons (player, spice, game, etc.).
    /// Tests should provide their own resolver for singletons.
    /// </summary>
    public static readonly TargetResolver DefaultResolver = (int sentinel, Dictionary<string, int> aliases, List<object> arguments, out object? argument) =>
    {
        argument = null;
        // Game-specific singletons: not available in test context
        return false;
    };

    /// <summary>
    /// Private nested class reproducing GameText.VariableRoutine (line 93-146).
    /// Holds per-routine state during parsing of a single <c>=...=</c> expression.
    /// </summary>
    private sealed class VariableRoutine
    {
        public DummyEntryRack? Target;
        public List<object> Arguments = [];
        public List<string> Parameters = [];
        public DummyTargetType Type;

        public bool TryFindEntry(out DummyReplacerEntry entry)
        {
            if (Target == null)
            {
                entry = null!;
                return false;
            }

            return Target.TryFind(Arguments, out entry);
        }

        public DummyReplacerEntry GetEntry()
        {
            if (TryFindEntry(out var entry))
            {
                return entry;
            }

            // Build descriptive error message (faithful to decompiled line 110-122)
            StringBuilder sb = new();
            sb.Append("Variable replacer =");
            if (Arguments.Count > 0)
            {
                sb.Append(Arguments[0]?.GetType().Name ?? "null").Append('.');
            }

            sb.Append(Target?.Key ?? "[NO KEY]");
            for (int i = 1; i < Arguments.Count; i++)
            {
                sb.Append('#').Append(Arguments[i]?.GetType().Name ?? "null");
            }

            sb.Append("= not found.");
            throw new KeyNotFoundException(sb.ToString());
        }

        public void Clear()
        {
            Target = null;
            Arguments.Clear();
            Parameters.Clear();
            Type = DummyTargetType.None;
        }
    }

    /// <summary>
    /// Private nested class reproducing GameText.ProcessContext (line 20-91).
    /// Holds all per-call mutable state. Not pooled in test (no concurrency concern).
    /// </summary>
    private sealed class ProcessContext
    {
        public int BufferCapacity = 64;
        public char[] Buffer = new char[64];
        public DummyVariableContext VariableContext = new();
        public DummyGameTextInvocation Invocation;
        public List<VariableRoutine> Routines = [new VariableRoutine()];
        public int CurrentRoutineIndex;

        public VariableRoutine CurrentRoutine => Routines[CurrentRoutineIndex];

        public ProcessContext()
        {
            Invocation = VariableContext.Invocation;
        }

        public void PushRoutine()
        {
            int next = CurrentRoutineIndex + 1;
            if (next >= Routines.Count)
            {
                Routines.Add(new VariableRoutine());
            }

            CurrentRoutineIndex = next;
        }

        public void ClearRoutines()
        {
            for (int i = CurrentRoutineIndex; i >= 0; i--)
            {
                Routines[i].Clear();
            }

            CurrentRoutineIndex = 0;
        }
    }

    /// <summary>
    /// Main variable expansion processor. Faithful to GameText.Process (line 462-620).
    /// Parses <c>=key|postprocessor=</c> syntax in <paramref name="message"/> and replaces
    /// variable expressions with their expanded values.
    /// </summary>
    /// <param name="message">The message to process. Modified in-place.</param>
    /// <param name="arguments">Ordered arguments (index 0 = subject, 1 = object, etc.).</param>
    /// <param name="aliases">Named aliases mapping to argument indices.</param>
    /// <param name="stripColors">If true, strip color markup after processing.</param>
    /// <param name="resolver">
    /// Optional target resolver for game-specific singletons.
    /// If null, uses <see cref="DefaultResolver"/>.
    /// </param>
    public static void Process(
        StringBuilder message,
        List<object>? arguments = null,
        Dictionary<string, int>? aliases = null,
        bool stripColors = false,
        TargetResolver? resolver = null)
    {
        resolver ??= DefaultResolver;

        ProcessContext ctx = new();
        char[] buffer = ctx.Buffer;
        int bufferCapacity = ctx.BufferCapacity;
        List<VariableRoutine> routines = ctx.Routines;
        DummyVariableContext variableContext = ctx.VariableContext;
        StringBuilder value = variableContext.Value;
        DummyGameTextInvocation invocation = variableContext.Invocation;
        invocation.Message = message;
        invocation.Aliases = aliases;
        invocation.Arguments = arguments;

        // State machine variables (faithful to line 479-486)
        bool dotSeen = false;   // 'flag' in decompiled
        int bufferPos = 0;      // 'num2' — offset into buffer
        int varStart = -1;      // 'num3' — index in message where '=' was seen
        int sectionStart = -1;  // 'num4' — start offset of current section in buffer
        int sectionType = -1;   // 'num5' — current section type constant

        int i = 0;
        int length = message.Length;

        while (i < length)
        {
            char c = message[i];

            if (varStart == -1)
            {
                // Outside a variable expression
                if (c == '=')
                {
                    varStart = i;
                    sectionType = SECTION_KEY;
                    dotSeen = false;
                    sectionStart = 0;
                    ctx.ClearRoutines();
                }

                i++;
                continue;
            }

            // Inside a variable expression
            bufferPos = i - varStart - 1;

            // Grow buffer if needed (faithful to line 502-508)
            if (bufferPos >= bufferCapacity)
            {
                char[] newBuffer = new char[bufferCapacity * 2];
                Array.Copy(buffer, 0, newBuffer, 0, bufferCapacity);
                buffer = newBuffer;
                ctx.Buffer = buffer;
                bufferCapacity *= 2;
                ctx.BufferCapacity = bufferCapacity;
            }

            buffer[bufferPos] = c;

            switch (c)
            {
                case ' ':
                case '\n':
                    // Abort variable if in KEY or POST_KEY section (line 512-518)
                    if (sectionType is SECTION_KEY or SECTION_POST_KEY)
                    {
                        varStart = -1;
                    }

                    break;

                case ':':
                    // Parameter section (line 519-523)
                    ProcessSection(ctx, buffer, sectionStart, bufferPos - sectionStart, sectionType, resolver);
                    sectionType = SECTION_PARAMETER;
                    sectionStart = bufferPos + 1;
                    break;

                case '#':
                    // Argument section (line 524-528)
                    ProcessSection(ctx, buffer, sectionStart, bufferPos - sectionStart, sectionType, resolver);
                    sectionType = SECTION_ARGUMENT;
                    sectionStart = bufferPos + 1;
                    break;

                case '|':
                    // Pipe: push new routine for postprocessor (line 529-535)
                    ProcessSection(ctx, buffer, sectionStart, bufferPos - sectionStart, sectionType, resolver);
                    ctx.PushRoutine();
                    dotSeen = false;
                    sectionType = SECTION_POST_KEY;
                    sectionStart = bufferPos + 1;
                    break;

                case '.':
                    // Dot: attempt to set subject type via KEY_ARGUMENT (line 537-544)
                    if (!dotSeen && sectionType is SECTION_KEY or SECTION_POST_KEY)
                    {
                        dotSeen = true;
                        if (ProcessSection(ctx, buffer, sectionStart, bufferPos - sectionStart, SECTION_KEY_ARGUMENT, resolver))
                        {
                            sectionStart = bufferPos + 1;
                        }
                    }

                    break;

                case '=':
                    // Closing '=' — execute the variable (line 547-608)
                    if (bufferPos == 0)
                    {
                        // Empty variable "==" → abort
                        varStart = -1;
                        break;
                    }

                    ProcessSection(ctx, buffer, sectionStart, bufferPos - sectionStart, sectionType, resolver);
                    value.Clear();
                    variableContext.ClearState();

                    string? result = null;
                    try
                    {
                        int routineIdx = 0;
                        int lastRoutineIdx = ctx.CurrentRoutineIndex;

                        while (true)
                        {
                            VariableRoutine routine = routines[routineIdx];
                            DummyReplacerEntry entry = routine.GetEntry();
                            List<object> routineArgs = routine.Arguments;

                            // Set up context from entry (line 568-571)
                            variableContext.Default = entry.Default;
                            variableContext.Capitalize = entry.Capitalize;
                            variableContext.Flags = entry.Flags;
                            variableContext.Parameters = routine.Parameters;
                            variableContext.Type = routine.Type;

                            // Append static extra args from entry (line 573-575)
                            if (entry.Arguments.Length != 0)
                            {
                                routineArgs.AddRange(entry.Arguments);
                            }

                            // Call the delegate (line 576)
                            string? delegateResult = entry.Delegate(variableContext, [.. routineArgs]);

                            if (routineIdx == lastRoutineIdx)
                            {
                                result = delegateResult ?? value.ToString();
                                break;
                            }

                            // Chain: intermediate results go into Value (line 583-585)
                            if (delegateResult != null)
                            {
                                value.Clear();
                                value.Append(delegateResult);
                            }

                            routineIdx++;
                        }

                        // Splice result into message (line 589-595)
                        if (result != null)
                        {
                            int removeLength = i - varStart + 1;
                            message.Remove(varStart, removeLength);
                            message.Insert(varStart, result);
                            i = varStart - 1 + result.Length;
                            length = message.Length;
                        }
                    }
#pragma warning disable CA1031 // Faithful to beta: catch all exceptions during variable processing
                    catch (Exception)
                    {
                        // Beta logs and continues (line 598-605); tests silently skip
                    }
#pragma warning restore CA1031

                    varStart = -1;
                    break;
            }

            i++;
        }

        if (stripColors)
        {
            // In tests, delegate to DummyMarkup.Strip if available, or no-op
            StripColors(message);
        }
    }

    /// <summary>
    /// Process a string template with variable expansion.
    /// Returns the processed string.
    /// </summary>
    public static string Process(
        string message,
        List<object>? arguments = null,
        Dictionary<string, int>? aliases = null,
        bool stripColors = false,
        TargetResolver? resolver = null)
    {
        StringBuilder sb = new(message);
        Process(sb, arguments, aliases, stripColors, resolver);
        return sb.ToString();
    }

    /// <summary>
    /// Routes a parsed section token to the appropriate handler.
    /// Faithful to GameText.ProcessSection (line 622-692).
    /// </summary>
    private static bool ProcessSection(
        ProcessContext context,
        char[] buffer,
        int start,
        int sectionLength,
        int type,
        TargetResolver resolver)
    {
        VariableRoutine currentRoutine = context.CurrentRoutine;

        switch (type)
        {
            case SECTION_KEY: // 0
            {
                // If no arguments yet, try to parse as a known argument first
                if (currentRoutine.Arguments.Count == 0 &&
                    TryParseArgument(buffer, start, sectionLength, context.Invocation, resolver, out var arg, out var targetType))
                {
                    currentRoutine.Target = DummyVariableReplacers.DefaultRack;
                    currentRoutine.Arguments.Add(arg!);
                    currentRoutine.Type = targetType;
                    return true;
                }

                // Otherwise look up as a named replacer key
                string key = new(buffer, start, sectionLength);
                if (DummyVariableReplacers.Map.TryGetValue(key, out var rack))
                {
                    currentRoutine.Target = rack;
                    return true;
                }

                return false;
            }

            case SECTION_POST_KEY: // 1
            {
                if (currentRoutine.Arguments.Count == 0 &&
                    TryParseArgument(buffer, start, sectionLength, context.Invocation, resolver, out var arg, out var targetType))
                {
                    currentRoutine.Target = DummyVariableReplacers.DefaultRack;
                    currentRoutine.Arguments.Add(arg!);
                    currentRoutine.Type = targetType;
                    return true;
                }

                string key = new(buffer, start, sectionLength);
                if (DummyVariableReplacers.PostMap.TryGetValue(key, out var rack))
                {
                    currentRoutine.Target = rack;
                    return true;
                }

                return false;
            }

            case SECTION_PARAMETER: // 2
                currentRoutine.Parameters.Add(new string(buffer, start, sectionLength));
                return true;

            case SECTION_ARGUMENT: // 3
            case SECTION_KEY_ARGUMENT: // 4
            {
                if (TryParseArgument(buffer, start, sectionLength, context.Invocation, resolver, out var arg, out var targetType))
                {
                    currentRoutine.Arguments.Add(arg!);
                    if (type == SECTION_KEY_ARGUMENT)
                    {
                        currentRoutine.Type = targetType;
                    }

                    return true;
                }

                return false;
            }
        }

        return false;
    }

    /// <summary>
    /// Resolves a token to a concrete argument object.
    /// Faithful to GameText.TryParseArgument (line 694-780).
    /// Special targets (player/spice/game/etc.) are resolved via the injected <paramref name="resolver"/>.
    /// </summary>
    private static bool TryParseArgument(
        char[] buffer,
        int start,
        int sectionLength,
        DummyGameTextInvocation invocation,
        TargetResolver resolver,
        out object? argument,
        out DummyTargetType type)
    {
        argument = null;
        type = DummyTargetType.None;

        Dictionary<string, int>? aliases = invocation.Aliases;
        int targetIndex = -1;
        int end = start + sectionLength - 1;

        // Check for bracket notation: key[index] (line 701-713)
        if (buffer[end] == ']')
        {
            int bracketStart = Array.LastIndexOf(buffer, '[', end - 2);
            if (bracketStart != -1)
            {
                string keyPart = new(buffer, start, bracketStart - start);
                string indexPart = new(buffer, bracketStart + 1, end - bracketStart - 1);
                if (!DefaultTargets.ContainsKey(keyPart) || !int.TryParse(indexPart, out int bracketIndex))
                {
                    return false;
                }

                targetIndex = bracketIndex;
            }
        }
        else
        {
            // Standard lookup (line 716-723)
            string token = new(buffer, start, sectionLength);
            if ((aliases == null || !aliases.TryGetValue(token, out int aliasValue)) &&
                !DefaultTargets.TryGetValue(token, out aliasValue))
            {
                return false;
            }

            targetIndex = aliasValue;
        }

        // Map subject/object sentinels to indices (line 724-734)
        switch (targetIndex)
        {
            case TARGET_SUBJECT:
                type = DummyTargetType.Subject;
                targetIndex = 0;
                break;
            case TARGET_OBJECT:
                type = DummyTargetType.Object;
                targetIndex = 1;
                break;
        }

        List<object>? arguments = invocation.Arguments;

        // Handle special sentinel targets via resolver (line 736-759)
        if (targetIndex == TARGET_PLAYER)
        {
            type = DummyTargetType.Player;
            return resolver(TARGET_PLAYER, aliases!, arguments!, out argument);
        }

        if (targetIndex == TARGET_SPICE)
        {
            return resolver(TARGET_SPICE, aliases!, arguments!, out argument);
        }

        if (targetIndex == TARGET_GAME)
        {
            return resolver(TARGET_GAME, aliases!, arguments!, out argument);
        }

        if (targetIndex == TARGET_ACTIVE_ZONE)
        {
            return resolver(TARGET_ACTIVE_ZONE, aliases!, arguments!, out argument);
        }

        if (targetIndex == TARGET_NOW)
        {
            return resolver(TARGET_NOW, aliases!, arguments!, out argument);
        }

        // Negative indices that weren't handled above are invalid (line 762-763)
        if (targetIndex < 0)
        {
            return false;
        }

        // Resolve from arguments list (line 766-778)
        if (arguments == null || targetIndex >= arguments.Count)
        {
            return false;
        }

        argument = arguments[targetIndex];
        if (argument is DummyArgumentGenerator generator)
        {
            argument = generator();
            arguments[targetIndex] = argument;
        }

        return true;
    }

    /// <summary>
    /// Strip color markup from a StringBuilder. Delegates to <see cref="DummyMarkup.Strip"/>
    /// if available; otherwise performs a basic strip of <c>&amp;X</c>, <c>^x</c>, and <c>{{...|}}</c> patterns.
    /// </summary>
    private static void StripColors(StringBuilder sb)
    {
        // Delegate to DummyMarkup/DummyColorUtility for full fidelity.
        // For Phase 10a, use a simplified inline strip that handles the most common patterns.
        string text = sb.ToString();
        string stripped = DummyColorUtility.StripFormatting(text);
        sb.Clear();
        sb.Append(stripped);
    }

}

#pragma warning restore CA1034
#pragma warning restore CA1707
