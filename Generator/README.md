# Esolang.Piet.Generator

Piet source generator for .NET.

## Changelog

- [Repository changelog](../CHANGELOG.md)

## Install

```bash
dotnet add package Esolang.Piet.Generator
```

## Usage

Use `GeneratePietMethodAttribute` on a `partial` method.

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("program.png")]
    public static partial void Run();
}
```

In your project file, specify the PNG via `PietImage`.

```xml
<ItemGroup>
    <PietImage Include="Assets\program.png" PietLogicalPath="program.png" />
</ItemGroup>
```

## Features

- Resolves Piet image paths from `PietImage` items.
- Converts PNG to generator-readable AdditionalFiles via build targets.
- Supports explicit input/output bindings (string, TextReader/TextWriter, PipeReader/PipeWriter).

## Supported Method Signatures

| Category | Supported types |
| --- | --- |
| Return type | `void`, `string`, `System.Threading.Tasks.Task<string>`, `System.Threading.Tasks.ValueTask<string>`, `System.Collections.Generic.IEnumerable<byte>`, `System.Collections.Generic.IAsyncEnumerable<byte>` |
| Input parameter | `string`, `System.IO.TextReader`, `System.IO.Pipelines.PipeReader` |
| Output parameter | `System.IO.TextWriter`, `System.IO.Pipelines.PipeWriter` |
| Other parameter | `System.Threading.CancellationToken` |

## Required I/O diagnostics

When the analyzed Piet program requires I/O, the method signature must expose a compatible mechanism.

- `PT0007` is reported when output is required but no output mechanism is provided.
: output mechanism means either a non-`void` return type, `System.IO.TextWriter`, or `System.IO.Pipelines.PipeWriter`.
- `PT0008` is reported when input is required but no input mechanism is provided.
: input mechanism means `string`, `System.IO.TextReader`, or `System.IO.Pipelines.PipeReader`.

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

No implicit `Console.In` / `Console.Out` is injected.

**Example usage:**

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

**Example usage:**

```csharp
var asString = PietSample.RunToString();
Console.WriteLine($"RunToString: {asString}");
```

### 3. Input from `stringinput-output.png")]
    public static partial string RunWithStringInput(string input);
}
```

Uses `input` as a `StringReader` source.

**Example usage:**

```csharp
var withStringInput = PietSample.RunWithStringInput("123");
Console.WriteLine($"RunWithStringInput: {withStringInput}");
```
{
    [GeneratePietMethod("program.png")]
    public static partial string RunWithStringInput(string input);
}
```

Uses `input` as a `StringReader` source.

### 4. Return as `Task<sthello-world.png")]
    public static partial System.Threading.Tasks.Task<string> RunToTaskString();
}
```

Returns `Task.FromResult(output)` without allocating a state machine.

**Example usage:**

```csharp
var taskString = await PietSample.RunToTaskString();
Console.WriteLine($"RunToTaskString: {taskString}");
```
{
    [GeneratePietMethod("program.png")]
    public static partial System.Threading.Tasks.Task<string> RunToTaskString();
}
```

Returns `Task.FromResult(output)` without allocating a state machine.

### 5. Return as `ValueTahello-world.png")]
    public static partial System.Threading.Tasks.ValueTask<string> RunToValueTaskString();
}
```

**Example usage:**

```csharp
var valueTaskString = await PietSample.RunToValueTaskString();
Console.WriteLine($"RunToValueTaskString: {valueTaskString}");``csharp
using Esolang.Piet;

partial class PietSamplehello-world.png")]
    public static partial System.Collections.Generic.IEnumerable<byte> RunToEnumerableBytes();
}
```

Yields UTF-8 encoded output bytes after buffered execution.

**Example usage:**

```csharp
var bytes = new List<byte>(PietSample.RunToEnumerableBytes());
Console.WriteLine($"RunToEnumerableBytes: {Encoding.UTF8.GetString(bytes.ToArray())}");
```

### 6. Return as `IEnumerable<byte>`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("hello-world.png")]
    public static partial System.Collections.Generic.IAsyncEnumerable<byte> RunToAsyncEnumerableBytes(
        System.Threading.CancellationToken cancellationToken = default);
}
```

Async iterator that streams UTF-8 output bytes progressively through a pipe. Checks cancellation between reads and each yielded byte.

**Example usage:**

```csharp
var asyncBytes = new List<byte>();
await foreach (var b in PietSample.RunToAsyncEnumerableBytes(CancellationToken.None))
    asyncBytes.Add(b);
Console.WriteLine($"RunToAsyncEnumerableBytes: {Encoding.UTF8.GetString(asyncBytes.ToArray())}");
```
### 7. Return as `IAsyncEnumerable<byte>`

```csharp
using Esolang.Piet;

partial class PietSample
{input-output.png")]
    public static partial string RunWithPipeReader(System.IO.Pipelines.PipeReader input);
}
```

Wraps the `PipeReader` as a `StreamReader` for text input.

**Example usage:**

```csharp
var inputPipe = new Pipe();
await inputPipe.Writer.WriteAsync(Encoding.UTF8.GetBytes("789\n"));
inputPipe.Writer.Complete();
var withPipeReader = PietSample.RunWithPipeReader(inputPipe.Reader);
Console.WriteLine($"RunWihello-world.png")]
    public static partial void RunWithPipeWriter(System.IO.Pipelines.PipeWriter output);
}
```

Wraps the `PipeWriter` as an auto-flushing `StreamWriter`.

**Example usage:**

```csharp
var outputPipe = new Pipe();
PietSample.RunWithPipeWriter(outputPipe.Writer);
outputPipe.Writer.Complete();
var pipeWriterResult = new StreamReader(outputPipe.Reader.AsStream()).ReadToEnd();
Console.WriteLine($"RunWithPipeWriter: {pipeWriterResult}");
```

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("program.png")]
    public static partial string RunWithPipeReader(System.IO.Pipelines.PipeReader input);
}
```

Wraps the `PipeReader` as a `StreamReader` for text input.

### 9. Output to `PipeWriter`

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("program.png")]
    public static partial void RunWithPipeWriter(System.IO.Pipelines.PipeWriter output);
}
```

Wraps the `PipeWriter` as an auto-flushing `StreamWriter`.

## Combination Rules

- At most one input source: `string`, `TextReader`, or `PipeReader`.
- At most one output destination: `TextWriter` or `PipeWriter`.
- Output parameters cannot be combined with non-`void` return types (`PT0011`).
- `CancellationToken` may be freely combined with any other parameters.
- Use at most one `CancellationToken` parameter.

## Samples

For a concrete sample project, usage patterns, and sample image reference, see:

- `samples/Generator.UseConsole/README.md`

## Diagnostics

| ID | Meaning |
| --- | --- |
| PT0001 | Invalid value parameter on attribute. |
| PT0002 | Unsupported return type. |
| PT0003 | Unsupported parameter type. |
| PT0004 | Duplicate unsupported parameter pattern. |
| PT0005 | Image file not found. |
| PT0006 | Invalid image format. |
| PT0007 | Required output interface not provided. |
| PT0008 | Required input interface not provided. |
| PT0009 | Duplicate image path mapping. |
| PT0010 | Input interface provided but not required (Hidden). |
| PT0011 | Non-void return type conflicts with explicit output parameter. |
