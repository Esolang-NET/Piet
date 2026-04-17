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

The tool parses the image file and forwards the resulting `PietProgram` to `Esolang.Piet.Processor`.

## Current Status

- Command-line argument parsing is wired up.
- The underlying processor is not implemented yet, so execution is not complete today.

## Notes

- Use this package when you want the command-line entry point for Piet programs.
- For compile-time method generation, see `Esolang.Piet.Generator`.
- For parsing APIs, see `Esolang.Piet.Parser`.
