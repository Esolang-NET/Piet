using SixLabors.ImageSharp;
using SixLabors.ImageSharp.PixelFormats;
using System.Text;

namespace Esolang.Piet.Interpreter.Tests;

[TestClass]
public class ProgramTests(TestContext TestContext)
{
#pragma warning disable MSTEST0054
    CancellationToken CancellationToken => TestContext.CancellationTokenSource.Token;
#pragma warning restore MSTEST0054
    static int Run(string[] args)
    {
        var entryPoint = typeof(Program).Assembly.EntryPoint;
        Assert.IsNotNull(entryPoint);
        object?[] parameters = [args];
        var result = entryPoint.Invoke(null, parameters) as int?;
        Assert.IsNotNull(result);
        return result.Value;
    }

    [TestMethod]
    public void Run_Default_ReturnsZero() => Assert.AreEqual(0, Run([]));

    [TestMethod]
    [DataRow("_")]
    [DataRow("??")]
    public void Run_AsciiPietTextWithoutPath_ReturnsZero(string asciiPietText) => Assert.AreEqual(0, Run(["--ascii-piet-text", asciiPietText]));

    [TestMethod]
    [DataRow("_", "_")]
    public void Run_AsciiPietTextWithoutPath_WithAsciiPietOption_WritesText(string asciiPietText, string expectedAsciiPiet)
    {
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, Run(["--ascii-piet-text", asciiPietText, "--ascii-piet"]));
            Assert.AreEqual(expectedAsciiPiet, writer.ToString());
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public void Run_WithoutPathAndWithoutAsciiPietText_ReturnsNonZero() => Assert.AreNotEqual(0, Run([]));

    [TestMethod]
    public void Run_WithPathAndAsciiPietText_ReturnsNonZero()
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", "no-op.png");
        Assert.AreNotEqual(0, Run([path, "--ascii-piet-text", "_"]));
    }

    [TestMethod]
    [DataRow("hello-world.png")]
    [DataRow("ascii-piet-sample.txt")]
    [DataRow("ppm-sample.ppm")]
    [DataRow("dot.gif")]
    public void Run_SamplePrograms_ReturnZero(string sampleFileName)
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);
        Assert.AreEqual(0, Run([path]), $"Expected success exit code for sample '{sampleFileName}'.");
    }

    [TestMethod]
    public async Task RunAsync_SamplePrograms_ProduceExpectedOutput()
    {
        const string sampleFileName = "hello-world.png";
        const string expectedOutput = "Hello, world!";
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync([path]));
            Assert.AreEqual(expectedOutput, writer.ToString().TrimEnd('\r', '\n'));
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public async Task RunAsync_InlineAsciiPiet_ProduceExpectedExecutionOutput()
    {
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["--ascii-piet-text", "_"]));
            Assert.AreEqual(string.Empty, writer.ToString().TrimEnd('\r', '\n'));
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public void Run_HelpOption_ReturnsNonZero() => Assert.AreNotEqual(0, Run(["--help"]));

    [TestMethod]
    public async Task RunAsync_WhitePixelImage_ReturnsZero()
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            using (var image = new Image<Rgba32>(1, 1))
            {
                image[0, 0] = new Rgba32(255, 255, 255);
                image.Save(path);
            }
            Assert.AreEqual(0, await Program.RunAsync([path]));
        }
        finally { if (File.Exists(path)) File.Delete(path); }
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
            Assert.AreEqual(0, await Program.RunAsync([path, "--ascii-piet"]));
            Assert.AreEqual("l_ C", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path)) File.Delete(path);
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
            Assert.AreEqual(0, await Program.RunAsync(["parse", path]));
            Assert.AreEqual("l_ C", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    static string FindFileInRepository(params string[] relativeParts)
    {
        var directory = new DirectoryInfo(AppContext.BaseDirectory);
        while (directory is not null)
        {
            var candidate = Path.Combine(directory.FullName, Path.Combine(relativeParts));
            if (File.Exists(candidate)) return candidate;
            directory = directory.Parent;
        }
        throw new FileNotFoundException($"Could not find file: {Path.Combine(relativeParts)}");
    }
}
