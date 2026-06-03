# Esolang.Piet.Processor

Execution engine primitives for Piet programs.

This package is intended to execute parsed Piet programs and host the core Piet runtime model.

## Install

```bash
dotnet add package Esolang.Piet.Processor
```

## Usage

### Basic Usage (Event Streaming)

イベントをストリーミングして処理する場合の基本的なアプローチです。

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

`Esolang.Processor.Extensions.IO` パッケージを使用して、終了コードと出力先を指定して簡潔に実行するアプローチです。

```csharp
using Esolang.Piet.Parser;
using Esolang.Piet.Processor;
using Esolang.Processor;
using Esolang.Processor.Extensions.IO; // Esolang.Processor.Extensions.IO パッケージが必要

var program = PietParser.Parse("hello-world.png");
var processor = new PietProcessor(program);

// 出力先を準備
using var output = new StringWriter();

// 実行して終了コードを取得（出力はTextWriter引数経由）
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
