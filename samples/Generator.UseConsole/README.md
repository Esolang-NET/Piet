# Esolang.Piet.Generator.UseConsole Sample

This sample demonstrates how to use Esolang.Piet.Generator with the main method signature patterns.

## Project

- `Esolang.Piet.Generator.UseConsole.csproj`
- `Esolang.Piet.Generator.UseConsole.cs`

## Sample images

This sample uses three images:

- `samples/no-op.png` (no input, no output)
- `samples/hello-world.png` (output)
- `samples/input-output.png` (input + output)

Preview:

![no-op.png](samples/no-op.png)
![hello-world.png](samples/hello-world.png)
![input-output.png](samples/input-output.png)

## Config in csproj

Images are connected through `PietImage` items:

```xml
<ItemGroup>
  <PietImage Include="samples\no-op.png" PietLogicalPath="no-op.png" />
  <PietImage Include="samples\hello-world.png" PietLogicalPath="hello-world.png" />
  <PietImage Include="samples\input-output.png" PietLogicalPath="input-output.png" />
</ItemGroup>
```

## Run

```bash
dotnet run --project samples/Generator.UseConsole/Esolang.Piet.Generator.UseConsole.csproj --framework net10.0
```

Example output:

```text
Running Piet generated sample...
RunToString: Hello, world!
RunToTaskString: Hello, world!
RunToValueTaskString: Hello, world!
RunToEnumerableBytes: Hello, world!
RunToAsyncEnumerableBytes: Hello, world!
RunWithStringInput:
RunWithPipeReader:
RunWithPipeWriter: Hello, world!
```

## Signature patterns shown in this sample

- `void` with console input/output
- `string` return
- `System.Threading.Tasks.Task<string>` return
- `System.Threading.Tasks.ValueTask<string>` return
- `System.Collections.Generic.IEnumerable<byte>` return
- `System.Collections.Generic.IAsyncEnumerable<byte>` return
- `string` input
- `System.IO.Pipelines.PipeReader` input
- `System.IO.Pipelines.PipeWriter` output

### Method to image mapping

- `RunToConsole` -> `no-op.png`
- `RunToString` -> `hello-world.png`
- `RunToTaskString` -> `hello-world.png`
- `RunToValueTaskString` -> `hello-world.png`
- `RunToEnumerableBytes` -> `hello-world.png`
- `RunToAsyncEnumerableBytes` -> `hello-world.png`
- `RunWithStringInput` -> `input-output.png`
- `RunWithPipeReader` -> `input-output.png`
- `RunWithPipeWriter` -> `hello-world.png`
