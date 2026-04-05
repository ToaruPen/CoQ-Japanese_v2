#pragma warning disable CA1707

namespace QudJP.Tests.DummyTargets;

/// <summary>
/// Minimal injectable historic spice fixture for query-engine tests.
/// </summary>
internal static class DummyHistoricSpice
{
    private static Dictionary<string, object>? Root;

    public static void Reset()
    {
        Root = null;
    }

    public static void Init(Dictionary<string, object> fixture)
    {
        Root = fixture;
    }

    public static Dictionary<string, object> GetRoot()
    {
        Root ??= CreateDefaultFixture();
        return Root;
    }

    private static Dictionary<string, object> CreateDefaultFixture()
    {
        return new(StringComparer.Ordinal)
        {
            ["jewelry"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["tiara"] = string.Empty,
            },
            ["cooking"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["ate"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["You eat the meal."] = string.Empty,
                },
                ["recipeNames"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["categorizedFoods"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["stew"] = new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["seasoned stew"] = string.Empty,
                        },
                    },
                },
            },
            ["professions"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["gladiator"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["plural"] = "gladiators",
                    ["guildhall"] = "arena",
                },
            },
            ["elements"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["glass"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["mirror"] = string.Empty,
                    },
                },
                ["ice"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["hourglass"] = string.Empty,
                    },
                },
                ["time"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["nouns"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["atomic clock"] = string.Empty,
                    },
                },
            },
            ["history"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["gospels"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["LostItem"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["LateSultanate"] = new Dictionary<string, object>(StringComparer.Ordinal)
                        {
                            ["lost =spice.entity:possessivePronoun= prized =item= while the townsfolk were busy restoring the glass light bulb"] = string.Empty,
                        },
                    },
                },
            },
            ["villages"] = new Dictionary<string, object>(StringComparer.Ordinal)
            {
                ["mayor"] = new Dictionary<string, object>(StringComparer.Ordinal)
                {
                    ["reasonIBecame"] = new Dictionary<string, object>(StringComparer.Ordinal)
                    {
                        ["For years I spent my time =activity=. I came upon this village and its inhabitants. They asked me to employ my magisterial skills and lead the village."] = string.Empty,
                    },
                },
            },
        };
    }
}

#pragma warning restore CA1707
