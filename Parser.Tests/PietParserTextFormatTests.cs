using TUnit.Assertions.Enums;

namespace Esolang.Piet.Parser.Tests;

public sealed class PietParserTextFormatTests
{
    [Test]
    public async Task Format_AsciiPiet_WorksWithoutNewlines()
    {
        var program = new PietProgram(
            2,
            2,
            [
                PietColor.Red, PietColor.White,
                PietColor.Black, PietColor.DarkCyan,
            ]);

        var text = AsciiPietFormatter.Format(program);

        await Assert.That(text).IsEqualTo("l_ C");
    }

    [Test]
    public async Task Parse_AsciiPiet_Works(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.txt");
        try
        {
            await File.WriteAllLinesAsync(path,
            [
                "l_",
                " C"
            ], CancellationToken);
            var program = PietParser.Parse(path, cancellationToken: CancellationToken);
            await Assert.That(program.Width).IsEqualTo(2);
            await Assert.That(program.Height).IsEqualTo(2);
            await Assert.That(program.Codels)
                .IsEquivalentTo((PietColor[])[
                    PietColor.Red,
                    PietColor.White,
                    PietColor.Black,
                    PietColor.DarkCyan,
                ], CollectionOrdering.Matching);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }

    [Test]
    public async Task Format_ThenParse_AsciiPiet_RoundTrips()
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

        await Assert.That(parsed.Width).IsEqualTo(original.Width);
        await Assert.That(parsed.Height).IsEqualTo(original.Height);
        await Assert.That(parsed.Codels).IsEquivalentTo(original.Codels, CollectionOrdering.Matching);
    }

    [Test]
    public async Task Parse_PpmPiet_Works(CancellationToken CancellationToken)
    {
        var path = Path.Combine(Path.GetTempPath(), $"{Guid.NewGuid():N}.ppm");
        try
        {
            await File.WriteAllLinesAsync(path,
            [
                "P3",
                "2 2",
                "255",
                "255 0 0   255 255 255",
                "0 0 0     0 255 255"
            ], CancellationToken);
            var program = PietParser.Parse(path, cancellationToken: CancellationToken);
            await Assert.That(program.Width).IsEqualTo(2);
            await Assert.That(program.Height).IsEqualTo(2);
            await Assert.That(program.Codels).IsEquivalentTo((PietColor[])[
                    PietColor.Red, PietColor.White,
                    PietColor.Black, PietColor.Cyan
                ], CollectionOrdering.Matching);
        }
        finally
        {
            if (File.Exists(path)) File.Delete(path);
        }
    }
}
