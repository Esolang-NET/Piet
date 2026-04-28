using System.Text;

namespace Esolang.Piet.Parser;

/// <summary>
/// Parses ascii-piet format text files into PietProgram instances.
/// </summary>
/// <seealso href="https://github.com/dloscutoff/ascii-piet#encoding-specification">ascii-piet encoding specification</seealso>
public static class AsciiPietParser
{
    /// <summary>
    /// ascii-piet 公式仕様に基づく文字→PietColor 対応表（通常用・行末用どちらも同じ色にマッピング）
    /// </summary>
    private static readonly Dictionary<char, (PietColor Color, bool IsEndOfLine)> CharToColor = new()
    {
        // Black
         {' ', (PietColor.Black, false) }, { '@', (PietColor.Black, true) },
        // Dark blue
        { 'a', (PietColor.DarkBlue, false) }, { 'A', (PietColor.DarkBlue, true) },
        // Dark green
        { 'b', (PietColor.DarkGreen, false) }, { 'B', (PietColor.DarkGreen, true) },
        // Dark cyan
        { 'c', (PietColor.DarkCyan, false) }, { 'C', (PietColor.DarkCyan, true) },
        // Dark red
        { 'd', (PietColor.DarkRed, false) }, { 'D', (PietColor.DarkRed, true) },
        // Dark magenta
        { 'e', (PietColor.DarkMagenta, false) }, { 'E', (PietColor.DarkMagenta, true) },
        // Dark yellow
        { 'f', (PietColor.DarkYellow, false) }, { 'F', (PietColor.DarkYellow, true) },
        // Blue
        { 'i', (PietColor.Blue, false) }, { 'I', (PietColor.Blue, true) },
        // Green
        { 'j', (PietColor.Green, false) }, { 'J', (PietColor.Green, true) },
        // Cyan
        { 'k', (PietColor.Cyan, false) }, { 'K', (PietColor.Cyan, true) },
        // Red
        { 'l', (PietColor.Red, false) }, { 'L', (PietColor.Red, true) },
        // Magenta
        { 'm', (PietColor.Magenta, false) }, { 'M', (PietColor.Magenta, true) },
        // Yellow
        { 'n', (PietColor.Yellow, false) }, { 'N', (PietColor.Yellow, true) },
        // Light blue
        { 'q', (PietColor.LightBlue, false) }, { 'Q', (PietColor.LightBlue, true) },
        // Light green
        { 'r', (PietColor.LightGreen, false) }, { 'R', (PietColor.LightGreen, true) },
        // Light cyan
        { 's', (PietColor.LightCyan, false) }, { 'S', (PietColor.LightCyan, true) },
        // Light red
        { 't', (PietColor.LightRed, false) }, { 'T', (PietColor.LightRed, true) },
        // Light magenta
        { 'u', (PietColor.LightMagenta, false) }, { 'U', (PietColor.LightMagenta, true) },
        // Light yellow
        { 'v', (PietColor.LightYellow, false) }, { 'V', (PietColor.LightYellow, true) },
        // White
        { '?', (PietColor.White, false) }, { '_', (PietColor.White, true) },
    };

    /// <summary>
    /// A set of characters usable in ASCII-PIET files, based on the official ASCII-PIET specification.
    /// </summary>
    static readonly HashSet<byte> AsciiPietAllowed = [.. CharToColor.Keys.Select(c => (byte)c).Concat(new byte[] { (byte)'\t', (byte)'\r', (byte)'\n' })];

    /// <summary>
    /// Determines whether the given byte array appears to be an ascii-piet text file.
    /// </summary>
    /// <param name="bytes"></param>
    /// <returns></returns>
    public static bool LooksLikeAsciiPiet(byte[] bytes)
    {
        foreach (var b in bytes)
        {
            if (!AsciiPietAllowed.Contains(b))
                return false;
        }
        return true;
    }

    /// <summary>
    /// Loads a Piet program from an ascii-piet text file represented as a byte array.
    /// The byte array is decoded as ASCII text.
    /// </summary>
    /// <param name="bytes">The byte array containing the ASCII-PIET text.</param>
    /// <param name="codelSize">The size of each codel in pixels.</param>
    /// <returns>A PietProgram instance representing the parsed ASCII-PIET text.</returns>
    public static PietProgram Parse(byte[] bytes, int codelSize = 1)
    {
        var text = Encoding.ASCII.GetString(bytes);
            var lines = text.Replace("\r", "").Replace("\n", "");
        if (codelSize < 1)
            throw new ArgumentOutOfRangeException(nameof(codelSize), "codelSize is support 1 or over.");
        if (lines.Length == 0)
            throw new InvalidDataException("ascii-piet file is empty");
        return InternalParse(lines, codelSize);
    }

    /// <summary>
    /// Attempts to load a Piet program from an ascii-piet text file represented as a byte array.
    /// The byte array is decoded as ASCII text.
    /// </summary>
    /// <param name="bytes">The byte array containing the ASCII-PIET text.</param>
    /// <param name="codelSize">The size of each codel in pixels.</param>
    /// <param name="program">The resulting PietProgram instance if parsing is successful.</param>
    /// <returns>True if parsing is successful; otherwise, false.</returns>
    public static bool TryParse(byte[] bytes, int codelSize, out PietProgram program)
    {
        program = default!;
        if (bytes.Length < 0) return false;
        if (codelSize < 1) return false;
        try
        {
            program = Parse(bytes, codelSize);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Internal method to parse ascii-piet text into a PietProgram instance.
    /// </summary>
    /// <param name="lines">The ASCII-PIET text lines.</param>
    /// <param name="codelSize">The size of each codel in pixels.</param>
    /// <returns>A PietProgram instance representing the parsed ASCII-PIET text.</returns>
    /// <exception cref="ArgumentOutOfRangeException"></exception>
    /// <exception cref="InvalidDataException"></exception>
    static PietProgram InternalParse(string lines, int codelSize = 1)
    {
        List<List<PietColor>> lineList = [];
        int x = 0;
        int y = 0;
        List<PietColor>? currentLine = null;
        foreach (var ch in lines)
        {
            if (!CharToColor.TryGetValue(ch, out var colorInfo))
                throw new InvalidDataException($"Unknown ascii-piet char: '{ch}' (0x{(int)ch:X}) at ({x},{y})");
            if (x == 0) lineList.Add(currentLine = []);
            var (color, isEndOfLine) = colorInfo;
            currentLine!.Add(color);
            if (isEndOfLine)
            {
                x = 0;
                y++;
                currentLine = null;
                continue;
            }
            x++;
        }
        var width = lineList.Max(l => l.Count);
        var height = lineList.Count;
        var codels = lineList.SelectMany(line => line.Concat(Enumerable.Repeat(PietColor.Black, width - line.Count))).ToArray();
        if (codelSize == 1)
            return new PietProgram(width, height, codels);
        {
            var codelWidth = width / codelSize;
            var codelHeight = height / codelSize;
            var colors = new PietColor[codelWidth * codelHeight];
            for (y = 0; y < codelHeight; y++)
            {
                for (x = 0; x < codelWidth; x++)
                {
                    colors[(y * codelWidth) + x] = (PietColor)codels[y * codelSize * width + (x * codelSize)];
                }
            }
            return new PietProgram(codelWidth, codelHeight, colors);
        }
    }
}
