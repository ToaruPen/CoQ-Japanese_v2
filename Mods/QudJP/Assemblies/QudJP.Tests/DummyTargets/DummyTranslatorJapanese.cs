#pragma warning disable CA1707

using System.Globalization;
using System.Text;

namespace QudJP.Tests.DummyTargets;

[DummyLanguageProvider("ja")]
internal sealed class DummyTranslatorJapanese : DummyTranslatorBase
{
    private static readonly string[] KanjiDigits =
    [
        "〇",
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

    private CultureInfo? cultureInfo;
    private StringComparer? stringComparer;
    private StringComparer? stringComparerIgnoreCase;

    // --- 数量: 英数字 ---
    public override string Cardinal(long number) => number.ToString(CultureInfo.InvariantCulture);

    // --- 序列: 漢数字 ---
    public override string Ordinal(long number) => string.Concat("第", ToKanjiCardinal(number));

    // --- 日付等: 英数字のみ ---
    public override string OrdinalWithDigits(long number) => number.ToString(CultureInfo.InvariantCulture);

    // --- ゼロ個: 英数字 ---
    public override string CardinalNo(long number) => number.ToString(CultureInfo.InvariantCulture);

    // --- 回数: 英数字+回, 0は特殊 ---
    public override string Multiplicative(long number) => number == 0
        ? "一度もない"
        : string.Concat(number.ToString(CultureInfo.InvariantCulture), "回");

    public override string MakeCommaList(IReadOnlyList<string> items) => items.Count switch
    {
        0 => string.Empty,
        1 => items[0],
        _ => string.Join("、", items),
    };

    public override string MakeAndList(IReadOnlyList<string> items, bool serialComma = true)
    {
        _ = serialComma; // Oxford comma の概念は日本語に存在しないため未使用
        return items.Count switch
        {
            0 => string.Empty,
            1 => items[0],
            2 => string.Concat(items[0], "と", items[1]),
            _ => string.Join("、", items),
        };
    }

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

    /// <summary>Kanji cardinal for Ordinal use: 一, 十二, 百二十三.</summary>
    private static string ToKanjiCardinal(long number)
    {
        if (number < 0)
        {
            // Avoid negating long.MinValue which overflows; compute absolute value as ulong directly.
            ulong absNeg = number == long.MinValue ? (ulong)long.MaxValue + 1UL : (ulong)(-number);
            return string.Concat("マイナス", ToKanjiCardinalUnsigned(absNeg));
        }

        if (number == 0)
        {
            return KanjiDigits[0];
        }

        return ToKanjiCardinalUnsigned((ulong)number);
    }

    private static string ToKanjiCardinalUnsigned(ulong abs)
    {
        StringBuilder sb = new();
        AppendKanjiGroup(sb, ref abs, 1_0000_0000_0000_0000UL, "京");
        AppendKanjiGroup(sb, ref abs, 1_0000_0000_0000UL, "兆");
        AppendKanjiGroup(sb, ref abs, 1_0000_0000UL, "億");
        AppendKanjiGroup(sb, ref abs, 1_0000UL, "万");
        AppendKanjiBelowTenThousand(sb, abs);
        return sb.ToString();
    }

    private static void AppendKanjiGroup(StringBuilder sb, ref ulong number, ulong magnitude, string name)
    {
        if (number < magnitude)
        {
            return;
        }

        ulong quotient = number / magnitude;
        AppendKanjiBelowTenThousand(sb, quotient);
        sb.Append(name);
        number %= magnitude;
    }

    private static void AppendKanjiBelowTenThousand(StringBuilder sb, ulong number)
    {
        AppendKanjiMagnitude(sb, number / 1000, "千");
        number %= 1000;
        AppendKanjiMagnitude(sb, number / 100, "百");
        number %= 100;
        AppendKanjiMagnitude(sb, number / 10, "十");

        ulong ones = number % 10;
        if (ones > 0)
        {
            sb.Append(KanjiDigits[(int)ones]);
        }
    }

    private static void AppendKanjiMagnitude(StringBuilder sb, ulong count, string magnitude)
    {
        if (count == 0)
        {
            return;
        }

        if (count > 1)
        {
            sb.Append(KanjiDigits[(int)count]);
        }

        sb.Append(magnitude);
    }
}

#pragma warning restore CA1707
