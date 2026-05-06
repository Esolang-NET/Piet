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
