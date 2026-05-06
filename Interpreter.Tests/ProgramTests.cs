using Esolang.Piet.Interpreter;
using Microsoft.VisualStudio.TestTools.UnitTesting;
using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;

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
}
