# Esolang.Piet.Generator.UseConsole Sample

This sample demonstrates how to use Esolang.Piet.Generator with the main method signature patterns.

## Project

- `Esolang.Piet.Generator.UseConsole.csproj`
- `Esolang.Piet.Generator.UseConsole.cs`

## Sample images


This sample uses multiple image formats:

- `samples/no-op.png` (PNG, no input, no output)
- `samples/hello-world.png` (PNG, output)
- `samples/input-output.png` (PNG, input + output)
- `samples/ascii-piet-sample.txt` (ascii-piet text, output)
- `samples/ppm-sample.ppm` (PPM, output)
- `samples/sample.gif` (GIF, output)


Preview:

![no-op.png](samples/no-op.png)
![hello-world.png](samples/hello-world.png)
![input-output.png](samples/input-output.png)
ascii-piet: `samples/ascii-piet-sample.txt`
ppm: `samples/ppm-sample.ppm`
gif: `samples/sample.gif`

## Config in csproj


Images are connected through `PietImage` items (PNG, .txt, .ppm, .gif all supported):

```xml
<ItemGroup>
  <PietImage Include="samples\no-op.png" PietLogicalPath="no-op.png" />
  <PietImage Include="samples\hello-world.png" PietLogicalPath="hello-world.png" />
  <PietImage Include="samples\input-output.png" PietLogicalPath="input-output.png" />
  <PietImage Include="samples\ascii-piet-sample.txt" PietLogicalPath="ascii-piet-sample.txt" />
  <PietImage Include="samples\ppm-sample.ppm" PietLogicalPath="ppm-sample.ppm" />
  <PietImage Include="samples\sample.gif" PietLogicalPath="sample.gif" />
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
RunWithTextReader:
RunWithPipeReader:
RunWithPipeWriter: Hello, world!
RunWithTextWriter: Hello, world!
```

## Signature patterns shown in this sample

- `void` with no explicit I/O
- `string` return
- `System.Threading.Tasks.Task<string>` return
- `System.Threading.Tasks.ValueTask<string>` return
- `System.Collections.Generic.IEnumerable<byte>` return
- `System.Collections.Generic.IAsyncEnumerable<byte>` return
- `string` input
- `System.IO.TextReader` input
- `System.IO.Pipelines.PipeReader` input
- `System.IO.Pipelines.PipeWriter` output
- `System.IO.TextWriter` output

### Method to image mapping

- `RunNoOp` -> `no-op.png`
- `RunToString` -> `hello-world.png`
- `RunToTaskString` -> `hello-world.png`
- `RunToValueTaskString` -> `hello-world.png`
- `RunToEnumerableBytes` -> `hello-world.png`
- `RunToAsyncEnumerableBytes` -> `hello-world.png`
- `RunWithStringInput` -> `input-output.png`
- `RunWithTextReader` -> `input-output.png`
- `RunWithPipeReader` -> `input-output.png`
- `RunWithPipeWriter` -> `hello-world.png`
- `RunWithTextWriter` -> `hello-world.png`

`RunToConsole` is a handwritten wrapper that forwards to `RunWithTextWriter(Console.Out)`.

## Supported Piet Image Formats

- PNG (standard)
- GIF (static, `.gif`)
- ascii-piet text format (`.txt`)
- Netpbm PPM (P3, `.ppm`)

拡張子で自動判別されます。

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html
