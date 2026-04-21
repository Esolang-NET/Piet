namespace Esolang.Piet.Parser.Tests;

[TestClass]
public sealed class PietParserTextFormatTests
{
    [TestMethod]
    public void Parse_AsciiPiet_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllLines(path, new[]
            {
                "l_",
                " C"
            });
            var program = PietParser.Parse(path);
            Assert.AreEqual(2, program.Width);
            Assert.AreEqual(2, program.Height);
            CollectionAssert.AreEqual(
                new[]
                {
                    PietColor.Red, PietColor.White,
                    PietColor.Black, PietColor.DarkCyan
                },
                program.Codels.ToArray());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public void Parse_PpmPiet_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ppm");
        try
        {
            File.WriteAllLines(path, new[]
            {
                "P3",
                "2 2",
                "255",
                "255 0 0   255 255 255",
                "0 0 0     0 255 255"
            });
            var program = PietParser.Parse(path);
            Assert.AreEqual(2, program.Width);
            Assert.AreEqual(2, program.Height);
            CollectionAssert.AreEqual(
                new[]
                {
                    PietColor.Red, PietColor.White,
                    PietColor.Black, PietColor.Cyan
                },
                program.Codels.ToArray());
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
