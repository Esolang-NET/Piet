# Esolang.Piet.Processor

Execution engine primitives for Piet programs.

This package is intended to execute parsed Piet programs and host the core Piet runtime model.

## Install

```bash
dotnet add package Esolang.Piet.Processor
```

## Usage

```csharp
using Esolang.Piet.Parser;
using Esolang.Piet.Processor;

var program = PietParser.Parse("hello-world.png");
var processor = new PietProcessor(program);

processor.Run();
```

## Current Status

- `PietProcessor` executes parsed `PietProgram` instances.
- `Run()` supports `TextReader`/`TextWriter` I/O, and `RunAndOutputString()` returns captured output.

## Notes

- Use this package when you want to build on the execution model as it evolves.
- For image loading and normalization, use `Esolang.Piet.Parser`.
