#pragma warning disable CA1707

using System.Globalization;

namespace QudJP.Tests.DummyTargets;

internal abstract class DummyTranslatorBase
{
    private static readonly (ulong Value, string Name)[] LargeMagnitudes =
    [
        (1_000_000_000_000_000_000UL, "quintillion"),
        (1_000_000_000_000_000UL, "quadrillion"),
        (1_000_000_000_000UL, "trillion"),
        (1_000_000_000UL, "billion"),
        (1_000_000UL, "million"),
        (1_000UL, "thousand"),
    ];

    private static readonly string[] CardinalsUnderTwenty =
    [
        "zero",
        "one",
        "two",
        "three",
        "four",
        "five",
        "six",
        "seven",
        "eight",
        "nine",
        "ten",
        "eleven",
        "twelve",
        "thirteen",
        "fourteen",
        "fifteen",
        "sixteen",
        "seventeen",
        "eighteen",
        "nineteen",
    ];

    private static readonly string[] OrdinalsUnderTwenty =
    [
        "zeroth",
        "first",
        "second",
        "third",
        "fourth",
        "fifth",
        "sixth",
        "seventh",
        "eighth",
        "ninth",
        "tenth",
        "eleventh",
        "twelfth",
        "thirteenth",
        "fourteenth",
        "fifteenth",
        "sixteenth",
        "seventeenth",
        "eighteenth",
        "nineteenth",
    ];

    private static readonly string[] CardinalTens =
    [
        string.Empty,
        string.Empty,
        "twenty",
        "thirty",
        "forty",
        "fifty",
        "sixty",
        "seventy",
        "eighty",
        "ninety",
    ];

    private static readonly string[] OrdinalTens =
    [
        string.Empty,
        string.Empty,
        "twentieth",
        "thirtieth",
        "fortieth",
        "fiftieth",
        "sixtieth",
        "seventieth",
        "eightieth",
        "ninetieth",
    ];

    private static CultureInfo? s_cultureInfo;
    private static StringComparer? s_stringComparer;
    private static StringComparer? s_stringComparerIgnoreCase;

    public virtual string Cardinal(long number)
    {
        bool negative = number < 0;
        ulong absolute = GetAbsoluteValue(number);
        string cardinal = Cardinal(absolute);
        return negative ? $"negative {cardinal}" : cardinal;
    }

    public virtual string Ordinal(long number)
    {
        bool negative = number < 0;
        ulong absolute = GetAbsoluteValue(number);
        string ordinal = Ordinal(absolute);
        return negative ? $"negative {ordinal}" : ordinal;
    }

    public virtual string OrdinalWithDigits(long number) => string.Concat(number, GetOrdinalSuffix(GetAbsoluteValue(number)));

    public virtual string CardinalNo(long number) => number == 0 ? "no" : Cardinal(number);

    public virtual string Multiplicative(long number) => number switch
    {
        1 => "once",
        2 => "twice",
        _ => $"{Cardinal(number)} times",
    };

    public virtual string MakeCommaList(IReadOnlyList<string> items)
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        if (items.Count == 1)
        {
            return items[0];
        }

        string separator = ContainsSeparator(items, ',') ? "; " : ", ";
        return string.Join(separator, items);
    }

    public virtual string MakeAndList(IReadOnlyList<string> items, bool serialComma = true)
    {
        if (items.Count == 0)
        {
            return string.Empty;
        }

        if (items.Count == 1)
        {
            return items[0];
        }

        if (items.Count == 2)
        {
            return string.Concat(items[0], " and ", items[1]);
        }

        string separator = ContainsSeparator(items, ',') ? "; " : ", ";
        string leading = string.Join(separator, items.Take(items.Count - 1));
        string bridge = serialComma ? separator : " ";
        return string.Concat(leading, bridge, "and ", items[^1]);
    }

    public virtual void ExtractArticle(ref string name, out string article)
    {
        article = string.Empty;
        foreach ((string Prefix, string Article) candidate in new[]
        {
            ("the ", "the"),
            ("a ", "a"),
            ("an ", "a"),
            ("some ", "some"),
        })
        {
            if (name.StartsWith(candidate.Prefix, StringComparison.OrdinalIgnoreCase))
            {
                article = candidate.Article;
                name = name[candidate.Prefix.Length..];
                return;
            }
        }
    }

    public virtual void NextPossibleLineBreakIndex(ReadOnlySpan<char> span, int startIndex, out int breakBeforeIndex, out bool replaceIfBroken)
    {
        for (int index = Math.Max(startIndex, 0); index < span.Length; index++)
        {
            if (span[index] is ' ' or '\n')
            {
                breakBeforeIndex = index;
                replaceIfBroken = true;
                return;
            }
        }

        breakBeforeIndex = span.Length;
        replaceIfBroken = false;
    }

    public virtual CultureInfo GetCultureInfo() => s_cultureInfo ??= CultureInfo.GetCultureInfo("en-US");

    public virtual StringComparer GetStringComparer() => s_stringComparer ??= StringComparer.Create(GetCultureInfo(), ignoreCase: false);

    public virtual StringComparer GetStringComparerIgnoreCase() => s_stringComparerIgnoreCase ??= StringComparer.Create(GetCultureInfo(), ignoreCase: true);

    public virtual bool IsWordCharacter(char character) => char.IsLetterOrDigit(character) || character == '-';

    public virtual char ToLower(char character) => char.ToLower(character, GetCultureInfo());

    public virtual char ToUpper(char character) => char.ToUpper(character, GetCultureInfo());

    public virtual string DefiniteArticle(string name) => throw new NotImplementedException();

    public virtual string IndefiniteArticle(string name) => throw new NotImplementedException();

    public virtual string Stutterize(string text) => throw new NotImplementedException();

    public virtual string Weirdify(string text) => throw new NotImplementedException();

    public virtual string Unweirdify(string text) => throw new NotImplementedException();

    public virtual string GameObjectDisplayName(object gameObject) => throw new NotImplementedException();

    public virtual string MutateZoneName(string zoneName) => throw new NotImplementedException();

    public virtual string AddArticle(string name) => throw new NotImplementedException();

    public virtual DummyDescriptionBuilder CreateDescriptionBuilder(int cutoff = int.MaxValue, bool baseOnly = false) => throw new NotImplementedException();

    private static ulong GetAbsoluteValue(long number) => number < 0 ? unchecked((ulong)(-(number + 1))) + 1UL : (ulong)number;

    private static bool ContainsSeparator(IEnumerable<string> items, char separator) => items.Any(item => item.Contains(separator, StringComparison.Ordinal));

    private static string Cardinal(ulong number)
    {
        if (number < 20)
        {
            return CardinalsUnderTwenty[(int)number];
        }

        if (number < 100)
        {
            ulong remainder = number % 10;
            string tens = CardinalTens[(int)(number / 10)];
            return remainder == 0 ? tens : string.Concat(tens, "-", CardinalsUnderTwenty[(int)remainder]);
        }

        if (number < 1_000)
        {
            ulong remainder = number % 100;
            string hundreds = string.Concat(CardinalsUnderTwenty[(int)(number / 100)], " hundred");
            return remainder == 0 ? hundreds : string.Concat(hundreds, " ", Cardinal(remainder));
        }

        foreach ((ulong magnitude, string name) in LargeMagnitudes)
        {
            if (number >= magnitude)
            {
                ulong remainder = number % magnitude;
                string prefix = string.Concat(Cardinal(number / magnitude), " ", name);
                return remainder == 0 ? prefix : string.Concat(prefix, " ", Cardinal(remainder));
            }
        }

        throw new InvalidOperationException("Unsupported cardinal value.");
    }

    private static string Ordinal(ulong number)
    {
        if (number < 20)
        {
            return OrdinalsUnderTwenty[(int)number];
        }

        if (number < 100)
        {
            ulong remainder = number % 10;
            ulong tens = number / 10;
            return remainder == 0
                ? OrdinalTens[(int)tens]
                : string.Concat(CardinalTens[(int)tens], "-", Ordinal(remainder));
        }

        if (number < 1_000)
        {
            ulong remainder = number % 100;
            string hundreds = string.Concat(CardinalsUnderTwenty[(int)(number / 100)], " hundred");
            return remainder == 0 ? string.Concat(hundreds, "th") : string.Concat(hundreds, " ", Ordinal(remainder));
        }

        foreach ((ulong magnitude, string name) in LargeMagnitudes)
        {
            if (number >= magnitude)
            {
                ulong remainder = number % magnitude;
                string prefix = string.Concat(Cardinal(number / magnitude), " ", name);
                return remainder == 0 ? string.Concat(prefix, "th") : string.Concat(prefix, " ", Ordinal(remainder));
            }
        }

        throw new InvalidOperationException("Unsupported ordinal value.");
    }

    private static string GetOrdinalSuffix(ulong number)
    {
        ulong lastTwoDigits = number % 100;
        if (lastTwoDigits is 11 or 12 or 13)
        {
            return "th";
        }

        return (number % 10) switch
        {
            1 => "st",
            2 => "nd",
            3 => "rd",
            _ => "th",
        };
    }
}

internal abstract class DummyDescriptionBuilder
{
    public abstract override string ToString();
}

#pragma warning restore CA1707
