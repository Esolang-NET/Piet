using System.Text;
using TUnit.Assertions.Enums;

namespace Esolang.Piet.Parser.Tests;

public sealed class AsciiPietPlusPlusParserTests
{
    // Color index helpers: ' '=0, '0'=1...'9'=10, 'a'=11...'z'=36, 'A'=37...'Z'=62, '~'=63
    // Piet++ color 0 = Black (RGB 0,0,0), color 63 = White (RGB 0xFF,0xFF,0xFF)

    [Test]
    public async Task Parse_SingleRow_NoTrailingEol()
    {
        // ' ' = index 0 (Black), '0' = index 1, 'a' = index 11
        var bytes = Encoding.ASCII.GetBytes(" 0a");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Width).IsEqualTo(3);
        await Assert.That(program.Height).IsEqualTo(1);
        await Assert.That(program.Codels)
            .IsEquivalentTo((PietColor[])[
                 0, (PietColor)1, (PietColor)11
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Parse_MultipleRows_WithEolMarker()
    {
        // '0'=1, '1'=2 | 'a'=11, 'b'=12 |
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Width).IsEqualTo(2);
        await Assert.That(program.Height).IsEqualTo(2);
        await Assert.That(program.Codels)
            .IsEquivalentTo((PietColor[])[
                 (PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Parse_MultipleRows_WithAlternateEolMarker()
    {
        var bytes = Encoding.ASCII.GetBytes("01@ab@");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Width).IsEqualTo(2);
        await Assert.That(program.Height).IsEqualTo(2);
        await Assert.That(program.Codels)
            .IsEquivalentTo((PietColor[])[
                 (PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Parse_IgnoresActualNewlines()
    {
        var bytes = Encoding.ASCII.GetBytes("01|\r\nab|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Width).IsEqualTo(2);
        await Assert.That(program.Height).IsEqualTo(2);
        await Assert.That(program.Codels)
            .IsEquivalentTo((PietColor[])[
                 (PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12
            ], CollectionOrdering.Matching);
    }

    [Test]
    public async Task Parse_SpaceIsBlack_TildeIsWhite()
    {
        var bytes = Encoding.ASCII.GetBytes(" ~|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Width).IsEqualTo(2);
        await Assert.That(program.Height).IsEqualTo(1);
        await Assert.That(program.Codels).HasItemAt(0, 0);   // Black
        await Assert.That(program.Codels).HasItemAt(1, (PietColor)63);  // White
    }

    [Test]
    public async Task Parse_UppercaseLetters_MappedCorrectly()
    {
        // 'A'=37, 'Z'=62
        var bytes = Encoding.ASCII.GetBytes("AZ|");
        var program = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(program.Codels).HasItemAt(0, (PietColor)37);
        await Assert.That(program.Codels).HasItemAt(1, (PietColor)62);
    }

    [Test]
    public async Task Parse_WithCodelSize_Downscales()
    {
        // 2x2 grid of color 1 ('0'), codelSize=2 → 1x1
        var bytes = Encoding.ASCII.GetBytes("00|00|");
        var program = AsciiPietPlusPlusParser.Parse(bytes, codelSize: 2);

        await Assert.That(program.Width).IsEqualTo(1);
        await Assert.That(program.Height).IsEqualTo(1);
        await Assert.That(program.Codels.Single()).IsEqualTo((PietColor)1);
    }

    [Test]
    public void Parse_Throws_ForEmpty()
        => Assert.Throws<InvalidDataException>(() => AsciiPietPlusPlusParser.Parse([]));

    [Test]
    public void Parse_Throws_ForInvalidCodelSize()
    {
        var bytes = Encoding.ASCII.GetBytes("0|");
        Assert.Throws<ArgumentOutOfRangeException>(() => AsciiPietPlusPlusParser.Parse(bytes, 0));
    }

    [Test]
    public void Parse_Throws_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("0!|");  // '!' is not a valid ascii-piet++ char
        Assert.Throws<InvalidDataException>(() => AsciiPietPlusPlusParser.Parse(bytes));
    }

    [Test]
    public async Task TryParse_ReturnsFalse_ForEmpty()
    {
        var ok = AsciiPietPlusPlusParser.TryParse([], 1, out _);
        await Assert.That(ok).IsFalse();
    }

    [Test]
    public async Task TryParse_ReturnsFalse_ForInvalidChar()
    {
        var bytes = Encoding.ASCII.GetBytes("0!|");  // '!' is not a valid ascii-piet++ char
        var ok = AsciiPietPlusPlusParser.TryParse(bytes, 1, out _);
        await Assert.That(ok).IsFalse();
    }

    [Test]
    public void Parse_Throws_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        Assert.Throws<OperationCanceledException>(() => AsciiPietPlusPlusParser.Parse(bytes, cancellationToken: cts.Token));
    }

    [Test]
    public void TryParse_Throws_WhenCancelled()
    {
        using var cts = new CancellationTokenSource();
        cts.Cancel();
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        Assert.Throws<OperationCanceledException>(() => AsciiPietPlusPlusParser.TryParse(bytes, 1, out _, cts.Token));
    }

    [Test]
    public async Task LooksLikeAsciiPietPlusPlus_ReturnsFalse_ForBinaryData()
    {
        var binaryBytes = new byte[] { 0x89, 0x50, 0x4E, 0x47 }; // PNG header
        await Assert.That(AsciiPietPlusPlusParser.LooksLikeAsciiPietPlusPlus(binaryBytes)).IsFalse();
    }

    [Test]
    public async Task LooksLikeAsciiPietPlusPlus_ReturnsTrue_ForValidContent()
    {
        var bytes = Encoding.ASCII.GetBytes("01|ab|");
        await Assert.That(AsciiPietPlusPlusParser.LooksLikeAsciiPietPlusPlus(bytes)).IsTrue();
    }

    [Test]
    public async Task LooksLikeAsciiPietPlusPlus_ReturnsTrue_ForAlternateEolMarker()
    {
        var bytes = Encoding.ASCII.GetBytes("01@ab@");
        await Assert.That(AsciiPietPlusPlusParser.LooksLikeAsciiPietPlusPlus(bytes)).IsTrue();
    }
}

public sealed class AsciiPietPlusPlusFormatterTests
{
    [Test]
    public async Task Format_SingleRow_ProducesCorrectString()
    {
        // color 1 ('0'), color 11 ('a'), color 63 ('~')
        var program = new PietProgram(3, 1, [(PietColor)1, (PietColor)11, (PietColor)63]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo("0a~|");
    }

    [Test]
    public async Task Format_MultipleRows_EachRowEndsWithPipe()
    {
        var program = new PietProgram(2, 2, [(PietColor)1, (PietColor)2, (PietColor)11, (PietColor)12]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo("01|ab|");
    }

    [Test]
    public async Task Format_BlackAndWhite()
    {
        var program = new PietProgram(2, 1, [(PietColor)0, (PietColor)63]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo(" ~|");
    }

    [Test]
    public async Task Format_TrailingBlackTrimmed()
    {
        // Row: [1, 0, 0] — trailing Blacks should be trimmed → "0|"
        var program = new PietProgram(3, 1, [(PietColor)1, (PietColor)0, (PietColor)0]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo("0|");
    }

    [Test]
    public async Task Format_AllBlackRow_EmitsJustPipe()
    {
        // Row: [0, 0] — all Black → "|"
        var program = new PietProgram(2, 1, [(PietColor)0, (PietColor)0]);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo("|");
    }

    [Test]
    public async Task Format_TrailingBlack_RoundTrips()
    {
        // Format trims trailing Blacks, so parsed width shrinks to last non-Black.
        // [1, 0, 0] → "0|" → parsed as width=1 [(PietColor)1].
        var original = new PietProgram(3, 1, [(PietColor)1, (PietColor)0, (PietColor)0]);
        var text = AsciiPietPlusPlusFormatter.Format(original);
        var parsed = AsciiPietPlusPlusParser.Parse(Encoding.ASCII.GetBytes(text));
        await Assert.That(parsed.Width).IsEqualTo(1);
        await Assert.That(parsed.Height).IsEqualTo(1);
        await Assert.That(parsed.Codels[0]).IsEqualTo((PietColor)1);
    }

    [Test]
    public void Format_Throws_ForNull()
        => Assert.Throws<ArgumentNullException>(() => AsciiPietPlusPlusFormatter.Format(null!));

    [Test]
    public async Task Format_Empty_ReturnsEmptyString()
    {
        var program = new PietProgram(0, 0, []);
        var result = AsciiPietPlusPlusFormatter.Format(program);
        await Assert.That(result).IsEqualTo(string.Empty);
    }

    [Test]
    public async Task Format_ThenParse_RoundTrips()
    {
        var original = new PietProgram(3, 2,
        [
            (PietColor)0,  (PietColor)1,  (PietColor)10,
            (PietColor)11, (PietColor)36, (PietColor)63,
        ]);

        var text = AsciiPietPlusPlusFormatter.Format(original);
        var bytes = Encoding.ASCII.GetBytes(text);
        var parsed = AsciiPietPlusPlusParser.Parse(bytes);

        await Assert.That(parsed.Width).IsEqualTo(original.Width);
        await Assert.That(parsed.Height).IsEqualTo(original.Height);
        await Assert.That(parsed.Codels).IsEquivalentTo(original.Codels, CollectionOrdering.Matching);
    }
}
