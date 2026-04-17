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

This repository is in early bootstrap state. The package layout is in place, and the next implementation steps are parser normalization, processor execution semantics, and CLI integration.