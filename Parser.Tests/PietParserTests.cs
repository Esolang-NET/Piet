using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;
using TUnit.Assertions.Enums;

namespace Esolang.Piet.Parser.Tests;

public sealed class PietParserTests
{
    [Test]
    public async Task Parse_ReturnsNormalizedProgram()
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

            var program = PietParser.Parse(path, cancellationToken: CancellationToken.None);

            await Assert.That(program.Width).IsEqualTo(2);
            await Assert.That(program.Height).IsEqualTo(2);
            await Assert.That(program.Codels)
                .IsEquivalentTo((PietColor[])[
                    PietColor.Red,
                    PietColor.White,
                    PietColor.Black,
                    PietColor.Cyan,
                ], CollectionOrdering.Matching);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
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

            Assert.Throws<InvalidOperationException>(() => PietParser.Parse(path));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Parse_FallsBackWhenPngChunkCrcIsInvalid(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0xFF, 0xFF, 0xFF);
                await image.SaveAsync(path, CancellationToken);
            }

            var bytes = File.ReadAllBytes(path);
            bytes[29] = 0x00;
            bytes[30] = 0x00;
            bytes[31] = 0x00;
            bytes[32] = 0x00;
            File.WriteAllBytes(path, bytes);

            var program = PietParser.Parse(path, cancellationToken: CancellationToken);

            await Assert.That(program.Width).IsEqualTo(1);
            await Assert.That(program.Height).IsEqualTo(1);
            await Assert.That(program.Codels)
                .IsEquivalentTo((PietColor[])[PietColor.White], CollectionOrdering.Matching);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Parse_ThrowsInvalidImageContent_ForNonPngData(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            await File.WriteAllTextAsync(path, "not a png", CancellationToken);

            Assert.Throws<InvalidImageContentException>(() => PietParser.Parse(path, cancellationToken: CancellationToken));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task Parse_FallbackThrowsForUnsupportedColor(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0x12, 0x34, 0x56);
                await image.SaveAsync(path, CancellationToken);
            }

            var bytes = await File.ReadAllBytesAsync(path, CancellationToken);
            bytes[29] = 0x00;
            bytes[30] = 0x00;
            bytes[31] = 0x00;
            bytes[32] = 0x00;
            await File.WriteAllBytesAsync(path, bytes, CancellationToken);

            Assert.Throws<InvalidImageContentException>(() => PietParser.Parse(path, cancellationToken: CancellationToken));
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task TryDecodePng_ReturnsNullForInvalidSignatureAndUnsupportedColorType(CancellationToken CancellationToken)
    {
        var invalidSig = new byte[] { 0x00, 0x11, 0x22 };
        var decoded = PietParser.DecodePng(invalidSig, out _, out _, CancellationToken);
        await Assert.That(decoded).IsNull();

        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(0xFF, 0xFF, 0xFF);
                await image.SaveAsync(path, CancellationToken);
            }

            var bytes = await File.ReadAllBytesAsync(path, CancellationToken);
            bytes[25] = 3;
            decoded = PietParser.DecodePng(bytes, out _, out _, CancellationToken);
            await Assert.That(decoded).IsNull();
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [Test]
    public async Task PrivateHelpers_ApplyPngFilterAndColorMap_Work()
    {
        var row = new byte[] { 1, 2, 3, 1, 1, 1 };
        var prev = new byte[] { 1, 1, 1, 1, 1, 1 };

        PietParser.ApplyPngFilter(1, row, prev, 3);
        await Assert.That(row).IsEquivalentTo((byte[])[1, 2, 3, 2, 3, 4]);

        row = [1, 1, 1];
        PietParser.ApplyPngFilter(2, row, prev, 3);
        await Assert.That(row).IsEquivalentTo((byte[])[2, 2, 2]);

        row = [2, 2, 2];
        PietParser.ApplyPngFilter(3, row, prev, 3);
        await Assert.That(row).IsEquivalentTo((byte[])[2, 2, 2]);

        row = [1, 1, 1];
        PietParser.ApplyPngFilter(4, row, prev, 3);
        await Assert.That(row).IsEquivalentTo((byte[])[2, 2, 2]);

        var color = PietParser.MapToPietColor(0x12, 0x34, 0x56);
        await Assert.That(color).IsEqualTo(-1);
    }

    [Test]
    public async Task TryParse_AsciiPiet_ByExtension_Works()
    {
        var bytes = Encoding.ASCII.GetBytes("l_\n C");
        var ok = PietParser.TryParse(bytes, ".txt", 1, out var program);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Width).IsEqualTo(2);
        await Assert.That(program.Height).IsEqualTo(2);
    }

    [Test]
    public async Task TryParse_AsciiPiet_ByContent_Works()
    {
        var bytes = Encoding.ASCII.GetBytes("l_\n C");
        var ok = PietParser.TryParse(bytes, ".dat", 1, out var program);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Width).IsEqualTo(2);
    }

    [Test]
    public async Task TryParse_PngImageSharp_Works(CancellationToken CancellationToken)
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(255, 0, 0);
        using var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms, CancellationToken);

        var ok = PietParser.TryParse(ms.ToArray(), ".png", 1, out var program, CancellationToken);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Codels[0]).IsEqualTo(PietColor.Red);
    }

    [Test]
    public async Task TryParse_PngFallback_Works(CancellationToken CancellationToken)
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(255, 255, 255);
        using var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms, CancellationToken);

        var bytes = ms.ToArray();
        bytes[29] = bytes[30] = bytes[31] = bytes[32] = 0x00;

        var ok = PietParser.TryParse(bytes, ".png", 1, out var program, CancellationToken);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Codels).HasItemAt(0, PietColor.White);
    }

    [Test]
    public async Task TryParse_Ppm_Works()
    {
        var text = """
            P3
            1 1
            255
            255 0 0
            """;
        var bytes = Encoding.ASCII.GetBytes(text);

        var ok = PietParser.TryParse(bytes, ".ppm", 1, out var program);

        await Assert.That(ok).IsTrue();
        await Assert.That(program.Codels[0]).IsEqualTo(PietColor.Red);
    }

    [Test]
    public async Task TryParse_UnsupportedColor_ReturnsFalse(CancellationToken CancellationToken)
    {
        using var img = new Image<Rgba32>(1, 1);
        img[0, 0] = new Rgba32(0x12, 0x34, 0x56);
        using var ms = new MemoryStream();
        await img.SaveAsPngAsync(ms, CancellationToken);

        var ok = PietParser.TryParse(ms.ToArray(), ".png", 1, out _, CancellationToken);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryParse_InvalidData_ReturnsFalse()
    {
        var bytes = Encoding.ASCII.GetBytes("not an image");
        var ok = PietParser.TryParse(bytes, ".png", 1, out _);

        await Assert.That(ok).IsFalse();
    }

    [Test]
    public void Parse_Path_Throws_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var path = FindSamplePath("hello-world.png");
        Assert.Throws<OperationCanceledException>(() => PietParser.Parse(path, cancellationToken: cts.Token));
    }

    [Test]
    public void TryParse_Throws_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var bytes = Encoding.ASCII.GetBytes("l_\n C");
        Assert.Throws<OperationCanceledException>(() => PietParser.TryParse(bytes, ".txt", 1, out _, cts.Token));
    }

    static string FindSamplePath(string fileName)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, "samples", "Generator.UseConsole", "samples", fileName);
            if (File.Exists(candidate))
                return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException(fileName);
    }
}
