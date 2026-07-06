using System.Globalization;
using System.Text;

namespace Esolang.Piet.Parser;

static class NetpbmPpmParser
{
    public static bool LooksLikeP3(byte[] bytes, CancellationToken cancellationToken = default)
    {
        if (!TryReadTokens(bytes, out var tokens, cancellationToken))
            return false;
        return tokens.Count > 0 && string.Equals(tokens[0], "P3", StringComparison.Ordinal);
    }

    public static byte[] ParseP3(byte[] bytes, out int width, out int height, CancellationToken cancellationToken = default)
    {
        if (!TryParseP3(bytes, out width, out height, out var rgbPixels, cancellationToken))
            throw new InvalidDataException("Invalid Netpbm PPM (P3) image.");
        return rgbPixels;
    }

    public static bool TryParseP3(byte[] bytes, out int width, out int height, out byte[] rgbPixels, CancellationToken cancellationToken = default)
    {
        width = 0;
        height = 0;
        rgbPixels = default!;

        if (!TryReadTokens(bytes, out var tokens, cancellationToken))
            return false;
        if (tokens.Count < 4 || !string.Equals(tokens[0], "P3", StringComparison.Ordinal))
            return false;
        if (!int.TryParse(tokens[1], NumberStyles.None, CultureInfo.InvariantCulture, out width) || width <= 0)
            return false;
        if (!int.TryParse(tokens[2], NumberStyles.None, CultureInfo.InvariantCulture, out height) || height <= 0)
            return false;
        if (!int.TryParse(tokens[3], NumberStyles.None, CultureInfo.InvariantCulture, out var maxColorValue) || maxColorValue <= 0)
            return false;

        int pixelCount;
        int componentCount;
        try
        {
            pixelCount = checked(width * height);
            componentCount = checked(pixelCount * 3);
        }
        catch (OverflowException)
        {
            return false;
        }

        if (tokens.Count != 4 + componentCount)
            return false;

        rgbPixels = new byte[componentCount];
        for (var i = 0; i < componentCount; i++)
        {
            cancellationToken.ThrowIfCancellationRequested();
            if (!int.TryParse(tokens[4 + i], NumberStyles.None, CultureInfo.InvariantCulture, out var component))
                return false;
            if ((uint)component > maxColorValue)
                return false;

            if (maxColorValue == 255)
            {
                rgbPixels[i] = (byte)component;
                continue;
            }

            var scaledComponent = component * 255 + maxColorValue / 2;
            rgbPixels[i] = (byte)(scaledComponent / maxColorValue);
        }

        return true;
    }

    static bool TryReadTokens(byte[] bytes, out List<string> tokens, CancellationToken cancellationToken = default)
    {
        tokens = [];
        if (bytes.Length == 0)
            return false;

        var builder = new StringBuilder();
        var inComment = false;

        foreach (var b in bytes)
        {
            cancellationToken.ThrowIfCancellationRequested();
            var ch = (char)b;
            if (inComment)
            {
                if (ch is '\r' or '\n')
                    inComment = false;
                continue;
            }

            if (ch == '#')
            {
                FlushToken(builder, tokens);
                inComment = true;
                continue;
            }

            if (char.IsWhiteSpace(ch))
            {
                FlushToken(builder, tokens);
                continue;
            }

            builder.Append(ch);
        }

        FlushToken(builder, tokens);
        return tokens.Count > 0;
    }

    static void FlushToken(StringBuilder builder, List<string> tokens)
    {
        if (builder.Length == 0)
            return;

        tokens.Add(builder.ToString());
        builder.Clear();
    }
}
