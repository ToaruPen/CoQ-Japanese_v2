#pragma warning disable CA1515
#pragma warning disable CA1707
#if HAS_GAME_DLL

using System.Reflection;
using HarmonyLib;
using QudJP.Patches;
using XRL.Language;
using XRL.World.Text;

namespace QudJP.Tests.L2G;

/// <summary>
/// L2G wiring tests for Grammar Harmony patches.
/// Verifies [HarmonyPatch] attribute target resolution, Prefix signature
/// compatibility, and runtime type resolution against the game DLL.
/// </summary>
[Category("L2G")]
[TestFixture]
public sealed class GrammarPatchWiringTests
{
    private static readonly Type[] AllPatchTypes =
    [
        typeof(GrammarPluralizePatch),
        typeof(GrammarArticleStringPatch),
        typeof(GrammarArticleStringBuilderPatch),
        typeof(GrammarArticleTextBuilderPatch),
        typeof(GrammarArticleNumberPatch),
        typeof(GrammarArticleNumberStringBuilderPatch),
        typeof(GrammarMakePossessivePatch),
        typeof(GrammarMakeAndListPatch),
        typeof(GrammarMakeOrListArrayPatch),
        typeof(GrammarMakeOrListPatch),
        typeof(GrammarInitCapPatch),
        typeof(GrammarInitLowerPatch),
        typeof(GrammarThirdPersonPatch),
        typeof(GrammarPastTenseOfPatch),
    ];

    private static IEnumerable<TestCaseData> PatchTypeCases()
    {
        foreach (var type in AllPatchTypes)
        {
            yield return new TestCaseData(type).SetName(type.Name);
        }
    }

    [TestCaseSource(nameof(PatchTypeCases))]
    public void PatchTarget_ResolvesToGameMethod(Type patchType)
    {
        var (targetType, methodName, argTypes) = ResolvePatchAttributes(patchType);

        Assert.That(targetType, Is.Not.Null, "declaringType must be set");
        Assert.That(methodName, Is.Not.Null, "methodName must be set");

        var target = AccessTools.Method(targetType, methodName, argTypes);
        Assert.That(target, Is.Not.Null,
            $"Grammar.{methodName}({FormatTypes(argTypes)}) not found in game DLL");
    }

    [TestCaseSource(nameof(PatchTypeCases))]
    public void PrefixParameters_AlignWithTarget(Type patchType)
    {
        var (targetType, methodName, argTypes) = ResolvePatchAttributes(patchType);
        Assert.That(targetType, Is.Not.Null, "declaringType must be set");
        Assert.That(methodName, Is.Not.Null, "methodName must be set");

        var target = AccessTools.Method(targetType, methodName, argTypes);
        Assert.That(target, Is.Not.Null, "target method must resolve");

        var prefix = AccessTools.Method(patchType, "Prefix");
        Assert.That(prefix, Is.Not.Null, "Prefix method must exist");

        var targetParamsByName = target!.GetParameters()
            .ToDictionary(p => p.Name!, p => p.ParameterType, StringComparer.OrdinalIgnoreCase);

        foreach (var pp in prefix!.GetParameters())
        {
            if (pp.Name!.StartsWith("__", StringComparison.Ordinal))
            {
                if (pp.Name == "__result")
                {
                    Assert.That(pp.ParameterType, Is.EqualTo(target.ReturnType.MakeByRefType()),
                        "__result type must match target return type (by ref)");
                }

                continue;
            }

            Assert.That(targetParamsByName, Does.ContainKey(pp.Name),
                $"Prefix parameter '{pp.Name}' has no counterpart in target");
            Assert.That(pp.ParameterType, Is.EqualTo(targetParamsByName[pp.Name]),
                $"Prefix parameter '{pp.Name}' type mismatch");
        }
    }

    [TestCaseSource(nameof(PatchTypeCases))]
    public void PrefixMethod_IsStaticBoolWithHarmonyAttribute(Type patchType)
    {
        var prefix = AccessTools.Method(patchType, "Prefix");

        Assert.That(prefix, Is.Not.Null, "Prefix method must exist");
        Assert.Multiple(() =>
        {
            Assert.That(prefix!.ReturnType, Is.EqualTo(typeof(bool)), "must return bool");
            Assert.That(prefix.IsStatic, Is.True, "must be static");
            Assert.That(prefix.GetCustomAttribute<HarmonyPrefix>(), Is.Not.Null,
                "[HarmonyPrefix] attribute required");
        });
    }

    [Test]
    public void AllPatchTypes_TargetGrammarClass()
    {
        Assert.Multiple(() =>
        {
            foreach (var patchType in AllPatchTypes)
            {
                var (targetType, _, _) = ResolvePatchAttributes(patchType);
                Assert.That(targetType, Is.EqualTo(typeof(Grammar)),
                    $"{patchType.Name} must target Grammar class");
            }
        });
    }

    [Test]
    public void TextBuilderAppendString_IsResolvable()
    {
        var appendMethod = typeof(TextBuilder).GetMethod(
            nameof(TextBuilder.Append), [typeof(string)]);
        Assert.That(appendMethod, Is.Not.Null,
            "TextBuilder.Append(string) must exist for GrammarArticleTextBuilderPatch");
    }

    [Test]
    public void PatchCount_Matches14ExpectedPatches()
    {
        Assert.That(AllPatchTypes, Has.Length.EqualTo(14),
            "Expected 14 Grammar Harmony prefix patches");
    }

    private static (Type? declaringType, string? methodName, Type[]? argumentTypes) ResolvePatchAttributes(
        Type patchType)
    {
        Type? declaringType = null;
        string? methodName = null;
        Type[]? argumentTypes = null;

        foreach (var attr in patchType.GetCustomAttributes<HarmonyPatch>())
        {
            var info = attr.info;
            declaringType ??= info.declaringType;
            methodName ??= info.methodName;
            argumentTypes ??= info.argumentTypes;
        }

        return (declaringType, methodName, argumentTypes);
    }

    private static string FormatTypes(Type[]? types) =>
        types == null ? "" : string.Join(", ", types.Select(t => t.Name));
}

#endif
#pragma warning restore CA1707
#pragma warning restore CA1515
