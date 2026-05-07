using Esolang.Piet.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace Esolang.Piet.Interpreter.Tests;

[TestClass]
public class ProgramTests
{
    [TestMethod]
    public async Task RunAsync_HelpOption_ReturnsZero()
    {
        var exitCode = await Program.RunAsync(["--help"]);
        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_WhitePixelImage_ReturnsZero()
    {
        // A 1×1 white-pixel image is a valid, immediately-terminating Piet program.
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(255, 255, 255);
                image.Save(path);
            }

            var exitCode = await Program.RunAsync([path]);
            Assert.AreEqual(0, exitCode);
        }
        finally
        {
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RunAsync_AsciiPietOption_WritesTextWithoutTrailingNewline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        var originalOutput = Console.Out;
        var writer = new StringWriter(new StringBuilder());
        try
        {
            using (var image = new Image<Rgba32>(2, 2))
            {
                image[0, 0] = new Rgba32(255, 0, 0);
                image[1, 0] = new Rgba32(255, 255, 255);
                image[0, 1] = new Rgba32(0, 0, 0);
                image[1, 1] = new Rgba32(0, 192, 192);
                image.Save(path);
            }

            Console.SetOut(writer);

            var exitCode = await Program.RunAsync([path, "--ascii-piet"]);

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual("l_ C", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path))
                File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RunAsync_ParseCommand_WritesTextWithoutTrailingNewline()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        var originalOutput = Console.Out;
        var writer = new StringWriter(new StringBuilder());
        try
        {
            using (var image = new Image<Rgba32>(2, 2))
            {
                image[0, 0] = new Rgba32(255, 0, 0);
                image[1, 0] = new Rgba32(255, 255, 255);
                image[0, 1] = new Rgba32(0, 0, 0);
                image[1, 1] = new Rgba32(0, 192, 192);
                image.Save(path);
            }

            Console.SetOut(writer);

            var exitCode = await Program.RunAsync(["parse", path]);

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual("l_ C", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path))
                File.Delete(path);
        }
    }
}
