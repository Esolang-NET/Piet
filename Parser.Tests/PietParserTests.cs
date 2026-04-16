using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

namespace Esolang.Piet.Tests;

[TestClass]
public sealed class PietParserTests
{
    [TestMethod]
    public void Parse_ReturnsNormalizedProgram()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(2, 2))
            {
                image[0, 0] = new Rgba32(0xFF, 0x00, 0x00);
                image[1, 0] = new Rgba32(0xFF, 0xFF, 0xFF);
                image[0, 1] = new Rgba32(0x00, 0x00, 0x00);
                image[1, 1] = new Rgba32(0x00, 0xFF, 0xFF);
                image.Save(path);
            }

            var program = PietParser.Parse(path);

            Assert.AreEqual(2, program.Width);
            Assert.AreEqual(2, program.Height);
            CollectionAssert.AreEqual(
                new[]
                {
                    PietColor.Red,
                    PietColor.White,
                    PietColor.Black,
                    PietColor.Cyan,
                },
                program.Codels.ToArray());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void Parse_ThrowsForUnsupportedColor()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0x12, 0x34, 0x56);
                image.Save(path);
            }

            _ = Assert.ThrowsExactly<InvalidOperationException>(() => PietParser.Parse(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
