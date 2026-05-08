using System.Text;

namespace Esolang.Piet.Parser;

/// <summary>
/// Formats a <see cref="PietProgram"/> as ascii-piet text.
/// </summary>
public static class AsciiPietFormatter
{
    private static readonly Dictionary<PietColor, (char Regular, char EndOfLine)> ColorToChar = new()
    {
        { PietColor.Black, (' ', '@') },
        { PietColor.DarkBlue, ('a', 'A') },
        { PietColor.DarkGreen, ('b', 'B') },
        { PietColor.DarkCyan, ('c', 'C') },
        { PietColor.DarkRed, ('d', 'D') },
        { PietColor.DarkMagenta, ('e', 'E') },
        { PietColor.DarkYellow, ('f', 'F') },
        { PietColor.Blue, ('i', 'I') },
        { PietColor.Green, ('j', 'J') },
        { PietColor.Cyan, ('k', 'K') },
        { PietColor.Red, ('l', 'L') },
        { PietColor.Magenta, ('m', 'M') },
        { PietColor.Yellow, ('n', 'N') },
        { PietColor.LightBlue, ('q', 'Q') },
        { PietColor.LightGreen, ('r', 'R') },
        { PietColor.LightCyan, ('s', 'S') },
        { PietColor.LightRed, ('t', 'T') },
        { PietColor.LightMagenta, ('u', 'U') },
        { PietColor.LightYellow, ('v', 'V') },
        { PietColor.White, ('?', '_') },
    };

    /// <summary>
    /// Converts a Piet program to ascii-piet text without inserting newline characters.
    /// </summary>
    public static string Format(PietProgram program)
    {
        if (program is null)
            throw new ArgumentNullException(nameof(program));

        if (program.Width == 0 || program.Height == 0)
            return string.Empty;

        var expectedCount = program.Width * program.Height;
        if (program.Codels.Count != expectedCount)
            throw new ArgumentException("The program codel count does not match its dimensions.", nameof(program));

        var builder = new StringBuilder(expectedCount);
        for (var y = 0; y < program.Height; y++)
        {
            for (var x = 0; x < program.Width; x++)
            {
                var codel = program[x, y];
                if (!ColorToChar.TryGetValue(codel, out var chars))
                    throw new ArgumentOutOfRangeException(nameof(program), $"Unsupported Piet color: {codel}.");

                builder.Append(x == program.Width - 1 ? chars.EndOfLine : chars.Regular);
            }
        }

        return builder.ToString();
    }
}
