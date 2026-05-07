# dotnet-piet

Command-line interpreter for Piet programs.

This package provides a .NET global tool entry point for running Piet image files from the command line.

## Install

```bash
dotnet tool install --global dotnet-piet
```

## Usage

```bash
dotnet-piet path/to/program.png
```

To convert a parsed program to ascii-piet text and write it to standard output without a trailing newline:

```bash
dotnet-piet path/to/program.png --ascii-piet
```

Or with a dedicated parse command:

```bash
dotnet-piet parse path/to/program.png
```

Or with the included launch profile in this repository:

```bash
dotnet run --project Interpreter/Esolang.Piet.Interpreter.csproj -f net10.0 --launch-profile Piet.HelloWorld
```

The tool parses the image file and forwards the resulting `PietProgram` to `Esolang.Piet.Processor`.

## Current Status

- Command-line argument parsing is wired up.
- `Esolang.Piet.Processor` is integrated and executes parsed programs.

## Notes

- Use this package when you want the command-line entry point for Piet programs.
- For compile-time method generation, see `Esolang.Piet.Generator`.
- For parsing APIs, see `Esolang.Piet.Parser`.

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
