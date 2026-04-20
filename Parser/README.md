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

var program = PietParser.Parse("hello-world.png");

Console.WriteLine($"Size: {program.Width} x {program.Height}");
Console.WriteLine($"Top-left codel: {program[0, 0]}");
```

## API Surface

- `PietParser.Parse(string path)` loads a Piet image file and returns a `PietProgram`.
- `PietProgram` exposes `Width`, `Height`, `Codels`, and an indexer for coordinate access.
- `PietColor` represents the normalized Piet palette including black, white, and the 18 command colors.

## Notes

- Input is read from an image file path.
- Unsupported colors cause parsing to fail.
- This package focuses on normalization, not execution.

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
