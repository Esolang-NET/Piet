using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Reflection;
using System.Text;

namespace Esolang.Piet.Parser.Tests;

[TestClass]
public sealed class PietParserTests
{
    static readonly MethodInfo ApplyPngFilterMethod = typeof(PietParser)
        .GetMethod("ApplyPngFilter", BindingFlags.NonPublic | BindingFlags.Static)!;


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

    [TestMethod]
    public void Parse_FallsBackWhenPngChunkCrcIsInvalid()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0xFF, 0xFF, 0xFF);
                image.Save(path);
            }

            var bytes = File.ReadAllBytes(path);
            bytes[29] = 0x00;
            bytes[30] = 0x00;
            bytes[31] = 0x00;
            bytes[32] = 0x00;
            File.WriteAllBytes(path, bytes);

            var program = PietParser.Parse(path);

            Assert.AreEqual(1, program.Width);
            Assert.AreEqual(1, program.Height);
            CollectionAssert.AreEqual(new[] { PietColor.White }, program.Codels.ToArray());
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void Parse_ThrowsInvalidImageContent_ForNonPngData()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            File.WriteAllText(path, "not a png");

            _ = Assert.ThrowsExactly<InvalidImageContentException>(() => PietParser.Parse(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void Parse_FallbackThrowsForUnsupportedColor()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0x12, 0x34, 0x56);
                image.Save(path);
            }

            var bytes = File.ReadAllBytes(path);
            bytes[29] = 0x00;
            bytes[30] = 0x00;
            bytes[31] = 0x00;
            bytes[32] = 0x00;
            File.WriteAllBytes(path, bytes);

            _ = Assert.ThrowsExactly<InvalidImageContentException>(() => PietParser.Parse(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void TryDecodePng_ReturnsNullForInvalidSignatureAndUnsupportedColorType()
    {
        var invalidSig = new byte[] { 0x00, 0x11, 0x22 };
        var decoded = PietParser.DecodePng(invalidSig, out _, out _);
        Assert.IsNull(decoded);

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0xFF, 0xFF, 0xFF);
                image.Save(path);
            }

            var bytes = File.ReadAllBytes(path);
            bytes[25] = 3;
            decoded = PietParser.DecodePng(bytes, out _, out _);
            Assert.IsNull(decoded);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public void PrivateHelpers_ApplyPngFilterAndColorMap_Work()
    {
        var row = new byte[] { 1, 2, 3, 1, 1, 1 };
        var prev = new byte[] { 1, 1, 1, 1, 1, 1 };

        PietParser.ApplyPngFilter(1, row, prev, 3);
        CollectionAssert.AreEqual(new byte[] { 1, 2, 3, 2, 3, 4 }, row);

        row = new byte[] { 1, 1, 1 };
        PietParser.ApplyPngFilter(2, row, prev, 3);
        CollectionAssert.AreEqual(new byte[] { 2, 2, 2 }, row);

        row = new byte[] { 2, 2, 2 };
        PietParser.ApplyPngFilter(3, row, prev, 3);
        CollectionAssert.AreEqual(new byte[] { 2, 2, 2 }, row);

        row = new byte[] { 1, 1, 1 };
        PietParser.ApplyPngFilter(4, row, prev, 3);
        CollectionAssert.AreEqual(new byte[] { 2, 2, 2 }, row);

        var color = PietParser.MapToPietColor(0x12, 0x34, 0x56);
        Assert.AreEqual(-1, color);
    }

    [TestMethod]
    public void TryParse_AsciiPiet_ByExtension_Works()
    {
        var bytes = Encoding.ASCII.GetBytes("l_\n C");
        var ok = PietParser.TryParse(bytes, ".txt", 1, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(2, program.Width);
        Assert.AreEqual(2, program.Height);
    }

    [TestMethod]
    public void TryParse_AsciiPiet_ByContent_Works()
    {
        var bytes = Encoding.ASCII.GetBytes("l_\n C");
        var ok = PietParser.TryParse(bytes, ".dat", 1, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(2, program.Width);
    }

    [TestMethod]
    public void TryParse_PngImageSharp_Works()
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(255, 0, 0);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);

        var ok = PietParser.TryParse(ms.ToArray(), ".png", 1, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(PietColor.Red, program.Codels[0]);
    }

    [TestMethod]
    public void TryParse_PngFallback_Works()
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(255, 255, 255);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);

        var bytes = ms.ToArray();
        bytes[29] = bytes[30] = bytes[31] = bytes[32] = 0x00;

        var ok = PietParser.TryParse(bytes, ".png", 1, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(PietColor.White, program.Codels[0]);
    }

    [TestMethod]
    public void TryParse_Ppm_Works()
    {
        var text = """
            P3
            1 1
            255
            255 0 0
            """;
        var bytes = Encoding.ASCII.GetBytes(text);

        var ok = PietParser.TryParse(bytes, ".ppm", 1, out var program);

        Assert.IsTrue(ok);
        Assert.AreEqual(PietColor.Red, program.Codels[0]);
    }

    [TestMethod]
    public void TryParse_UnsupportedColor_ReturnsFalse()
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(0x12, 0x34, 0x56);
        using var ms = new MemoryStream();
        img.SaveAsPng(ms);

        var ok = PietParser.TryParse(ms.ToArray(), ".png", 1, out _);

        Assert.IsFalse(ok);
    }

    [TestMethod]
    public void TryParse_InvalidData_ReturnsFalse()
    {
        var bytes = Encoding.ASCII.GetBytes("not an image");
        var ok = PietParser.TryParse(bytes, ".png", 1, out _);

        Assert.IsFalse(ok);
    }
}
