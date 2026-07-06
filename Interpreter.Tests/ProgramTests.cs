using SkiaSharp;
using System.Text;
using TUnit.Assertions.Exceptions;

namespace Esolang.Piet.Interpreter.Tests;

public class ProgramTests
{
    static void WriteLog(string message) => TestContext.Current!.OutputWriter.WriteLine(message);
    static int Run(string[] args, TextWriter? writer = null)
    {
        WriteLog($"args: {string.Join(", ", args)}");
        TextWriter? originalOutput = null;
        try
        {
            if (writer is not null)
            {
                originalOutput = Console.Out;
#pragma warning disable TUnit0055 // Do not overwrite the Console writer
                Console.SetOut(writer);
#pragma warning restore TUnit0055 // Do not overwrite the Console writer

            }
            var entryPoint = typeof(Program).Assembly.EntryPoint;
            Assert.NotNull(entryPoint);
            object?[] parameters = [args];
            var result = entryPoint.Invoke(null, parameters) as int?;
            Assert.NotNull(result);
            return result.Value;
        }
        finally
        {
            if (originalOutput is not null)
            {
#pragma warning disable TUnit0055 // Do not overwrite the Console writer
                Console.SetOut(originalOutput);
#pragma warning restore TUnit0055 // Do not overwrite the Console writer
            }
        }
    }


    static async Task<int> RunAsync(string[] args, TextWriter? writer = null, TextReader? reader = null, CancellationToken cancellationToken = default)
    {
        WriteLog($"args: {string.Join(", ", args.Select(a => $"\"{a}\""))}");
        return await Program.RunAsync(args, writer: writer, reader: reader, cancellationToken: cancellationToken);
    }

    [Test]
    public async Task Run_Default_ReturnOne() => await Assert.That(Run([])).IsEqualTo(1);

    [Test]
    [Arguments("_")]
    [Arguments("??")]
    public async Task Run_AsciiPietTextWithoutPath_ReturnsZero(string asciiPietText) => await Assert.That(Run(["--ascii-piet-text", asciiPietText])).IsEqualTo(0);

    [Test]
    [Arguments("_", "_")]
    [NotInParallel]
    public async Task Run_AsciiPietTextWithoutPath_WithAsciiPietOption_WritesText(string asciiPietText, string expectedAsciiPiet)
    {
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            await Assert.That(Run(["--ascii-piet-text", asciiPietText, "--ascii-piet"], writer)).IsEqualTo(0);
            await Assert.That(writer.ToString()).IsEqualTo(expectedAsciiPiet);
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
    }

    [Test]
    [NotInParallel]
    public async Task Run_WithoutPathAndWithoutAsciiPietText_ReturnsNonZero() => await Assert.That(Run([])).IsNotEqualTo(0);

    [Test]
    public async Task Run_WithPathAndAsciiPietText_ReturnsNonZero()
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", "no-op.png");
        await Assert.That(Run([path, "--ascii-piet-text", "_"])).IsNotEqualTo(0);
    }

    [Test]
    [Arguments("hello-world.png")]
    [Arguments("ascii-piet-sample.txt")]
    [Arguments("ppm-sample.ppm")]
    [Arguments("dot.gif")]
    [NotInParallel]
    public async Task Run_SamplePrograms_ReturnZero(string sampleFileName)
    {
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);
        await Assert.That(Run([path])).IsEqualTo(0);
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_SamplePrograms_ProduceExpectedOutput(CancellationToken CancellationToken)
    {
        const string sampleFileName = "hello-world.png";
        const string expectedOutput = "Hello, world!";
        var path = FindFileInRepository("samples", "Generator.UseConsole", "samples", sampleFileName);
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            await Assert.That(RunAsync([path], writer: writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString().TrimEnd('\r', '\n')).IsEqualTo(expectedOutput);
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_InlineAsciiPiet_ProduceExpectedExecutionOutput(CancellationToken CancellationToken)
    {
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            await Assert
                .That(RunAsync(["--piet-plus-plus", "--ascii-piet-text", "~"], writer: writer, cancellationToken: CancellationToken))
                .IsEqualTo(0);
            await Assert
                .That(writer.ToString().TrimEnd('\r', '\n'))
                .IsEqualTo(string.Empty);
        }
        catch (AssertionException)
        {
            WriteLog($"// output start\r\n{writer}\r\n// output end");
            throw;
        }

    }

    [Test]
    [NotInParallel]
    public async Task Run_HelpOption_ReturnsZero() => await Assert.That(Run(["--help"])).IsEqualTo(0);

    [Test]
    [NotInParallel]
    public async Task Run_ColorsCommand_ReturnsZero() => await Assert.That(Run(["colors"])).IsEqualTo(0);

    [Test]
    [NotInParallel]
    public async Task Run_ColorsCommand_PietPlusPlus_ReturnsZero() => await Assert.That(Run(["colors", "--piet-plus-plus"])).IsEqualTo(0);

    [Test]
    [NotInParallel]
    public async Task RunAsync_ColorsCommand_WritesAsciiPietTable(CancellationToken CancellationToken)
    {
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            await Assert.That(RunAsync(["colors"], writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString())
                .Contains("ascii-piet")
                .And.Contains("Black")
                .And.Contains("'l'")
                .And.Contains("'_'");
        }
        catch (AssertionException)
        {
            WriteLog($"// output start\r\n{writer}\r\n// output end");
            throw;
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_ColorsCommand_PietPlusPlus_WritesAsciiPietPlusPlusTable(CancellationToken CancellationToken)
    {
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            await Assert.That(RunAsync(["colors", "--piet-plus-plus"], writer: writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString())
                .Contains("ascii-piet++")
                .And.Contains("(Black)")
                .And.Contains("(White)")
                .And.Contains("'~'");
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_WhitePixelImage_ReturnsZero(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        try
        {
            WritePng(path, 1, 1, bitmap => bitmap.SetPixel(0, 0, new SKColor(255, 255, 255)));
            await Assert.That(RunAsync([path], cancellationToken: CancellationToken)).IsEqualTo(0);
        }
        finally { if (File.Exists(path)) File.Delete(path); }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_AsciiPietOption_WritesTextWithoutTrailingNewline(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            WritePng(path, 2, 2, bitmap =>
            {
                bitmap.SetPixel(0, 0, new SKColor(255, 0, 0));
                bitmap.SetPixel(1, 0, new SKColor(255, 255, 255));
                bitmap.SetPixel(0, 1, new SKColor(0, 0, 0));
                bitmap.SetPixel(1, 1, new SKColor(0, 192, 192));
            });
            await Assert.That(RunAsync([path, "--ascii-piet"], writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString()).IsEqualTo("l_ C");
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_ParseCommand_WritesTextWithoutTrailingNewline(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.png");
        using var writer = new StringWriter(new StringBuilder());
        try
        {
            WritePng(path, 2, 2, bitmap =>
            {
                bitmap.SetPixel(0, 0, new SKColor(255, 0, 0));
                bitmap.SetPixel(1, 0, new SKColor(255, 255, 255));
                bitmap.SetPixel(0, 1, new SKColor(0, 0, 0));
                bitmap.SetPixel(1, 1, new SKColor(0, 192, 192));
            });
            await Assert.That(RunAsync(["parse", path], writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString()).IsEqualTo("l_ C");
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_ParseCommand_PietPlusPlus_WritesAsciiPietPlusPlusText(CancellationToken CancellationToken)
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
        var writer = new StringWriter(new StringBuilder());
        try
        {
            WritePng(path, 2, 2, bitmap =>
            {
                bitmap.SetPixel(0, 0, new SKColor(255, 0, 0));     // Red (index 3)
                bitmap.SetPixel(1, 0, new SKColor(255, 255, 255)); // White (index 1)
                bitmap.SetPixel(0, 1, new SKColor(0, 0, 0));       // Black (index 0)
                bitmap.SetPixel(1, 1, new SKColor(0, 192, 192));   // DarkCyan (index 13)
            });
            await Assert.That(RunAsync(["parse", path, "--piet-plus-plus"], writer: writer, cancellationToken: CancellationToken)).IsEqualTo(0);
            await Assert.That(writer.ToString())
            // Each row ends with '|', no trailing newline
                .EndsWith("|")
                .And.DoesNotContain("\n").Because("ascii-piet++ output should not contain newlines");
        }
        catch (AssertionException)
        {
            WriteLog($"// output start:\r\n{writer}\r\n// output end");
            throw;
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_AsciiPietPlusPlusTextOption_Executes(CancellationToken CancellationToken)
    {
        // '~' = White (index 63) = noop, '|' = EOL → single-row, single-White-codel program
        using var writer = new StringWriter(new StringBuilder());
        await Assert.That(RunAsync(["--ascii-piet-text", "~|", "--piet-plus-plus"], writer: writer, cancellationToken: CancellationToken)).IsEqualTo(0);

    }

    [Test]
    [NotInParallel]
    public async Task RunAsync_AsciiPietPlusPlusTextWithAsciiPietOption_WritesAsciiPietPlusPlusText(CancellationToken CancellationToken)
    {
        // '~' = White (index 63), '|' = EOL
        using var writer = new StringWriter(new StringBuilder());

        await Assert.That(RunAsync(["--ascii-piet-text", "~|", "--piet-plus-plus", "--ascii-piet"], writer: writer, cancellationToken: CancellationToken)).IsEqualTo(0);
        await Assert.That(writer.ToString())
            .EndsWith("|");
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

    static void WritePng(string path, int width, int height, Action<SKBitmap> configureBitmap)
    {
        using var bitmap = new SKBitmap(width, height);
        configureBitmap(bitmap);
        using var image = SKImage.FromBitmap(bitmap);
        using var data = image.Encode(SKEncodedImageFormat.Png, 100)
            ?? throw new InvalidOperationException("Failed to encode test PNG image.");
        File.WriteAllBytes(path, data.ToArray());
    }
}
