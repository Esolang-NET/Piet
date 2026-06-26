using System.Text;

namespace Esolang.Piet.Parser;

/// <summary>
/// Formats a <see cref="PietProgram"/> as ascii-piet++ text for the Piet++ language.
/// </summary>
/// <remarks>
/// Each row of codels is written as color characters followed by the
/// <see cref="AsciiPietPlusPlusParser.EolMarker"/> (<c>'|'</c>).
/// No actual newline characters are inserted.
/// Supported file extension: <c>.appp</c>, <c>.txt2</c>.
/// </remarks>
public static class AsciiPietPlusPlusFormatter
{
    static readonly char[] IndexToChar = BuildIndexToChar();

    static char[] BuildIndexToChar()
    {
        var chars = new char[64];
        chars[0] = ' ';
        for (var i = 0; i < 10; i++) chars[1 + i] = (char)('0' + i);
        for (var i = 0; i < 26; i++) chars[11 + i] = (char)('a' + i);
        for (var i = 0; i < 26; i++) chars[37 + i] = (char)('A' + i);
        chars[63] = '~';
        return chars;
    }

    /// <summary>
    /// Converts a Piet++ program to ascii-piet++ text.
    /// Each row ends with <c>'|'</c>; no newline characters are inserted.
    /// </summary>
    /// <param name="program">The Piet++ program to format.</param>
    /// <returns>An ascii-piet++ string representation of the program.</returns>
    public static string Format(PietProgram program)
    {
        if (program is null)
            throw new ArgumentNullException(nameof(program));
        if (program.Width == 0 || program.Height == 0)
            return string.Empty;
        if (program.Codels.Count != program.Width * program.Height)
            throw new ArgumentException("Codel count does not match program dimensions.", nameof(program));

        var sb = new StringBuilder(program.Width * program.Height + program.Height);
        for (var y = 0; y < program.Height; y++)
        {
            // Find the last non-Black (non-zero) codel in the row to enable trailing-Black trimming.
            var lastNonBlack = -1;
            for (var x = program.Width - 1; x >= 0; x--)
            {
                if ((int)program[x, y] != 0) { lastNonBlack = x; break; }
            }
            for (var x = 0; x <= lastNonBlack; x++)
            {
                var idx = (int)program[x, y];
                if ((uint)idx >= 64u)
                    throw new ArgumentOutOfRangeException(nameof(program), $"Color index {idx} is outside Piet++ range 0–63.");
                sb.Append(IndexToChar[idx]);
            }
            sb.Append(AsciiPietPlusPlusParser.EolMarker);
        }
        return sb.ToString();
    }
}
