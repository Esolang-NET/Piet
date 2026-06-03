using System.Text;

namespace Esolang.Piet.Parser.Tests;

[TestClass]
public sealed class AsciiPietParserTests
{
    [TestMethod]
    public void AsciiPietParser_CanDownscale_WithCodelSize()
    {
        // 改行は無視されるので 1 行で書く
        var text = "lLll";
        var bytes = Encoding.ASCII.GetBytes(text);

        var ok = AsciiPietParser.TryParse(bytes, codelSize: 2, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(1, program.Width);
        Assert.AreEqual(1, program.Height);
        Assert.AreEqual(PietColor.Red, program.Codels.Single());
    }

    [TestMethod]
    public void AsciiPietParser_Throws_ForEmpty()
    {
        var bytes = Array.Empty<byte>();
        Assert.ThrowsExactly<InvalidDataException>(() => AsciiPietParser.Parse(bytes));
    }

    [TestMethod]
    public void AsciiPietParser_Throws_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("lXl"); // X は不正
        Assert.ThrowsExactly<InvalidDataException>(() => AsciiPietParser.Parse(bytes));
    }

    [TestMethod]
    public void AsciiPietParser_Throws_ForInvalidCodelSize()
    {
        var bytes = Encoding.ASCII.GetBytes("ll");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AsciiPietParser.Parse(bytes, 0));
    }

    [TestMethod]
    public void AsciiPietParser_TryParse_ReturnsFalse_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("lXl");
        var ok = AsciiPietParser.TryParse(bytes, 1, out _);
        Assert.IsFalse(ok);
    }
}

[TestClass]
public sealed class AsciiPietFormatterTests
{
    [TestMethod]
    public void Format_TrailingBlackTrimmed()
    {
        // Row: [Red, Black, Black] — trailing Blacks trimmed → "L" (Red EOL)
        var program = new PietProgram(3, 1, [PietColor.Red, PietColor.Black, PietColor.Black]);
        var result = AsciiPietFormatter.Format(program);
        Assert.AreEqual("L", result);
    }

    [TestMethod]
    public void Format_AllBlackRow_EmitsAtSign()
    {
        // All-Black row → '@' (Black EOL)
        var program = new PietProgram(2, 1, [PietColor.Black, PietColor.Black]);
        var result = AsciiPietFormatter.Format(program);
        Assert.AreEqual("@", result);
    }

    [TestMethod]
    public void Format_NonTrailingBlackPreserved()
    {
        // Row: [Black, Red] — Black is not trailing → ' ' + 'L'
        var program = new PietProgram(2, 1, [PietColor.Black, PietColor.Red]);
        var result = AsciiPietFormatter.Format(program);
        Assert.AreEqual(" L", result);
    }

    [TestMethod]
    public void Format_TrailingBlack_RoundTrips()
    {
        // Format trims trailing Blacks, so parsed width shrinks to last non-Black.
        // [Red, Black, Black] → "L" → parsed as width=1 [Red].
        var original = new PietProgram(3, 1, [PietColor.Red, PietColor.Black, PietColor.Black]);
        var text = AsciiPietFormatter.Format(original);
        var parsed = AsciiPietParser.Parse(Encoding.ASCII.GetBytes(text));
        Assert.AreEqual(1, parsed.Width);
        Assert.AreEqual(1, parsed.Height);
        Assert.AreEqual(PietColor.Red, parsed.Codels[0]);
    }
}
