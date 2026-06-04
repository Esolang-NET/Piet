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
    public void Run_Default_ReturnOne() => Assert.AreEqual(1, Run([]));

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
            Assert.AreEqual(0, await Program.RunAsync([path], CancellationToken));
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
            Assert.AreEqual(0, await Program.RunAsync(["--ascii-piet-text", "_"], CancellationToken));
            Assert.AreEqual(string.Empty, writer.ToString().TrimEnd('\r', '\n'));
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public void Run_HelpOption_ReturnsZero() => Assert.AreEqual(0, Run(["--help"]));

    [TestMethod]
    public void Run_ColorsCommand_ReturnsZero() => Assert.AreEqual(0, Run(["colors"]));

    [TestMethod]
    public void Run_ColorsCommand_PietPlusPlus_ReturnsZero() => Assert.AreEqual(0, Run(["colors", "--piet-plus-plus"]));

    [TestMethod]
    public async Task RunAsync_ColorsCommand_WritesAsciiPietTable()
    {
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["colors"], CancellationToken));
            var output = writer.ToString();
            Assert.Contains("ascii-piet", output);
            Assert.Contains("Black", output);
            Assert.Contains("'l'", output);
            Assert.Contains("'_'", output);
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public async Task RunAsync_ColorsCommand_PietPlusPlus_WritesAsciiPietPlusPlusTable()
    {
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["colors", "--piet-plus-plus"], CancellationToken));
            var output = writer.ToString();
            Assert.Contains("ascii-piet++", output);
            Assert.Contains("(Black)", output);
            Assert.Contains("(White)", output);
            Assert.Contains("'~'", output);
        }
        finally { Console.SetOut(originalOutput); }
    }

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
            Assert.AreEqual(0, await Program.RunAsync([path], CancellationToken));
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
            Assert.AreEqual(0, await Program.RunAsync([path, "--ascii-piet"], CancellationToken));
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
            Assert.AreEqual(0, await Program.RunAsync(["parse", path], CancellationToken));
            Assert.AreEqual("l_ C", writer.ToString());
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RunAsync_ParseCommand_PietPlusPlus_WritesAsciiPietPlusPlusText()
    {
        // Red(index=3), White(index=1), Black(index=0), DarkCyan(index=13)
        // ascii-piet++: row0 -> "3~|" (Red='3', White='~', EOL='|'), but trailing Black trimming applies per row
        // row0: Red='3', White='~' → "3~|"
        // row1: Black=' ', DarkCyan char = index 13 → 'w' ... wait, let me recalculate
        // PietColor indices: Black=0, White=1, LightRed=2, Red=3, DarkRed=4, LightYellow=5, Yellow=6, DarkYellow=7,
        //   LightGreen=8, Green=9, DarkGreen=10, LightCyan=11, Cyan=12, DarkCyan=13, LightBlue=14, Blue=15, DarkBlue=16,
        //   LightMagenta=17, Magenta=18, DarkMagenta=19
        // ascii-piet++ char for index 3 (Red): '0'+2 = '2'? No: '0'=1...'9'=10, so index 3 = '0'+2 = '2'
        // Wait: 0='space', 1='0', 2='1', 3='2', 4='3', ... so Red(3) = '2', White(1) = '0'
        // DarkCyan(13) = 11+'b'=12? No: 11='a', 12='b', 13='c'
        // row0: Red(3)='2', White(1)='0' → "20|"
        // row1: Black(0)=' ' (trimmed as trailing), DarkCyan(13)='c' → but Black is at x=0, DarkCyan at x=1
        //   lastNonBlack = 1, so x=0 outputs ' ', x=1 outputs 'c' → " c|"
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        var originalOutput = Console.Out;
        var writer = new StringWriter(new StringBuilder());
        try
        {
            using (var image = new Image<Rgba32>(2, 2))
            {
                image[0, 0] = new Rgba32(255, 0, 0);     // Red (index 3)
                image[1, 0] = new Rgba32(255, 255, 255); // White (index 1)
                image[0, 1] = new Rgba32(0, 0, 0);       // Black (index 0)
                image[1, 1] = new Rgba32(0, 192, 192);   // DarkCyan (index 13)
                image.Save(path);
            }
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["parse", path, "--piet-plus-plus"], CancellationToken));
            var output = writer.ToString();
            // Each row ends with '|', no trailing newline
            Assert.EndsWith("|", output);
            Assert.IsFalse(output.Contains('\n'), "ascii-piet++ output should not contain newlines");
        }
        finally
        {
            Console.SetOut(originalOutput);
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [TestMethod]
    public async Task RunAsync_AsciiPietPlusPlusTextOption_Executes()
    {
        // '~' = White (index 63) = noop, '|' = EOL → single-row, single-White-codel program
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["--ascii-piet-text", "~|", "--piet-plus-plus"], CancellationToken));
        }
        finally { Console.SetOut(originalOutput); }
    }

    [TestMethod]
    public async Task RunAsync_AsciiPietPlusPlusTextWithAsciiPietOption_WritesAsciiPietPlusPlusText()
    {
        // '~' = White (index 63), '|' = EOL
        var originalOutput = Console.Out;
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            Console.SetOut(writer);
            Assert.AreEqual(0, await Program.RunAsync(["--ascii-piet-text", "~|", "--piet-plus-plus", "--ascii-piet"], CancellationToken));
            var output = writer.ToString();
            Assert.EndsWith("|", output);
        }
        finally { Console.SetOut(originalOutput); }
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
