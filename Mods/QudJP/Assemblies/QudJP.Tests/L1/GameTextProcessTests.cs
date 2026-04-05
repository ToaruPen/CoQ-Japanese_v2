#pragma warning disable CA1515
#pragma warning disable CA1707

using System.Text;
using QudJP.Tests.DummyTargets;

namespace QudJP.Tests.L1;

/// <summary>
/// L1 tests for DummyGameText.Process() — faithful reproduction of the
/// XRL.GameText char-by-char state machine parser (212.17).
/// Tests cover: basic key replacement, pipe chaining, parameters, arguments,
/// dot notation, subject/object resolution, abort conditions, and edge cases.
/// </summary>
[Category("L1")]
[TestFixture]
public sealed class GameTextProcessTests
{
    /// <summary>
    /// Registers a minimal set of test replacers and postprocessors
    /// to exercise the parser without requiring the full game registry.
    /// </summary>
    [SetUp]
    public void SetUp()
    {
        DummyVariableReplacers.Reset();

        // "toString" — DefaultRack: returns argument.ToString()
        DummyVariableReplacers.Register(
            ["toString"],
            new DummyReplacerEntry(
                (ctx, args) => args.Length > 0 ? args[0]?.ToString() : null,
                [typeof(object)]));

        // "greeting" — simple keyless replacer, returns "hello"
        DummyVariableReplacers.Register("greeting", (_, _) => "hello");

        // "name" — returns Default if no subject, else subject.ToString()
        DummyVariableReplacers.Register(
            ["name"],
            new DummyReplacerEntry(
                (ctx, _) => ctx.Default ?? "thing",
                [],
                @default: "thing"));

        // "echo" — returns first parameter verbatim
        DummyVariableReplacers.Register("echo",
            (ctx, _) => ctx.Parameters.Count > 0 ? ctx.Parameters[0] : "");

        // "concat" — joins all parameters with "+"
        DummyVariableReplacers.Register("concat",
            (ctx, _) => string.Join("+", ctx.Parameters));

        DummyVariableReplacers.RegisterPost("upper", UpperPost);
        DummyVariableReplacers.RegisterPost("addBang", AddBangPost);

        DummyVariableReplacers.FinalizeInit();
    }

    private static string UpperPost(DummyVariableContext ctx, object[] _)
    {
        string text = ctx.Value.ToString().ToUpperInvariant();
        ctx.Value.Clear();
        ctx.Value.Append(text);
        return null!;
    }

    private static string AddBangPost(DummyVariableContext ctx, object[] _)
    {
        ctx.Value.Append('!');
        return null!;
    }

    [TearDown]
    public void TearDown()
    {
        DummyVariableReplacers.Reset();
    }

    // --- Plain text (no variables) ---

    [Test]
    public void Process_PlainText_ReturnsUnchanged()
    {
        string result = DummyGameText.Process("Hello world");

        Assert.That(result, Is.EqualTo("Hello world"));
    }

    [Test]
    public void Process_EmptyString_ReturnsEmpty()
    {
        string result = DummyGameText.Process("");

        Assert.That(result, Is.EqualTo(""));
    }

    // --- Simple key replacement ---

    [Test]
    public void Process_SimpleKey_ReplacesWithReplacerResult()
    {
        string result = DummyGameText.Process("Say =greeting=!");

        Assert.That(result, Is.EqualTo("Say hello!"));
    }

    [Test]
    public void Process_MultipleVariables_ReplacesAll()
    {
        string result = DummyGameText.Process("=greeting= =greeting=");

        Assert.That(result, Is.EqualTo("hello hello"));
    }

    [Test]
    public void Process_AdjacentVariables_ReplacesAll()
    {
        string result = DummyGameText.Process("=greeting==greeting=");

        Assert.That(result, Is.EqualTo("hellohello"));
    }

    // --- Default value ---

    [Test]
    public void Process_ReplacerWithDefault_PropagatesDefaultToContext()
    {
        string result = DummyGameText.Process("a =name=");

        Assert.That(result, Is.EqualTo("a thing"));
    }

    // --- Empty variable == ---

    [Test]
    public void Process_DoubleEquals_TreatedAsLiteral()
    {
        // "==" has bufferPos==0 at closing '=' → abort, output "=="
        string result = DummyGameText.Process("x==y");

        Assert.That(result, Is.EqualTo("x==y"));
    }

    // --- Abort on space/newline ---

    [Test]
    public void Process_SpaceInKeySection_AbortsVariable()
    {
        string result = DummyGameText.Process("=no key=");

        Assert.That(result, Is.EqualTo("=no key="));
    }

    [Test]
    public void Process_NewlineInKeySection_AbortsVariable()
    {
        string result = DummyGameText.Process("=no\nkey=");

        Assert.That(result, Is.EqualTo("=no\nkey="));
    }

    // --- Parameter section (:) ---

    [Test]
    public void Process_ParameterSection_PassesParameterToReplacer()
    {
        string result = DummyGameText.Process("=echo:world=");

        Assert.That(result, Is.EqualTo("world"));
    }

    [Test]
    public void Process_MultipleParameters_AllPassedToReplacer()
    {
        string result = DummyGameText.Process("=concat:a:b:c=");

        Assert.That(result, Is.EqualTo("a+b+c"));
    }

    // --- Pipe (postprocessor) ---

    [Test]
    public void Process_PipePostProcessor_AppliesPostToReplacerResult()
    {
        string result = DummyGameText.Process("=greeting|upper=");

        Assert.That(result, Is.EqualTo("HELLO"));
    }

    [Test]
    public void Process_MultiplePipes_ChainsPostProcessors()
    {
        string result = DummyGameText.Process("=greeting|upper|addBang=");

        Assert.That(result, Is.EqualTo("HELLO!"));
    }

    // --- Space in post-key section aborts ---

    [Test]
    public void Process_SpaceInPostKeySection_AbortsVariable()
    {
        string result = DummyGameText.Process("=greeting|no post=");

        Assert.That(result, Is.EqualTo("=greeting|no post="));
    }

    // --- Subject/Object argument resolution ---

    [Test]
    public void Process_SubjectArgument_ResolvesToIndex0()
    {
        // "subject" maps to TARGET_SUBJECT=-2, which resolves to arguments[0]
        List<object> args = ["test-subject"];
        string result = DummyGameText.Process("=subject=", arguments: args);

        Assert.That(result, Is.EqualTo("test-subject"));
    }

    [Test]
    public void Process_ObjectArgument_ResolvesToIndex1()
    {
        List<object> args = ["subj", "test-object"];
        string result = DummyGameText.Process("=object=", arguments: args);

        Assert.That(result, Is.EqualTo("test-object"));
    }

    // --- Alias resolution ---

    [Test]
    public void Process_AliasResolution_UsesAliasedIndex()
    {
        List<object> args = ["zero", "one", "target"];
        Dictionary<string, int> aliases = new() { ["myAlias"] = 2 };
        string result = DummyGameText.Process("=myAlias=", arguments: args, aliases: aliases);

        Assert.That(result, Is.EqualTo("target"));
    }

    // --- Argument section (#) ---

    [Test]
    public void Process_ArgumentSection_ResolvesAdditionalArgument()
    {
        // =toString#subject= → toString(arguments[0])
        List<object> args = [42];
        string result = DummyGameText.Process("=toString#subject=", arguments: args);

        Assert.That(result, Is.EqualTo("42"));
    }

    // --- Dot notation (subject type) ---

    [Test]
    public void Process_DotNotation_SetsSubjectTypeAndKey()
    {
        // =subject.toString= → resolve "subject" as argument (sets Type=Subject),
        // then "toString" as key
        List<object> args = ["hero"];
        string result = DummyGameText.Process("=subject.toString=", arguments: args);

        Assert.That(result, Is.EqualTo("hero"));
    }

    // --- Unknown key throws ---

    [Test]
    public void Process_UnknownKey_CatchesAndContinues()
    {
        // Faithful to beta: unknown keys are caught and silently skipped (line 598-605)
        string result = DummyGameText.Process("before =nonExistentKey= after");

        // Variable expression is left in place when resolution fails
        Assert.That(result, Is.EqualTo("before =nonExistentKey= after"));
    }

    // --- ArgumentGenerator lazy evaluation ---

    [Test]
    public void Process_ArgumentGenerator_LazilyEvaluated()
    {
        int callCount = 0;
        DummyArgumentGenerator gen = () => { callCount++; return "generated"; };
        List<object> args = [(object)gen];

        string result = DummyGameText.Process("=subject=", arguments: args);

        Assert.That(result, Is.EqualTo("generated"));
        Assert.That(callCount, Is.EqualTo(1));
    }

    [Test]
    public void Process_ArgumentGenerator_ReplacedInArgumentsList()
    {
        DummyArgumentGenerator gen = () => "replaced";
        List<object> args = [(object)gen];

        DummyGameText.Process("=subject=", arguments: args);

        // After processing, the generator should be replaced with its result
        Assert.That(args[0], Is.EqualTo("replaced"));
    }

    // --- Capitalize flag ---

    [Test]
    public void Process_CapitalizeFlag_PropagatedToContext()
    {
        // Register a replacer with capitalize=true
        DummyVariableReplacers.Register(
            ["Greeting"],
            new DummyReplacerEntry(
                (ctx, _) => ctx.Capitalize ? "Hello" : "hello",
                [],
                capitalize: true));

        string result = DummyGameText.Process("=Greeting=");

        Assert.That(result, Is.EqualTo("Hello"));
    }

    // --- Text around variables preserved ---

    [Test]
    public void Process_TextAroundVariable_Preserved()
    {
        string result = DummyGameText.Process("before =greeting= after");

        Assert.That(result, Is.EqualTo("before hello after"));
    }

    // --- Replacer returning null uses Value builder ---

    [Test]
    public void Process_ReplacerReturnsNull_UsesValueBuilder()
    {
        // Register a replacer that writes to ctx.Value and returns null
        DummyVariableReplacers.Register("writeValue",
            (ctx, _) => { ctx.Value.Append("from-value"); return null; });

        string result = DummyGameText.Process("=writeValue=");

        Assert.That(result, Is.EqualTo("from-value"));
    }
}

#pragma warning restore CA1707
#pragma warning restore CA1515
