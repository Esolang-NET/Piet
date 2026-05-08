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
    public async Task RunAsync_AsciiPietTextWithoutPath_ReturnsZero()
    {
        var exitCode = await Program.RunAsync(["--ascii-piet-text", "_"]);

        Assert.AreEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_AsciiPietTextWithoutPath_WithAsciiPietOption_WritesText()
    {
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);

            var exitCode = await Program.RunAsync(["--ascii-piet-text", "_", "--ascii-piet"]);

            Assert.AreEqual(0, exitCode);
            Assert.AreEqual("_", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

    [TestMethod]
    public async Task RunAsync_WithoutPathAndWithoutAsciiPietText_ReturnsNonZero()
    {
        var exitCode = await Program.RunAsync(Array.Empty<string>());

        Assert.AreNotEqual(0, exitCode);
    }

    [TestMethod]
    public async Task RunAsync_WithPathAndAsciiPietText_ReturnsNonZero()
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", "no-op.png");

        var exitCode = await Program.RunAsync([path, "--ascii-piet-text", "_"]);

        Assert.AreNotEqual(0, exitCode);
    }

    [TestMethod]
    [DataRow("hello-world.png")]
    [DataRow("no-op.png")]
    [DataRow("ascii-piet-sample.txt")]
    [DataRow("ppm-sample.ppm")]
    [DataRow("dot.gif")]
    [DataRow("dot-codel-11.gif")]
    public async Task RunAsync_SamplePrograms_ReturnZero(string sampleFileName)
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);

        var exitCode = await Program.RunAsync([path]);

        Assert.AreEqual(0, exitCode, $"Expected success exit code for sample '{sampleFileName}'.");
    }

    [TestMethod]
    [DataRow("hello-world.png", "Hello, world!")]
    [DataRow("no-op.png", "")]
    public async Task RunAsync_SamplePrograms_ProduceExpectedOutput(string sampleFileName, string expectedOutput)
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);

            var exitCode = await Program.RunAsync([path]);
            var actual = writer.ToString().TrimEnd('\r', '\n');

            Assert.AreEqual(0, exitCode, $"Expected success exit code for sample '{sampleFileName}'.");
            Assert.AreEqual(expectedOutput, actual);
        }
        finally
        {
            Console.SetOut(originalOutput);
        }
    }

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

    static string FindFileInRepository(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, Path.Combine(relativeParts));
            if (File.Exists(candidate))
                return candidate;

            directory = directory.Parent;
        }

        throw new FileNotFoundException($"Could not find file in repository: {Path.Combine(relativeParts)}");
    }
}
