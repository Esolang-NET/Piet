using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Esolang.Piet;

/// <summary>
/// Parses Piet source images into normalized program data.
/// </summary>
public static class PietParser
{
    /// <summary>
    /// Loads a Piet program from an image file.
    /// </summary>
    public static PietProgram Parse(string path)
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
