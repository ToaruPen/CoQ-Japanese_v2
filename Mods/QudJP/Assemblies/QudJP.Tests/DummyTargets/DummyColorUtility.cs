#pragma warning disable CA1707

using System.Globalization;
using System.Text;

namespace QudJP.Tests.DummyTargets;

internal static class DummyColorUtility
{
    private const char ForegroundMarker = '&';
    private const char BackgroundMarker = '^';

    public static string StripFormatting(string? text)
    {
        if (text is null)
        {
            return string.Empty;
        }

        StringBuilder builder = new(text.Length);
        int position = 0;
        bool inControl = false;
        int controlDepth = 0;

        while (TryReadNextPrintable(text, ref position, ref inControl, ref controlDepth, null, out string? printable))
        {
            builder.Append(printable);
        }

        return builder.ToString();
    }

    public static bool HasFormatting(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return false;
        }

        if (text.Contains("{{", StringComparison.Ordinal))
        {
            return true;
        }

        int index = 0;
        while (index < text.Length - 1)
        {
            if (text[index] == ForegroundMarker)
            {
                index++;
                if (index < text.Length && text[index] != ForegroundMarker)
                {
                    return true;
                }
            }
            else if (text[index] == BackgroundMarker)
            {
                index++;
                if (index < text.Length && text[index] != BackgroundMarker)
                {
                    return true;
                }
            }

            index++;
        }

        return false;
    }

    public static int LengthExceptFormatting(string? text)
    {
        if (string.IsNullOrEmpty(text))
        {
            return 0;
        }

        int count = 0;
        int position = 0;
        bool inControl = false;
        int controlDepth = 0;

        while (TryReadNextPrintable(text, ref position, ref inControl, ref controlDepth, null, out _))
        {
            count++;
        }

        return count;
    }

    public static string? ClipExceptFormatting(string? text, int want)
    {
        if (text is null)
        {
            return null;
        }

        if (want <= 0)
        {
            return string.Empty;
        }

        StringBuilder builder = new(text.Length);
        int position = 0;
        bool inControl = false;
        int controlDepth = 0;
        int count = 0;

        while (count < want && TryReadNextPrintable(text, ref position, ref inControl, ref controlDepth, builder, out string? printable))
        {
            builder.Append(printable);
            count++;
        }

        for (int index = 0; index < controlDepth; index++)
        {
            builder.Append("}}");
        }

        return builder.ToString();
    }

    public static string? EscapeFormatting(string? text)
    {
        if (text is null)
        {
            return null;
        }

        StringBuilder builder = new(text.Length * 2);
        foreach (char character in text)
        {
            builder.Append(character);
            if (character is ForegroundMarker or BackgroundMarker)
            {
                builder.Append(character);
            }
        }

        return builder.ToString();
    }

    private static bool TryReadNextPrintable(
        string text,
        ref int position,
        ref bool inControl,
        ref int controlDepth,
        StringBuilder? controlStore,
        out string? printable)
    {
        printable = null;

        while (position < text.Length)
        {
            if (inControl)
            {
                string controlElement = StringInfo.GetNextTextElement(text, position);
                controlStore?.Append(controlElement);
                position += controlElement.Length;
                if (controlElement == "|")
                {
                    inControl = false;
                }

                continue;
            }

            if (position + 1 < text.Length && text[position] == '{' && text[position + 1] == '{')
            {
                controlStore?.Append("{{");
                position += 2;
                inControl = true;
                controlDepth++;
                continue;
            }

            if (controlDepth > 0 && position + 1 < text.Length && text[position] == '}' && text[position + 1] == '}')
            {
                controlStore?.Append("}}");
                position += 2;
                controlDepth--;
                continue;
            }

            if (text[position] is ForegroundMarker or BackgroundMarker)
            {
                char marker = text[position];
                if (position + 1 < text.Length)
                {
                    char code = text[position + 1];
                    if (code != marker)
                    {
                        controlStore?.Append(marker).Append(code);
                        position += 2;
                        continue;
                    }

                    printable = marker.ToString();
                    position += 2;
                    return true;
                }
            }

            printable = StringInfo.GetNextTextElement(text, position);
            position += printable.Length;
            return true;
        }

        return false;
    }
}

#pragma warning restore CA1707
