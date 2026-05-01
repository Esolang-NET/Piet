# Esolang.Piet.Parser

Core parsing primitives for Piet programs.

This package loads Piet source images and normalizes them into an in-memory program model that can be consumed by processors and tools.

## Install

```bash
dotnet add package Esolang.Piet.Parser
```


## Usage

```csharp
using Esolang.Piet.Parser;

// Supports PNG, ascii-piet (.txt), and PPM (.ppm)
var program = PietParser.Parse("hello-world.png");
var program2 = PietParser.Parse("ascii-piet.txt");
var program3 = PietParser.Parse("sample.ppm");

Console.WriteLine($"Size: {program.Width} x {program.Height}");
Console.WriteLine($"Top-left codel: {program[0, 0]}");
```

## API Surface

- `PietParser.Parse(string path)` loads a Piet image file and returns a `PietProgram`.
- `PietProgram` exposes `Width`, `Height`, `Codels`, and an indexer for coordinate access.
- `PietColor` represents the normalized Piet palette including black, white, and the 18 command colors.


## Supported Formats

- PNG (standard Piet images)
- GIF (`.gif`)
- ascii-piet text format (`.txt`)
- Netpbm PPM (P3, `.ppm`)

Other common static image formats supported by ImageSharp (such as JPEG) can also be loaded.

Image format is detected automatically from the file extension.

## Notes

- Input is read from an image file path (PNG, .gif, .txt, .ppm and other ImageSharp-supported static formats)
- Unsupported colors or unknown ascii-piet chars cause parsing to fail.
- This package focuses on normalization, not execution.

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
- ascii-piet encoding specification: https://github.com/dloscutoff/ascii-piet#encoding-specification
