#pragma warning disable CA1707

using System.Text;

namespace QudJP.Tests.DummyTargets;

internal static class DummyMarkup
{
    public static string Transform(string? text, bool refreshAtNewline = false)
    {
        if (string.IsNullOrEmpty(text) || !text.Contains("{{", StringComparison.Ordinal))
        {
            return text ?? string.Empty;
        }

        int position = 0;
        List<INode> nodes = ParseNodes(text, ref position, stopAtClose: false);
        return RenderNodes(nodes, 'y', prefixPlainTextAtStart: true, refreshAtNewline);
    }

    public static string Strip(string? text)
    {
        return DummyColorUtility.StripFormatting(text);
    }

    public static string Color(string color, string text)
    {
        return "{{" + color + "|" + text + "}}";
    }

    private static List<INode> ParseNodes(string text, ref int position, bool stopAtClose)
    {
        List<INode> nodes = [];
        StringBuilder plainText = new();

        while (position < text.Length)
        {
            if (stopAtClose && position + 1 < text.Length && text[position] == '}' && text[position + 1] == '}')
            {
                position += 2;
                break;
            }

            if (position + 1 < text.Length && text[position] == '{' && text[position + 1] == '{')
            {
                FlushPlainText(nodes, plainText);
                position += 2;
                int actionStart = position;

                while (position < text.Length && text[position] != '|')
                {
                    position++;
                }

                if (position >= text.Length)
                {
                    plainText.Append("{{").Append(text.AsSpan(actionStart));
                    break;
                }

                string action = text[actionStart..position];
                position++;
                List<INode> children = ParseNodes(text, ref position, stopAtClose: true);
                nodes.Add(new MarkupNode(action, children));
                continue;
            }

            plainText.Append(text[position]);
            position++;
        }

        FlushPlainText(nodes, plainText);
        return nodes;
    }

    private static void FlushPlainText(List<INode> nodes, StringBuilder plainText)
    {
        if (plainText.Length == 0)
        {
            return;
        }

        nodes.Add(new TextNode(plainText.ToString()));
        plainText.Clear();
    }

    private static string RenderNodes(IReadOnlyList<INode> nodes, char initialColor, bool prefixPlainTextAtStart, bool refreshAtNewline)
    {
        StringBuilder builder = new();
        char currentColor = initialColor;
        bool shouldPrefixPlainText = prefixPlainTextAtStart;

        for (int index = 0; index < nodes.Count; index++)
        {
            if (nodes[index] is TextNode textNode)
            {
                RenderText(textNode.Text, builder, ref currentColor, shouldPrefixPlainText, refreshAtNewline);
                shouldPrefixPlainText = false;
                continue;
            }

            MarkupNode markupNode = (MarkupNode)nodes[index];
            char parentColor = currentColor;
            char childColor = ResolveColor(markupNode.Action, parentColor);
            string childOutput = RenderNodes(markupNode.Children, childColor, prefixPlainTextAtStart: false, refreshAtNewline);
            bool emitLeadingColor = markupNode.Action.Length > 0 && !StartsWithExplicitForeground(childOutput);

            if (emitLeadingColor)
            {
                builder.Append('&').Append(childColor);
            }

            builder.Append(childOutput);

            if (index < nodes.Count - 1 && builder.Length > 0)
            {
                builder.Append('&').Append(parentColor);
            }

            currentColor = parentColor;
            shouldPrefixPlainText = false;
        }

        return builder.ToString();
    }

    private static void RenderText(string text, StringBuilder builder, ref char currentColor, bool prefixAtStart, bool refreshAtNewline)
    {
        bool prefixed = !prefixAtStart;

        int index = 0;
        while (index < text.Length)
        {
            char character = text[index];
            if (character is '&' or '^' && index + 1 < text.Length)
            {
                char next = text[index + 1];
                builder.Append(character).Append(next);

                if (character == '&' && next != '&')
                {
                    currentColor = next;
                    prefixed = true;
                }

                if (refreshAtNewline && next == '\n')
                {
                    builder.Append('&').Append(currentColor);
                }

                index += 2;
                continue;
            }

            if (!prefixed)
            {
                builder.Append('&').Append(currentColor);
                prefixed = true;
            }

            builder.Append(character);

            if (refreshAtNewline && character == '\n')
            {
                builder.Append('&').Append(currentColor);
            }

            index++;
        }
    }

    private static bool StartsWithExplicitForeground(string text)
    {
        return text.Length >= 2 && text[0] == '&' && text[1] != '&';
    }

    private static char ResolveColor(string action, char fallbackColor)
    {
        return action.Length > 0 ? action[0] : fallbackColor;
    }

    private interface INode;

    private sealed class TextNode(string text) : INode
    {
        public string Text { get; } = text;
    }

    private sealed class MarkupNode(string action, List<INode> children) : INode
    {
        public string Action { get; } = action;

        public List<INode> Children { get; } = children;
    }
}

#pragma warning restore CA1707
