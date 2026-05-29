namespace Esolang.Piet.Parser.Tests;

[TestClass]
public sealed class PietParserTextFormatTests
{
    [TestMethod]
    public void Format_AsciiPiet_WorksWithoutNewlines()
    {
        var program = new PietProgram(
            2,
            2,
            [
                PietColor.Red, PietColor.White,
                PietColor.Black, PietColor.DarkCyan,
            ]);

        var text = AsciiPietFormatter.Format(program);

        Assert.AreEqual("l_ C", text);
    }

    [TestMethod]
    public void Parse_AsciiPiet_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        try
        {
            File.WriteAllLines(path,
            [
                "l_",
                " C"
            ]);
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
    public void Format_ThenParse_AsciiPiet_RoundTrips()
    {
        var original = new PietProgram(
            3,
            2,
            [
                PietColor.LightRed, PietColor.Yellow, PietColor.DarkBlue,
                PietColor.Black, PietColor.White, PietColor.LightCyan,
            ]);

        var bytes = System.Text.Encoding.ASCII.GetBytes(AsciiPietFormatter.Format(original));

        var parsed = AsciiPietParser.Parse(bytes);

        Assert.AreEqual(original.Width, parsed.Width);
        Assert.AreEqual(original.Height, parsed.Height);
        CollectionAssert.AreEqual(original.Codels.ToArray(), parsed.Codels.ToArray());
    }

    [TestMethod]
    public void Parse_PpmPiet_Works()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ppm");
        try
        {
            File.WriteAllLines(path,
            [
                "P3",
                "2 2",
                "255",
                "255 0 0   255 255 255",
                "0 0 0     0 255 255"
            ]);
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
