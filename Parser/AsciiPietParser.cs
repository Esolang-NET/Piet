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
    private static readonly Dictionary<char, PietColor> CharToColor = new()
    {
        // Black
        { '@', PietColor.Black },
        // Dark blue
        { 'a', PietColor.DarkBlue }, { 'A', PietColor.DarkBlue },
        // Dark green
        { 'b', PietColor.DarkGreen }, { 'B', PietColor.DarkGreen },
        // Dark cyan
        { 'c', PietColor.DarkCyan }, { 'C', PietColor.DarkCyan },
        // Dark red
        { 'd', PietColor.DarkRed }, { 'D', PietColor.DarkRed },
        // Dark magenta
        { 'e', PietColor.DarkMagenta }, { 'E', PietColor.DarkMagenta },
        // Dark yellow
        { 'f', PietColor.DarkYellow }, { 'F', PietColor.DarkYellow },
        // Blue
        { 'i', PietColor.Blue }, { 'I', PietColor.Blue },
        // Green
        { 'j', PietColor.Green }, { 'J', PietColor.Green },
        // Cyan
        { 'k', PietColor.Cyan }, { 'K', PietColor.Cyan },
        // Red
        { 'l', PietColor.Red }, { 'L', PietColor.Red },
        // Magenta
        { 'm', PietColor.Magenta }, { 'M', PietColor.Magenta },
        // Yellow
        { 'n', PietColor.Yellow }, { 'N', PietColor.Yellow },
        // Light blue
        { 'q', PietColor.LightBlue }, { 'Q', PietColor.LightBlue },
        // Light green
        { 'r', PietColor.LightGreen }, { 'R', PietColor.LightGreen },
        // Light cyan
        { 's', PietColor.LightCyan }, { 'S', PietColor.LightCyan },
        // Light red
        { 't', PietColor.LightRed }, { 'T', PietColor.LightRed },
        // Light magenta
        { 'u', PietColor.LightMagenta }, { 'U', PietColor.LightMagenta },
        // Light yellow
        { 'v', PietColor.LightYellow }, { 'V', PietColor.LightYellow },
        // White
        { '?', PietColor.White }, { '_', PietColor.White },
    };

    /// <summary>
    /// Loads a Piet program from an ascii-piet text file.
    /// </summary>
    public static PietProgram Parse(string path)
    {
        var lines = File.ReadAllLines(path)
            .Where(l => !string.IsNullOrWhiteSpace(l))
            .ToArray();
        if (lines.Length == 0)
            throw new InvalidDataException("ascii-piet file is empty");

        int height = lines.Length;
        int width = lines.Max(l => l.Length);
        var codels = new List<PietColor>(width * height);

        for (int y = 0; y < height; y++)
        {
            var line = lines[y].PadRight(width);
            for (int x = 0; x < width; x++)
            {
                char ch = line[x];
                if (!CharToColor.TryGetValue(ch, out var color))
                    throw new InvalidDataException($"Unknown ascii-piet char: '{ch}' at ({x},{y})");
                codels.Add(color);
            }
        }
        return new PietProgram(width, height, codels);
    }
}
