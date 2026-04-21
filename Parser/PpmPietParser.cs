using System.Globalization;

namespace Esolang.Piet.Parser;

/// <summary>
/// Parses Netpbm PPM (P3) format files into PietProgram instances.
/// </summary>
public static class PpmPietParser
{
    /// <summary>
    /// Loads a Piet program from a PPM (P3) text file.
    /// </summary>
    /// <param name="path">The file path to the PPM (P3) file.</param>
    /// <returns>A PietProgram instance representing the parsed PPM file.</returns>
    public static PietProgram Parse(string path)
    {
        using var stream = File.OpenRead(path);
        return InternalParse(stream);
    }

    /// <summary>
    /// Loads a Piet program from a PPM (P3) text file given as byte array.
    /// </summary>
    /// <param name="bytes">The byte array containing the PPM (P3) file data.</param>
    /// <returns>A PietProgram instance representing the parsed PPM file.</returns>
    public static PietProgram Parse(byte[] bytes)
    {
        using var stream = new MemoryStream(bytes);
        return InternalParse(stream);
    }
    static PietProgram InternalParse(Stream stream)
    {
        using var reader = new StreamReader(stream);
        string? line;
        // ヘッダ
        do { line = reader.ReadLine(); } while (line != null && string.IsNullOrWhiteSpace(line));
        if (line == null || !line.StartsWith("P3"))
            throw new InvalidDataException("Not a P3 PPM file");
        // コメント・空行スキップ
        string NextLine()
        {
            string? l;
            do { l = reader.ReadLine(); } while (l != null && (string.IsNullOrWhiteSpace(l) || l.TrimStart().StartsWith("#")));
            if (l == null) throw new InvalidDataException("Unexpected EOF in PPM header");
            return l;
        }
        // サイズ
        var sizeParts = NextLine().Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
        int width = int.Parse(sizeParts[0]);
        int height = int.Parse(sizeParts[1]);
        // 最大値
        int maxVal = int.Parse(NextLine());
        if (maxVal != 255) throw new InvalidDataException("Only maxVal=255 supported");
        // ピクセル値
        var pixels = new List<byte>();
        while ((line = reader.ReadLine()) != null)
        {
            if (string.IsNullOrWhiteSpace(line) || line.TrimStart().StartsWith("#")) continue;
            var parts = line.Split((char[]?)null, StringSplitOptions.RemoveEmptyEntries);
            foreach (var p in parts)
                pixels.Add(byte.Parse(p, CultureInfo.InvariantCulture));
        }
        if (pixels.Count != width * height * 3)
            throw new InvalidDataException($"PPM pixel count mismatch: expected {width * height * 3}, got {pixels.Count}");
        var codels = new PietColor[width * height];
        for (int i = 0; i < width * height; i++)
        {
            int r = pixels[i * 3];
            int g = pixels[i * 3 + 1];
            int b = pixels[i * 3 + 2];
            int colorIdx = PietParser.MapToPietColor((byte)r, (byte)g, (byte)b);
            if (colorIdx < 0) throw new InvalidDataException($"Unsupported color at pixel {i}: {r},{g},{b}");
            codels[i] = (PietColor)colorIdx;
        }
        return new PietProgram(width, height, codels);
    }
}
