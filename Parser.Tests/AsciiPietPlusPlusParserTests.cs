using System.Text;

namespace Esolang.Piet.Parser.Tests;

[TestClass]
public sealed class AsciiPietPlusPlusParserTests
{
    // Color index helpers: ' '=0, '0'=1...'9'=10, 'a'=11...'z'=36, 'A'=37...'Z'=62, '~'=63
    // Piet++ color 0 = Black (RGB 0,0,0), color 63 = White (RGB 0xFF,0xFF,0xFF)

    [TestMethod]
    public void Parse_SingleRow_NoTrailingEol()
    {
        // ' ' = index 0 (Black), '0' = index 1, 'a' = index 11
        var bytes = Encoding.ASCII.GetBytes(" 0a");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual(3, program.Width);
        Assert.AreEqual(1, program.Height);
        CollectionAssert.AreEqual(
            new PietColor[] { (PietColor)0, (PietColor)1, (PietColor)11 },
            program.Codels.ToArray());
    }

    [TestMethod]
    public void Parse_MultipleRows_WithEolMarker()
    {
        // '0'=1, '1'=2 | 'a'=11, 'b'=12 |
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual(2, program.Width);
        Assert.AreEqual(2, program.Height);
        CollectionAssert.AreEqual(
            new PietColor[] { (PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12 },
            program.Codels.ToArray());
    }

    [TestMethod]
    public void Parse_IgnoresActualNewlines()
    {
        var bytes = Encoding.ASCII.GetBytes("01|\r\nab|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual(2, program.Width);
        Assert.AreEqual(2, program.Height);
        CollectionAssert.AreEqual(
            new PietColor[] { (PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12 },
            program.Codels.ToArray());
    }

    [TestMethod]
    public void Parse_SpaceIsBlack_TildeIsWhite()
    {
        var bytes = Encoding.ASCII.GetBytes(" ~|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual(2, program.Width);
        Assert.AreEqual(1, program.Height);
        Assert.AreEqual((PietColor)0, program.Codels[0]);   // Black
        Assert.AreEqual((PietColor)63, program.Codels[1]);  // White
    }

    [TestMethod]
    public void Parse_UppercaseLetters_MappedCorrectly()
    {
        // 'A'=37, 'Z'=62
        var bytes = Encoding.ASCII.GetBytes("AZ|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual((PietColor)37, program.Codels[0]);
        Assert.AreEqual((PietColor)62, program.Codels[1]);
    }

    [TestMethod]
    public void Parse_WithCodelSize_Downscales()
    {
        // 2x2 grid of color 1 ('0'), codelSize=2 → 1x1
        var bytes = Encoding.ASCII.GetBytes("00|00|");
        var program = AsciiPietPlusPlusParser.Parse(bytes, codelSize: 2);

        Assert.AreEqual(1, program.Width);
        Assert.AreEqual(1, program.Height);
        Assert.AreEqual((PietColor)1, program.Codels.Single());
    }

    [TestMethod]
    public void Parse_Throws_ForEmpty()
        => Assert.ThrowsExactly<InvalidDataException>(() => AsciiPietPlusPlusParser.Parse([]));

    [TestMethod]
    public void Parse_Throws_ForInvalidCodelSize()
    {
        var bytes = Encoding.ASCII.GetBytes("0|");
        Assert.ThrowsExactly<ArgumentOutOfRangeException>(() => AsciiPietPlusPlusParser.Parse(bytes, 0));
    }

    [TestMethod]
    public void Parse_Throws_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("0!|");  // '!' is not a valid ascii-piet2 char
        Assert.ThrowsExactly<InvalidDataException>(() => AsciiPietPlusPlusParser.Parse(bytes));
    }

    [TestMethod]
    public void TryParse_ReturnsFalse_ForEmpty()
    {
        var ok = AsciiPietPlusPlusParser.TryParse([], 1, out _);
        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryParse_ReturnsFalse_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("0!|");  // '!' is not a valid ascii-piet2 char
        var ok = AsciiPietPlusPlusParser.TryParse(bytes, 1, out _);
        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void LooksLikeAsciiPietPlusPlus_ReturnsFalse_ForBinaryData()
    {
        var binaryBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        Assert.IsFalse(AsciiPietPlusPlusParser.LooksLikeAsciiPietPlusPlus(binaryBytes));
    }

    [TestMethod]
    public void LooksLikeAsciiPietPlusPlus_ReturnsTrue_ForValidContent()
    {
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        Assert.IsTrue(AsciiPietPlusPlusParser.LooksLikeAsciiPietPlusPlus(bytes));
    }
}

[TestClass]
public sealed class AsciiPietPlusPlusFormatterTests
{
    [TestMethod]
    public void Format_SingleRow_ProducesCorrectString()
    {
        // color 1 ('0'), color 11 ('a'), color 63 ('~')
        var program = new PietProgram(3, 1, [(PietColor)1, (PietColor)11, (PietColor)63]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        Assert.AreEqual("0a~|", result);
    }

    [TestMethod]
    public void Format_MultipleRows_EachRowEndsWithPipe()
    {
        var program = new PietProgram(2, 2, [(PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        Assert.AreEqual("01|ab|", result);
    }

    [TestMethod]
    public void Format_BlackAndWhite()
    {
        var program = new PietProgram(2, 1, [(PietColor)0, (PietColor)63]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        Assert.AreEqual(" ~|", result);
    }

    [TestMethod]
    public void Format_Throws_ForNull()
        => Assert.ThrowsExactly<ArgumentNullException>(() => AsciiPietPlusPlusFormatter.Format(null!));

    [TestMethod]
    public void Format_Empty_ReturnsEmptyString()
    {
        var program = new PietProgram(0, 0, []);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        Assert.AreEqual(string.Empty, result);
    }

    [TestMethod]
    public void Format_ThenParse_RoundTrips()
    {
        var original = new PietProgram(3, 2,
        [
            (PietColor)0,  (PietColor)1,  (PietColor)10,
            (PietColor)11, (PietColor)36, (PietColor)63,
        ]);

        var text = AsciiPietPlusPlusFormatter.Format(original);
        var bytes = Encoding.ASCII.GetBytes(text);
        var parsed = AsciiPietPlusPlusParser.Parse(bytes);

        Assert.AreEqual(original.Width, parsed.Width);
        Assert.AreEqual(original.Height, parsed.Height);
        CollectionAssert.AreEqual(original.Codels.ToArray(), parsed.Codels.ToArray());
    }
}
