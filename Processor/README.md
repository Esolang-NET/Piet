# Esolang.Piet.Processor

Execution engine primitives for Piet programs.

This package is intended to execute parsed Piet programs and host the core Piet runtime model.

## Install

```bash
dotnet add package Esolang.Piet.Processor
```

## Usage

### Basic Usage (Event Streaming)

This is the basic approach for processing events as a stream.

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

### Simplified Execution (Extensions)

This approach uses the `Esolang.Processor.Extensions.IO` package to run the processor with an explicit exit code and output target.

```csharp
using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using Esolang.Processor;
using Esolang.Processor.Extensions.IO; // Requires the Esolang.Processor.Extensions.IO package

var program = PietParser.Parse("hello-world.png");
var processor = new PietProcessor(program);

// Prepare an output sink
using var output = new StringWriter();

// Execute and get the exit code (output is written through the TextWriter argument)
int exitCode = await processor.RunToEndAsync(output: output);

Console.WriteLine($"Exit code: {exitCode}");
Console.WriteLine($"Output: {output}");
```

## Current Status

- `PietProcessor` executes parsed `PietProgram` instances.
- The processor implements `IEventProcessor` and streams events via `RunAsyncEnumerable()`.

## Notes

- Use this package when you want to build on the execution model as it evolves.
- For image loading and normalization, use `Esolang.Piet.Parser`.

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
