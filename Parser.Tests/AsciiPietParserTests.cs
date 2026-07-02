using System.Text;

namespace Esolang.Piet.Parser.Tests;

public sealed class AsciiPietParserTests
{
    [Test]
    public async Task AsciiPietParser_CanDownscale_WithCodelSize()
    {
        // 改行は無視されるので 1 行で書く
        var text = "lLll";
        var bytes = Encoding.ASCII.GetBytes(text);

        var ok = AsciiPietParser.TryParse(bytes, codelSize: 2, out var program);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Width).IsEqualTo(1);
        await Assert.That(program.Height).IsEqualTo(1);
        await Assert.That(program.Codels.Single()).IsEqualTo(PietColor.Red);
    }

    [Test]
    public async Task AsciiPietParser_Throws_ForEmpty()
    {
        var bytes = Array.Empty<byte>();
        Assert.Throws<InvalidDataException>(() => AsciiPietParser.Parse(bytes));
    }

    [Test]
    public void AsciiPietParser_Throws_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("lXl"); // X は不正
        Assert.Throws<InvalidDataException>(() => AsciiPietParser.Parse(bytes));
    }

    [Test]
    public void AsciiPietParser_Throws_ForInvalidCodelSize()
    {
        var bytes = Encoding.ASCII.GetBytes("ll");
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiPietParser.Parse(bytes, 0));
    }

    [Test]
    public async Task AsciiPietParser_TryParse_ReturnsFalse_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("lXl");
        var ok = AsciiPietParser.TryParse(bytes, 1, out _);
        await Assert.That(ok).IsFalse();
    }
}

public sealed class AsciiPietFormatterTests
{
    [Test]
    public async Task Format_TrailingBlackTrimmed()
    {
        // Row: [Red, Black, Black] — trailing Blacks trimmed → "L" (Red EOL)
        var program = new PietProgram(3, 1, [PietColor.Red, PietColor.Black, PietColor.Black]);
        var result = AsciiPietFormatter.Format(program);
        await Assert.That(result).IsEqualTo("L");
    }

    [Test]
    public async Task Format_AllBlackRow_EmitsAtSign()
    {
        // All-Black row → '@' (Black EOL)
        var program = new PietProgram(2, 1, [PietColor.Black, PietColor.Black]);
        var result = AsciiPietFormatter.Format(program);
        await Assert.That(result).IsEqualTo("@");
    }

    [Test]
    public async Task Format_NonTrailingBlackPreserved()
    {
        // Row: [Black, Red] — Black is not trailing → ' ' + 'L'
        var program = new PietProgram(2, 1, [PietColor.Black, PietColor.Red]);
        var result = AsciiPietFormatter.Format(program);
        await Assert.That(result).IsEqualTo(" L");
    }

    [Test]
    public async Task Format_TrailingBlack_RoundTrips()
    {
        // Format trims trailing Blacks, so parsed width shrinks to last non-Black.
        // [Red, Black, Black] → "L" → parsed as width=1 [Red].
        var original = new PietProgram(3, 1, [PietColor.Red, PietColor.Black, PietColor.Black]);
        var text = AsciiPietFormatter.Format(original);
        var parsed = AsciiPietParser.Parse(Encoding.ASCII.GetBytes(text));
        await Assert.That(parsed.Width).IsEqualTo(1);
        await Assert.That(parsed.Height).IsEqualTo(1);
        await Assert.That(parsed.Codels).HasItemAt(0, PietColor.Red);
    }
}
