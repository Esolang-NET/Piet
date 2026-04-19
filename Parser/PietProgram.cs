namespace Esolang.Piet.Parser;

/// <summary>
/// Represents a parsed Piet program as a 2D codel map.
/// </summary>
/// <summary>
/// Represents a parsed Piet program as a 2D codel map.
/// </summary>
public sealed class PietProgram
{
    /// <summary>
    /// Initializes a Piet program instance.
    /// </summary>
    public PietProgram(int width, int height, IReadOnlyList<PietColor> codels)
    {
        Width = width;
        Height = height;
        Codels = codels;
    }

    /// <summary>
    /// Program width in codels.
    /// </summary>
    public int Width { get; }

    /// <summary>
    /// Program height in codels.
    /// </summary>
    public int Height { get; }

    /// <summary>
    /// Flattened codel buffer in row-major order.
    /// </summary>
    public IReadOnlyList<PietColor> Codels { get; }

    /// <summary>
    /// Returns the codel at the specified coordinates.
    /// </summary>
    public PietColor this[int x, int y] => Codels[(y * Width) + x];
}
