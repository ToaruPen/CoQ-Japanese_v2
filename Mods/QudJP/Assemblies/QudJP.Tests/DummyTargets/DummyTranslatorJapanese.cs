#pragma warning disable CA1707

using System.Globalization;
using System.Text;

namespace QudJP.Tests.DummyTargets;

[DummyLanguageProvider("ja")]
internal sealed class DummyTranslatorJapanese : DummyTranslatorBase
{
    private static readonly string[] Digits =
    [
        "零",
        "一",
        "二",
        "三",
        "四",
        "五",
        "六",
        "七",
        "八",
        "九",
    ];

    private static readonly (ulong Value, string Name)[] LargeMagnitudes =
    [
        (1_0000_0000_0000UL, "兆"),
        (1_0000_0000UL, "億"),
        (1_0000UL, "万"),
    ];

    private CultureInfo? cultureInfo;
    private StringComparer? stringComparer;
    private StringComparer? stringComparerIgnoreCase;

    public override string Cardinal(long number)
    {
        bool negative = number < 0;
        ulong absolute = GetAbsoluteValue(number);
        string cardinal = Cardinal(absolute);
        return negative ? string.Concat("マイナス", cardinal) : cardinal;
    }

    public override string Ordinal(long number) => string.Concat("第", Cardinal(number));

    public override string OrdinalWithDigits(long number) => string.Concat("第", number.ToString(CultureInfo.InvariantCulture));

    public override string CardinalNo(long number) => number == 0 ? "零" : Cardinal(number);

    public override string Multiplicative(long number) => number switch
    {
        0 => "零回",
        1 => "一度",
        2 => "二度",
        _ => string.Concat(number.ToString(CultureInfo.InvariantCulture), "回"),
    };

    public override string MakeCommaList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => string.Empty,
        1 => items[0],
        _ => string.Join("、", items),
    };

    public override string MakeAndList(IReadOnlyList<string> items, bool serialComma = true) => items.Count switch
    {
        0 => string.Empty,
        1 => items[0],
        2 => string.Concat(items[0], "と", items[1]),
        _ => string.Join("、", items),
    };

    public override void ExtractArticle(ref string name, out string article) => article = string.Empty;

    public override void NextPossibleLineBreakIndex(ReadOnlySpan<char> span, int startIndex, out int breakBeforeIndex, out bool replaceIfBroken)
    {
        int normalizedStartIndex = Math.Max(startIndex, 0);
        breakBeforeIndex = normalizedStartIndex >= span.Length - 1 ? span.Length : normalizedStartIndex + 1;
        replaceIfBroken = false;
    }

    public override CultureInfo GetCultureInfo() => cultureInfo ??= new CultureInfo("ja-JP");

    public override StringComparer GetStringComparer() => stringComparer ??= StringComparer.Create(GetCultureInfo(), ignoreCase: false);

    public override StringComparer GetStringComparerIgnoreCase() => stringComparerIgnoreCase ??= StringComparer.Create(GetCultureInfo(), ignoreCase: true);

    public override bool IsWordCharacter(char character) => char.IsLetterOrDigit(character) || character == '-';

    public override char ToLower(char character) => char.ToLower(character, GetCultureInfo());

    public override char ToUpper(char character) => char.ToUpper(character, GetCultureInfo());

    public override string DefiniteArticle(string name) => string.Empty;

    public override string IndefiniteArticle(string name) => string.Empty;

    private static ulong GetAbsoluteValue(long number) => number < 0 ? unchecked((ulong)(-(number + 1))) + 1UL : (ulong)number;

    private static string Cardinal(ulong number)
    {
        if (number == 0)
        {
            return Digits[0];
        }

        foreach ((ulong magnitude, string name) in LargeMagnitudes)
        {
            if (number >= magnitude)
            {
                ulong quotient = number / magnitude;
                ulong remainder = number % magnitude;
                string prefix = string.Concat(Cardinal(quotient), name);
                return remainder == 0 ? prefix : string.Concat(prefix, Cardinal(remainder));
            }
        }

        return CardinalBelowTenThousand(number);
    }

    private static string CardinalBelowTenThousand(ulong number)
    {
        if (number == 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new();

        AppendMagnitude(builder, number / 1000, "千");
        number %= 1000;
        AppendMagnitude(builder, number / 100, "百");
        number %= 100;
        AppendMagnitude(builder, number / 10, "十");

        ulong ones = number % 10;
        if (ones > 0)
        {
            builder.Append(Digits[(int)ones]);
        }

        return builder.ToString();
    }

    private static void AppendMagnitude(StringBuilder builder, ulong count, string magnitude)
    {
        if (count == 0)
        {
            return;
        }

        if (count > 1)
        {
            builder.Append(Digits[(int)count]);
        }

        builder.Append(magnitude);
    }
}

#pragma warning restore CA1707
