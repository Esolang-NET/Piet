# Esolang.Piet.Generator

A Roslyn incremental source generator for Piet programs.

## Usage

Apply the `[GeneratePietMethod]` attribute to a partial method to generate Piet execution code:

```csharp
public partial class MyPietProgram
{
    [GeneratePietMethod("program.png")]
    public partial void Execute();
}
```

The generator will analyze the Piet image and generate the execution implementation.

## Supported Return Types

- `void`
- `Task`
- `ValueTask`
- `string` (when the program produces output)
- `Task<string>`
- `ValueTask<string>`
- `IEnumerable<byte>`
- `IAsyncEnumerable<byte>`

## Supported Parameters

- `CancellationToken` for async operations
- `PipeWriter` for output handling
- `PipeReader` for input handling
- `string` for input data
