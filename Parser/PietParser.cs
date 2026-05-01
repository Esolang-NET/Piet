using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.IO.Compression;
#if NETSTANDARD2_1_OR_GREATER
using System.Diagnostics.CodeAnalysis;
#endif

namespace Esolang.Piet.Parser;

/// <summary>
/// Parses Piet source images into normalized program data.
/// </summary>
public static class PietParser
{
    /// <summary>
    /// 
    /// </summary>
    /// <param name="bytes"></param>
    /// <param name="ext"></param>
    /// <param name="codelSize"></param>
    /// <param name="program"></param>
    /// <returns></returns>
    public static bool TryParse(byte[] bytes, string ext, int codelSize,
#if NETSTANDARD2_1_OR_GREATER
    [NotNullWhen(true)]
#endif
    out PietProgram program)
    {
        if (ext == ".txt" || ext == ".ascii-piet")
            return AsciiPietParser.TryParse(bytes, codelSize, out program);
        if (AsciiPietParser.LooksLikeAsciiPiet(bytes) && AsciiPietParser.TryParse(bytes, codelSize, out program))
            return true;
        if (TryParseInternal(bytes, codelSize, out program))
            return true;
        if (ext == ".png" && TryParseFallbackPng(bytes, codelSize, out program))
            return true;
        return false;

    }
    /// <summary>
    /// Loads a Piet program from raw image bytes with a specified extension.
    /// Supports .txt (ascii-piet), .ppm, and common image formats (PNG, JPEG, etc.).
    /// </summary>
    /// <param name="bytes">The byte array containing the image data.</param>
    /// <param name="ext">The file extension indicating the image format.</param>
    /// <param name="codelSize">The size of each codel in pixels.</param>
    /// <returns>A PietProgram instance representing the parsed image.</returns>
    /// <exception cref="ArgumentException"></exception>
    public static PietProgram Parse(byte[] bytes, string ext, int codelSize = 1)
    {
        if (ext == ".txt" || ext == ".ascii-piet")
            return AsciiPietParser.Parse(bytes, codelSize);
        if (AsciiPietParser.LooksLikeAsciiPiet(bytes) && AsciiPietParser.TryParse(bytes, codelSize, out var program))
            return program;
        try
        {
            using var image = Image.Load<Rgba32>(bytes);
            int codelWidth = image.Width / codelSize;
            int codelHeight = image.Height / codelSize;
            var colors = new PietColor[codelWidth * codelHeight];
            for (var y = 0; y < codelHeight; y++)
            {
                for (var x = 0; x < codelWidth; x++)
                {
                    // 左上ピクセルの色で代表とする（平均化したい場合はここを修正）
                    colors[(y * codelWidth) + x] = Normalize(image[x * codelSize, y * codelSize]);
                }
            }
            return new PietProgram(codelWidth, codelHeight, colors);
        }
        catch (Exception e) when (ext == ".png" && (e is InvalidImageContentException || e is UnknownImageFormatException))
        {
            // PNG形式であっても、ImageSharpが対応していない特殊なPNGの場合があるため、独自のPNGデコードを試みる
            return ParseWithRawPngFallback(bytes, codelSize);
        }

        throw new ArgumentException($"not supported image format: {ext}");
    }

    internal static bool TryParseInternal(byte[] bytes, int codelSize,
#if NETSTANDARD2_1_OR_GREATER
    [NotNullWhen(true)]
#endif
    out PietProgram program)
    {
        program = default!;
        if (bytes.Length <= 0) return false;
        if (codelSize < 1) return false;
        try
        {
            using var image = Image.Load<Rgba32>(bytes);
            int codelWidth = image.Width / codelSize;
            int codelHeight = image.Height / codelSize;
            var colors = new PietColor[codelWidth * codelHeight];
            for (var y = 0; y < codelHeight; y++)
            {
                for (var x = 0; x < codelWidth; x++)
                {
                    // 左上ピクセルの色で代表とする（平均化したい場合はここを修正）
                    colors[(y * codelWidth) + x] = Normalize(image[x * codelSize, y * codelSize]);
                }
            }
            program = new(codelWidth, codelHeight, colors);
            return true;
        }
        catch
        {
            return false;
        }
    }

    /// <summary>
    /// Loads a Piet program from an image file.
    /// Falls back to an internal PNG decoder when strict image decoding fails.
    /// </summary>
    public static PietProgram Parse(string path, int codelSize = 1)
    {
        var bytes = File.ReadAllBytes(path);
        // 拡張子が .txt の場合は ascii-piet 形式、.ppm の場合は PPM 形式として扱う
        var ext = Path.GetExtension(path).ToLowerInvariant();
        return Parse(bytes, ext, codelSize);
    }

    static bool TryParseFallbackPng(byte[] pngBytes, int codelSize,
#if NETSTANDARD2_1_OR_GREATER
    [NotNullWhen(true)]
#endif
    out PietProgram program)
    {
        program = default!;
        if (!TryDecodePng(pngBytes: pngBytes, out var width, out var height, out var codels))
            return false;

        program = InternalParseWithRawPngFallback(codels, width, height, codelSize);
        return true;
    }

    static PietProgram ParseWithRawPngFallback(byte[] pngBytes, int codelSize = 1)
    {
        if (!TryDecodePng(pngBytes, out var width, out var height, out var codels))
            throw new InvalidImageContentException("Failed to parse Piet PNG image.");

        return InternalParseWithRawPngFallback(codels, width, height, codelSize);
    }
    static PietProgram InternalParseWithRawPngFallback(byte[] codels, int width, int height, int codelSize)
    {
        int codelWidth = width / codelSize;
        int codelHeight = height / codelSize;
        var colors = new PietColor[codelWidth * codelHeight];
        for (var y = 0; y < codelHeight; y++)
        {
            for (var x = 0; x < codelWidth; x++)
            {
                colors[(y * codelWidth) + x] = (PietColor)codels[y * codelSize * width + (x * codelSize)];
            }
        }
        return new PietProgram(codelWidth, codelHeight, colors);
    }

    static bool TryDecodePng(byte[] pngBytes, out int width, out int height,
#if NETSTANDARD2_1_OR_GREATER
    [NotNullWhen(true)]
#endif
    out byte[] bytes)
    {
        width = 0;
        height = 0;
        bytes = default!;

        if (pngBytes.Length < 8)
            return false;

        if (pngBytes[0] != 0x89 || pngBytes[1] != 0x50 || pngBytes[2] != 0x4E ||
            pngBytes[3] != 0x47 || pngBytes[4] != 0x0D || pngBytes[5] != 0x0A ||
            pngBytes[6] != 0x1A || pngBytes[7] != 0x0A)
            return false;

        var pos = 8;
        var colorType = 2;
        var idatData = new List<byte>();

        while (pos + 8 <= pngBytes.Length)
        {
            var chunkLength = ReadInt32BE(pngBytes, pos);
            pos += 4;
            if (pos + 4 > pngBytes.Length)
                break;

            var t0 = pngBytes[pos];
            var t1 = pngBytes[pos + 1];
            var t2 = pngBytes[pos + 2];
            var t3 = pngBytes[pos + 3];
            pos += 4;

            var isIHDR = t0 == 73 && t1 == 72 && t2 == 68 && t3 == 82;
            var isIDAT = t0 == 73 && t1 == 68 && t2 == 65 && t3 == 84;
            var isIEND = t0 == 73 && t1 == 69 && t2 == 78 && t3 == 68;

            if (isIHDR && chunkLength >= 13)
            {
                if (pos + 13 > pngBytes.Length)
                    return false;

                width = ReadInt32BE(pngBytes, pos);
                height = ReadInt32BE(pngBytes, pos + 4);
                colorType = pngBytes[pos + 9];
            }
            else if (isIDAT)
            {
                if (pos + chunkLength > pngBytes.Length)
                    return false;

                for (var i = 0; i < chunkLength; i++)
                    idatData.Add(pngBytes[pos + i]);
            }
            else if (isIEND)
            {
                break;
            }

            pos += chunkLength + 4;
        }

        if (width <= 0 || height <= 0)
            return false;

        int bytesPerPixel;
        if (colorType == 2)
            bytesPerPixel = 3;
        else if (colorType == 6)
            bytesPerPixel = 4;
        else
            return false;

        if (idatData.Count < 3)
            return false;

        byte[] decompressed;
        try
        {
            var compressed = idatData.ToArray();
            using var ms = new MemoryStream(compressed, 2, compressed.Length - 2);
            using var ds = new DeflateStream(ms, CompressionMode.Decompress);
            using var outMs = new MemoryStream();
            ds.CopyTo(outMs);
            decompressed = outMs.ToArray();
        }
        catch
        {
            return false;
        }

        var stride = width * bytesPerPixel;
        var result = new byte[width * height];
        var prevRow = new byte[stride];
        var dataPos = 0;

        for (var y = 0; y < height; y++)
        {
            if (dataPos >= decompressed.Length)
                return false;

            var filterType = decompressed[dataPos++];
            if (dataPos + stride > decompressed.Length)
                return false;

            var row = new byte[stride];
            Array.Copy(decompressed, dataPos, row, 0, stride);
            dataPos += stride;

            ApplyPngFilter(filterType, row, prevRow, bytesPerPixel);

            for (var x = 0; x < width; x++)
            {
                var pp = x * bytesPerPixel;
                var colorIdx = MapToPietColor(row[pp], row[pp + 1], row[pp + 2]);
                if (colorIdx < 0)
                    return false;

                result[(y * width) + x] = (byte)colorIdx;
            }

            Array.Copy(row, prevRow, stride);
        }

        bytes = result;
        return true;
    }

    internal static byte[]? DecodePng(byte[] pngBytes, out int width, out int height)
    {
        if (TryDecodePng(pngBytes, out width, out height, out var bytes))
            return bytes;
        return null;
    }

    static int ReadInt32BE(byte[] data, int pos) =>
        (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];

    internal static void ApplyPngFilter(int filterType, byte[] row, byte[] prevRow, int bpp)
    {
        switch (filterType)
        {
            case 1:
                for (var i = bpp; i < row.Length; i++)
                    row[i] = (byte)(row[i] + row[i - bpp]);
                break;
            case 2:
                for (var i = 0; i < row.Length; i++)
                    row[i] = (byte)(row[i] + prevRow[i]);
                break;
            case 3:
                for (var i = 0; i < row.Length; i++)
                {
                    var a = i >= bpp ? row[i - bpp] : 0;
                    row[i] = (byte)(row[i] + (byte)((a + prevRow[i]) >> 1));
                }
                break;
            case 4:
                for (var i = 0; i < row.Length; i++)
                {
                    var a = i >= bpp ? row[i - bpp] : 0;
                    var b = prevRow[i];
                    var c = i >= bpp ? prevRow[i - bpp] : 0;
                    row[i] = (byte)(row[i] + PaethPredictor(a, b, c));
                }
                break;
        }
    }

    static int PaethPredictor(int a, int b, int c)
    {
        var p = a + b - c;
        var pa = p > a ? p - a : a - p;
        var pb = p > b ? p - b : b - p;
        var pc = p > c ? p - c : c - p;
        if (pa <= pb && pa <= pc)
            return a;
        if (pb <= pc)
            return b;
        return c;
    }

    /// <summary>
    /// Converts an RGB value to a PietColor index. Returns -1 for unsupported colors.
    /// </summary>
    public static int MapToPietColor(byte r, byte g, byte b)
    {
        if (r == 0x00 && g == 0x00 && b == 0x00) return 0;
        if (r == 0xFF && g == 0xFF && b == 0xFF) return 1;
        if (r == 0xFF && g == 0xC0 && b == 0xC0) return 2;
        if (r == 0xFF && g == 0x00 && b == 0x00) return 3;
        if (r == 0xC0 && g == 0x00 && b == 0x00) return 4;
        if (r == 0xFF && g == 0xFF && b == 0xC0) return 5;
        if (r == 0xFF && g == 0xFF && b == 0x00) return 6;
        if (r == 0xC0 && g == 0xC0 && b == 0x00) return 7;
        if (r == 0xC0 && g == 0xFF && b == 0xC0) return 8;
        if (r == 0x00 && g == 0xFF && b == 0x00) return 9;
        if (r == 0x00 && g == 0xC0 && b == 0x00) return 10;
        if (r == 0xC0 && g == 0xFF && b == 0xFF) return 11;
        if (r == 0x00 && g == 0xFF && b == 0xFF) return 12;
        if (r == 0x00 && g == 0xC0 && b == 0xC0) return 13;
        if (r == 0xC0 && g == 0xC0 && b == 0xFF) return 14;
        if (r == 0x00 && g == 0x00 && b == 0xFF) return 15;
        if (r == 0x00 && g == 0x00 && b == 0xC0) return 16;
        if (r == 0xFF && g == 0xC0 && b == 0xFF) return 17;
        if (r == 0xFF && g == 0x00 && b == 0xFF) return 18;
        if (r == 0xC0 && g == 0x00 && b == 0xC0) return 19;
        return -1;
    }

    internal static PietColor Normalize(Rgba32 color) => color switch
    {
        { R: 0x00, G: 0x00, B: 0x00 } => PietColor.Black,
        { R: 0xFF, G: 0xFF, B: 0xFF } => PietColor.White,
        { R: 0xFF, G: 0xC0, B: 0xC0 } => PietColor.LightRed,
        { R: 0xFF, G: 0x00, B: 0x00 } => PietColor.Red,
        { R: 0xC0, G: 0x00, B: 0x00 } => PietColor.DarkRed,
        { R: 0xFF, G: 0xFF, B: 0xC0 } => PietColor.LightYellow,
        { R: 0xFF, G: 0xFF, B: 0x00 } => PietColor.Yellow,
        { R: 0xC0, G: 0xC0, B: 0x00 } => PietColor.DarkYellow,
        { R: 0xC0, G: 0xFF, B: 0xC0 } => PietColor.LightGreen,
        { R: 0x00, G: 0xFF, B: 0x00 } => PietColor.Green,
        { R: 0x00, G: 0xC0, B: 0x00 } => PietColor.DarkGreen,
        { R: 0xC0, G: 0xFF, B: 0xFF } => PietColor.LightCyan,
        { R: 0x00, G: 0xFF, B: 0xFF } => PietColor.Cyan,
        { R: 0x00, G: 0xC0, B: 0xC0 } => PietColor.DarkCyan,
        { R: 0xC0, G: 0xC0, B: 0xFF } => PietColor.LightBlue,
        { R: 0x00, G: 0x00, B: 0xFF } => PietColor.Blue,
        { R: 0x00, G: 0x00, B: 0xC0 } => PietColor.DarkBlue,
        { R: 0xFF, G: 0xC0, B: 0xFF } => PietColor.LightMagenta,
        { R: 0xFF, G: 0x00, B: 0xFF } => PietColor.Magenta,
        { R: 0xC0, G: 0x00, B: 0xC0 } => PietColor.DarkMagenta,
        _ => throw new InvalidOperationException($"Unsupported Piet color: {color}")
    };
}
