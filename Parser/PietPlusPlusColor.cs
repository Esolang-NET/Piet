namespace Esolang.Piet.Parser;

/// <summary>
/// Provides color mapping utilities for the Piet++ language variant.
/// </summary>
public static class PietPlusPlusColorMapper
{
    static int MapChannel(byte v) => v switch
    {
        0x00 => 0,
        0x55 => 1,
        0xAA => 2,
        0xFF => 3,
        _ => -1,
    };

    /// <summary>
    /// Maps an RGB triplet to a Piet++ color index (0–63), or -1 if any channel is not a valid Piet++ value.
    /// </summary>
    public static int MapToPietPlusPlusColor(byte r, byte g, byte b)
    {
        var ri = MapChannel(r);
        var gi = MapChannel(g);
        var bi = MapChannel(b);
        if (ri < 0 || gi < 0 || bi < 0) return -1;
        return ri * 16 + gi * 4 + bi;
    }

    /// <summary>
    /// Converts a Piet++ color index (0–63) back to an RGB triplet.
    /// </summary>
    public static (byte r, byte g, byte b) PietPlusPlusColorToRgb(int color)
    {
        var ri = (color >> 4) & 3;
        var gi = (color >> 2) & 3;
        var bi = color & 3;
        return (ChannelToByte(ri), ChannelToByte(gi), ChannelToByte(bi));
    }

    static byte ChannelToByte(int v) => v switch
    {
        0 => 0x00,
        1 => 0x55,
        2 => 0xAA,
        _ => 0xFF,
    };
}
