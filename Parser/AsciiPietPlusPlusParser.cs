using System.Text;

namespace Esolang.Piet.Parser;

/// <summary>
/// Parses ascii-piet2 format text into <see cref="PietProgram"/> instances for the Piet++ language.
/// </summary>
/// <remarks>
/// <para>Character-to-color-index mapping:</para>
/// <list type="bullet">
///   <item><description><c>' '</c> (space) → 0 (Black)</description></item>
///   <item><description><c>'0'</c>–<c>'9'</c> → 1–10</description></item>
///   <item><description><c>'a'</c>–<c>'z'</c> → 11–36</description></item>
///   <item><description><c>'A'</c>–<c>'Z'</c> → 37–62</description></item>
///   <item><description><c>'~'</c> → 63 (White)</description></item>
///   <item><description><c>'|'</c> → end-of-line marker</description></item>
/// </list>
/// <para>Actual newline characters (<c>\r</c>, <c>\n</c>) are ignored and may appear freely.</para>
/// </remarks>
public static class AsciiPietPlusPlusParser
{
    /// <summary>The character used to mark the end of a row.</summary>
    public const char EolMarker = '|';

    static readonly Dictionary<char, int> CharToIndex =
        new[] { (' ', 0) }
        .Concat(Enumerable.Range(0, 10).Select(i => ((char)('0' + i), 1 + i)))
        .Concat(Enumerable.Range(0, 26).Select(i => ((char)('a' + i), 11 + i)))
        .Concat(Enumerable.Range(0, 26).Select(i => ((char)('A' + i), 37 + i)))
        .Append(('~', 63))
        .ToDictionary(t => t.Item1, t => t.Item2);

    static readonly HashSet<byte> AllowedBytes = [.. CharToIndex.Keys.Select(c => (byte)c).Concat([(byte)EolMarker, (byte)'\t', (byte)'\r', (byte)'\n'])];

    /// <summary>
    /// Determines whether the given byte array looks like an ascii-piet2 file
    /// (all bytes are in the allowed character set).
    /// </summary>
    public static bool LooksLikeAsciiPietPlusPlus(byte[] bytes)
    {
        foreach (var b in bytes)
            if (!AllowedBytes.Contains(b)) return false;
        return true;
    }

    /// <summary>
    /// Parses an ascii-piet2 byte array into a <see cref="PietProgram"/>.
    /// </summary>
    /// <param name="bytes">ASCII-encoded ascii-piet2 content.</param>
    /// <param name="codelSize">Codel size (1 or greater).</param>
    public static PietProgram Parse(byte[] bytes, int codelSize = 1)
    {
        if (codelSize < 1)
            throw new ArgumentOutOfRangeException(nameof(codelSize), "codelSize must be 1 or greater.");
        if (bytes.Length == 0)
            throw new InvalidDataException("ascii-piet2 file is empty.");
        var text = Encoding.ASCII.GetString(bytes).Replace("\r", "").Replace("\n", "");
        return InternalParse(text, codelSize);
    }

    /// <summary>
    /// Attempts to parse an ascii-piet2 byte array into a <see cref="PietProgram"/>.
    /// </summary>
    public static bool TryParse(byte[] bytes, int codelSize,
#if NETSTANDARD2_1_OR_GREATER
    [System.Diagnostics.CodeAnalysis.NotNullWhen(true)]
#endif
    out PietProgram program)
    {
        program = default!;
        if (bytes.Length == 0 || codelSize < 1) return false;
        try
        {
            program = Parse(bytes, codelSize);
            return true;
        }
        catch { return false; }
    }

    static PietProgram InternalParse(string text, int codelSize)
    {
        var lineList = new List<List<int>>();
        List<int>? currentLine = null;

        foreach (var ch in text)
        {
            if (ch == EolMarker)
            {
                if (currentLine is not null)
                {
                    lineList.Add(currentLine);
                    currentLine = null;
                }
                continue;
            }
            if (!CharToIndex.TryGetValue(ch, out var idx))
                throw new InvalidDataException($"Unknown ascii-piet2 character: '{ch}' (0x{(int)ch:X2})");
            (currentLine ??= []).Add(idx);
        }
        // Flush trailing row without EOL marker
        if (currentLine is { Count: > 0 })
            lineList.Add(currentLine);

        if (lineList.Count == 0)
            throw new InvalidDataException("ascii-piet2 file has no content.");

        var width = lineList.Max(l => l.Count);
        var height = lineList.Count;
        var flat = lineList
            .SelectMany(line => line.Concat(Enumerable.Repeat(0, width - line.Count)))
            .ToArray();

        if (codelSize == 1)
            return new PietProgram(width, height, [.. flat.Select(c => (PietColor)c)]);

        var cw = width / codelSize;
        var codelHeight = height / codelSize;
        var colors = new PietColor[cw * codelHeight];
        for (var y = 0; y < codelHeight; y++)
            for (var x = 0; x < cw; x++)
                colors[y * cw + x] = (PietColor)flat[y * codelSize * width + x * codelSize];
        return new PietProgram(cw, codelHeight, colors);
    }
}
