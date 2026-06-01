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
using Esolang.Processor;

var program = PietParser.Parse("hello-world.png");
var processor = new PietProcessor(program);

await foreach (var ev in processor.RunAsyncEnumerable())
{
    // Handle events (OutputInt, OutputChar, etc.)
}
```

## Current Status

- `PietProcessor` executes parsed `PietProgram` instances.
- The processor implements `IEventProcessor` and streams events via `RunAsyncEnumerable()`.

## Notes

- Use this package when you want to build on the execution model as it evolves.
- For image loading and normalization, use `Esolang.Piet.Parser`.

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
