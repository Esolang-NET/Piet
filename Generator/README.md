# Esolang.Piet.Generator

Piet source generator for .NET.

## Install

```bash
dotnet add package Esolang.Piet.Generator
```


## Usage

You can use PNG, GIF (.gif), ascii-piet text (.txt), or Netpbm PPM (P3, .ppm) as Piet source images.

Use `GeneratePietMethodAttribute` on a `partial` method.

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial string RunToString();

    [GeneratePietMethod("no-op.png")]
    public static partial void RunNoOp();

    [GeneratePietMethod("ascii-piet-sample.txt")]
    public static partial string RunAsciiPiet();

    [GeneratePietMethod("ppm-sample.ppm")]
    public static partial string RunPpm();

    // GIF with explicit codel size
    [GeneratePietMethod("dot.gif", codelSize: 1)]
    public static partial void RunDotGif();

    [GeneratePietMethod("dot-codel-11.gif", codelSize: 11)]
    public static partial void RunDotCodel11Gif();

    [GeneratePietMethod("hw1-11.gif", codelSize: 11)]
    public static partial string RunHw111Gif(string input = "");
}
```

In your project file, specify the image via `PietImage` (PNG, .gif, .txt, .ppm all supported):

```xml
<ItemGroup>
    <PietImage Include="samples\hello-world.png" PietLogicalPath="hello-world.png" />
    <PietImage Include="samples\no-op.png" PietLogicalPath="no-op.png" />
    <PietImage Include="samples\ascii-piet-sample.txt" PietLogicalPath="ascii-piet-sample.txt" />
    <PietImage Include="samples\ppm-sample.ppm" PietLogicalPath="ppm-sample.ppm" />
    <PietImage Include="samples\dot.gif" PietLogicalPath="dot.gif" />
    <PietImage Include="samples\dot-codel-11.gif" PietLogicalPath="dot-codel-11.gif" />
    <PietImage Include="samples\hw1-11.gif" PietLogicalPath="hw1-11.gif" />
</ItemGroup>
```


## Supported Piet Image Formats

- PNG (standard)
- GIF (`.gif`)
- ascii-piet text format (`.txt`)
- Netpbm PPM (P3, `.ppm`)

Image format is detected automatically from the file extension.

## Features

- Resolves Piet image paths from `PietImage` items.
- Converts PNG to generator-readable AdditionalFiles via build targets.
- Supports explicit input and output bindings.

## Supported Method Signatures

| Category | Supported types |
| --- | --- |
| Return type | `void`, `string`, `System.Threading.Tasks.Task<string>`, `System.Threading.Tasks.ValueTask<string>`, `System.Collections.Generic.IEnumerable<byte>`, `System.Collections.Generic.IAsyncEnumerable<byte>` |
| Input parameter | `string`, `System.IO.TextReader`, `System.IO.Pipelines.PipeReader` |
| Output parameter | `System.IO.TextWriter`, `System.IO.Pipelines.PipeWriter` |
| Other parameter | `System.Threading.CancellationToken` |

## Required I/O Diagnostics

When the analyzed Piet program requires I/O, the method signature must expose a compatible mechanism.

- `PT0007` is reported when output is required but no output mechanism is provided.
  Output mechanism means either a non-`void` return type, `System.IO.TextWriter`, or `System.IO.Pipelines.PipeWriter`.
- `PT0008` is reported when input is required but no input mechanism is provided.
  Input mechanism means `string`, `System.IO.TextReader`, or `System.IO.Pipelines.PipeReader`.

## Signature Patterns

The generator accepts one input source and one output destination at most.

### 1. `void` with no explicit I/O

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("no-op.png")]
    public static partial void RunNoOp();
}
```

No implicit `Console.In` or `Console.Out` is injected.

```csharp
PietSample.RunNoOp();
```

If you want console behavior, write a wrapper explicitly:

```csharp
public static void RunToConsole()
{
    RunWithTextWriter(Console.Out);
}
```

### 2. Return as `string`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial string RunToString();
}
```

Collects output into an internal `StringWriter` and returns the result.

```csharp
var asString = PietSample.RunToString();
Console.WriteLine($"RunToString: {asString}");
```

### 3. Return as `Task<string>`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial System.Threading.Tasks.Task<string> RunToTaskString();
}
```

Returns `Task.FromResult(output)`.

```csharp
var taskString = await PietSample.RunToTaskString();
Console.WriteLine($"RunToTaskString: {taskString}");
```

### 4. Return as `ValueTask<string>`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial System.Threading.Tasks.ValueTask<string> RunToValueTaskString();
}
```

```csharp
var valueTaskString = await PietSample.RunToValueTaskString();
Console.WriteLine($"RunToValueTaskString: {valueTaskString}");
```

### 5. Return as `IEnumerable<byte>`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial System.Collections.Generic.IEnumerable<byte> RunToEnumerableBytes();
}
```

Yields UTF-8 encoded output bytes after buffered execution.

```csharp
var bytes = new List<byte>(PietSample.RunToEnumerableBytes());
Console.WriteLine($"RunToEnumerableBytes: {Encoding.UTF8.GetString(bytes.ToArray())}");
```

### 6. Return as `IAsyncEnumerable<byte>`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial System.Collections.Generic.IAsyncEnumerable<byte> RunToAsyncEnumerableBytes(
        System.Threading.CancellationToken cancellationToken = default);
}
```

Async iterator that streams UTF-8 output bytes progressively through a pipe.
Cancellation is checked between reads and each yielded byte.

```csharp
var asyncBytes = new List<byte>();
await foreach (var b in PietSample.RunToAsyncEnumerableBytes(CancellationToken.None))
    asyncBytes.Add(b);
Console.WriteLine($"RunToAsyncEnumerableBytes: {Encoding.UTF8.GetString(asyncBytes.ToArray())}");
```

### 7. Input from `string`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithStringInput(string input);
}
```

Uses `input` as a `StringReader` source.

```csharp
var withStringInput = PietSample.RunWithStringInput("123");
Console.WriteLine($"RunWithStringInput: {withStringInput}");
```

### 8. Input from `TextReader`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithTextReader(System.IO.TextReader input);
}
```

Uses `input` directly as text input (`null` is treated as `TextReader.Null`).

```csharp
var withTextReader = PietSample.RunWithTextReader(new StringReader("456\n"));
Console.WriteLine($"RunWithTextReader: {withTextReader}");
```

### 9. Input from `PipeReader`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("input-output.png")]
    public static partial string RunWithPipeReader(System.IO.Pipelines.PipeReader input);
}
```

Wraps `PipeReader` as a `StreamReader` for text input.

```csharp
var inputPipe = new Pipe();
await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("789\n"));
inputPipe.Writer.Complete();
var withPipeReader = PietSample.RunWithPipeReader(inputPipe.Reader);
Console.WriteLine($"RunWithPipeReader: {withPipeReader}");
```

### 10. Output to `TextWriter`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithTextWriter(System.IO.TextWriter output);
}
```

Uses `output` directly (`null` is treated as `TextWriter.Null`).

```csharp
var textWriterOutput = new StringWriter();
PietSample.RunWithTextWriter(textWriterOutput);
Console.WriteLine($"RunWithTextWriter: {textWriterOutput}");
```

### 11. Output to `PipeWriter`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial void RunWithPipeWriter(System.IO.Pipelines.PipeWriter output);
}
```

Wraps `PipeWriter` as an auto-flushing `StreamWriter`.

```csharp
var outputPipe = new Pipe();
PietSample.RunWithPipeWriter(outputPipe.Writer);
outputPipe.Writer.Complete();
var pipeWriterResult = new StreamReader(outputPipe.Reader.AsStream()).ReadToEnd();
Console.WriteLine($"RunWithPipeWriter: {pipeWriterResult}");
```

### 12. GIF with `codelSize`

Specify `codelSize` when the codel size cannot be inferred from the file.

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("dot.gif", codelSize: 1)]
    public static partial void RunDotGif();

    [GeneratePietMethod("dot-codel-11.gif", codelSize: 11)]
    public static partial void RunDotCodel11Gif();

    [GeneratePietMethod("hw1-11.gif", codelSize: 11)]
    public static partial string RunHw111Gif(string input = "");
}
```

`codelSize` sets the pixel width/height of each codel.

```csharp
PietSample.RunDotGif();
PietSample.RunDotCodel11Gif();
Console.WriteLine($"RunHw111Gif: {PietSample.RunHw111Gif()}");
```

## Combination Rules

- At most one input source: `string`, `TextReader`, or `PipeReader`.
- At most one output destination: `TextWriter` or `PipeWriter`.
- Output parameters cannot be combined with non-`void` return types (`PT0011`).
- `CancellationToken` may be combined with other supported parameters.
- Use at most one `CancellationToken` parameter.

## Language Version

- Generated code expects C# 8.0 or later features (for example, async iterators and nullable context directives).
- If the consumer project language version is lower than C# 8.0, the generator reports `PT0012` as a warning.

## Samples

For a concrete sample project and runnable examples, see [samples/Generator.UseConsole/README.md](../samples/Generator.UseConsole/README.md).

## Diagnostics

| ID | Meaning |
| --- | --- |
| PT0001 | Invalid image path parameter on attribute. |
| PT0002 | Unsupported return type. |
| PT0003 | Unsupported parameter type. |
| PT0004 | Duplicate parameter kind (for input, output, or cancellation token). |
| PT0005 | Image file not found. |
| PT0006 | Invalid image format. |
| PT0007 | Required output interface not provided. |
| PT0008 | Required input interface not provided. |
| PT0009 | Duplicate image path mapping. |
| PT0010 | Input interface provided but not required (Hidden). |
| PT0011 | Non-void return type conflicts with explicit output parameter. |
| PT0012 | Consumer language version may be too low (C# 8.0 or later is recommended). |

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
- ascii-piet encoding specification: https://github.com/dloscutoff/ascii-piet#encoding-specification
