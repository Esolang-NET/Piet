using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Collections.Generic;
using System.IO.Compression;

namespace Esolang.Piet.Parser;

/// <summary>
/// Parses Piet source images into normalized program data.
/// </summary>
public static class PietParser
{
    /// <summary>
    /// Loads a Piet program from an image file.
    /// Falls back to an internal PNG decoder when strict image decoding fails.
    /// </summary>
    public static PietProgram Parse(string path)
    {
        try
        {
            using var image = Image.Load<Rgba32>(path);
            var colors = new PietColor[image.Width * image.Height];

            for (var y = 0; y < image.Height; y++)
            {
                for (var x = 0; x < image.Width; x++)
                {
                    colors[(y * image.Width) + x] = Normalize(image[x, y]);
                }
            }

            return new PietProgram(image.Width, image.Height, colors);
        }
        catch (InvalidImageContentException)
        {
            return ParseWithRawPngFallback(path);
        }
        catch (UnknownImageFormatException)
        {
            return ParseWithRawPngFallback(path);
        }
    }

    static PietProgram ParseWithRawPngFallback(string path)
    {
        var pngBytes = File.ReadAllBytes(path);
        var codels = TryDecodePng(pngBytes, out var width, out var height);
        if (codels is null || width <= 0 || height <= 0)
            throw new InvalidImageContentException("Failed to parse Piet PNG image.");

        var colors = new PietColor[codels.Length];
        for (var i = 0; i < codels.Length; i++)
            colors[i] = (PietColor)codels[i];

        return new PietProgram(width, height, colors);
    }

    static byte[]? TryDecodePng(byte[] pngBytes, out int width, out int height)
    {
        width = 0;
        height = 0;

        if (pngBytes.Length < 8)
            return null;

        if (pngBytes[0] != 0x89 || pngBytes[1] != 0x50 || pngBytes[2] != 0x4E ||
            pngBytes[3] != 0x47 || pngBytes[4] != 0x0D || pngBytes[5] != 0x0A ||
            pngBytes[6] != 0x1A || pngBytes[7] != 0x0A)
            return null;

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
                    return null;

                width = ReadInt32BE(pngBytes, pos);
                height = ReadInt32BE(pngBytes, pos + 4);
                colorType = pngBytes[pos + 9];
            }
            else if (isIDAT)
            {
                if (pos + chunkLength > pngBytes.Length)
                    return null;

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
            return null;

        int bytesPerPixel;
        if (colorType == 2)
            bytesPerPixel = 3;
        else if (colorType == 6)
            bytesPerPixel = 4;
        else
            return null;

        if (idatData.Count < 3)
            return null;

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
            return null;
        }

        var stride = width * bytesPerPixel;
        var result = new byte[width * height];
        var prevRow = new byte[stride];
        var dataPos = 0;

        for (var y = 0; y < height; y++)
        {
            if (dataPos >= decompressed.Length)
                return null;

            var filterType = decompressed[dataPos++];
            if (dataPos + stride > decompressed.Length)
                return null;

            var row = new byte[stride];
            Array.Copy(decompressed, dataPos, row, 0, stride);
            dataPos += stride;

            ApplyPngFilter(filterType, row, prevRow, bytesPerPixel);

            for (var x = 0; x < width; x++)
            {
                var pp = x * bytesPerPixel;
                var colorIdx = MapToPietColor(row[pp], row[pp + 1], row[pp + 2]);
                if (colorIdx < 0)
                    return null;

                result[(y * width) + x] = (byte)colorIdx;
            }

            Array.Copy(row, prevRow, stride);
        }

        return result;
    }

    static int ReadInt32BE(byte[] data, int pos) =>
        (data[pos] << 24) | (data[pos + 1] << 16) | (data[pos + 2] << 8) | data[pos + 3];

    static void ApplyPngFilter(int filterType, byte[] row, byte[] prevRow, int bpp)
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

    static int MapToPietColor(byte r, byte g, byte b)
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
