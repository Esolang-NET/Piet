# Piet

.NET libraries and tools for the Piet esoteric programming language.

## Packages

- `Esolang.Piet.Generator`: generates C# method implementations from Piet source images at compile time.
- `Esolang.Piet.Parser`: parses Piet source images into a normalized in-memory model.
- `Esolang.Piet.Processor`: executes Piet programs.
- `dotnet-piet`: .NET global tool for running Piet image files.

## Generator Guide

For detailed generator signatures and usage patterns, see:

- [Generator README](./Generator/README.md)

For runnable examples and sample image usage, see:

- [UseConsole sample README](./samples/Generator.UseConsole/README.md)

## Status

Core flow is implemented end-to-end:

- `Esolang.Piet.Parser` parses and normalizes Piet images.
- `Esolang.Piet.Processor` executes parsed programs.
- `dotnet-piet` runs Piet image files from the command line.

Current work is focused on stabilization, diagnostics, and release readiness.