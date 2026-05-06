namespace Esolang.Piet.Parser;

/// <summary>
/// Represents a parsed Piet program as a 2D codel map.
/// </summary>
/// <remarks>
/// Initializes a Piet program instance.
/// </remarks>
public sealed class PietProgram(int width, int height, IReadOnlyList<PietColor> codels)
{

    /// <summary>
    /// Program width in codels.
    /// </summary>
    public int Width { get; } = width;

    /// <summary>
    /// Program height in codels.
    /// </summary>
    public int Height { get; } = height;

    /// <summary>
    /// Flattened codel buffer in row-major order.
    /// </summary>
    public IReadOnlyList<PietColor> Codels { get; } = codels;

    /// <summary>
    /// Returns the codel at the specified coordinates.
    /// </summary>
    public PietColor this[int x, int y] => Codels[(y * Width) + x];
}
