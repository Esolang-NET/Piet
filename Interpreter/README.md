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

You can also run inline ascii-piet text directly without providing a path:

```bash
dotnet-piet --ascii-piet-text "l_ C"
```

To convert a parsed program to ascii-piet text and write it to standard output without a trailing newline:

```bash
dotnet-piet path/to/program.png --ascii-piet
```

This also works with inline ascii-piet text:

```bash
dotnet-piet --ascii-piet-text "l_ C" --ascii-piet
```

Or with a dedicated parse command:

```bash
dotnet-piet parse path/to/program.png
```

To output in ascii-piet++ format instead of ascii-piet:

```bash
dotnet-piet parse path/to/program.png --piet-plus-plus
dotnet-piet path/to/program.png --ascii-piet --piet-plus-plus
```

To run an inline ascii-piet++ program directly:

```bash
dotnet-piet --ascii-piet-text "~|" --piet-plus-plus
```

In ascii-piet++, row separators can be either `|` or `@`. Actual newline characters (`\r`, `\n`) are ignored while parsing.

To show the ascii-piet character encoding table:

```bash
dotnet-piet colors
```

To show the ascii-piet++ character encoding table:

```bash
dotnet-piet colors --piet-plus-plus
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
