# Esolang.Piet

[![.NET](https://github.com/Esolang-NET/Piet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Esolang-NET/Piet/actions/workflows/dotnet.yml)

## Quick Start (Generator)

Write Piet program once, call it as a C# method.

```csharp
using Esolang.Piet;

partial class PietSample
{
    [GeneratePietMethod("Programs/hello.png")]
    public static partial string HelloWorld();

    [GeneratePietMethod("Programs/hello.appp", language: LanguageType.PietPlusPlus)]
    public static partial string HelloWorldPietPlusPlus();
}
```

## Generator Guide

For detailed Generator signatures, patterns, and source specification (including inline sources), see:

- [Generator README](./Generator/README.md)

### Generator Signatures

| Attribute Argument | `partial` Method Parameters (Input) | `partial` Method Return Types (Output) |
| :--- | :--- | :--- |
| `string` (Path/Inline) | `string`, `TextReader`, `PipeReader` | `void`, `int`, `Task<int>`, `ValueTask<int>`, `string`, `Task<string>`, `ValueTask<string>`, `IEnumerable<byte>`, `IAsyncEnumerable<byte>` |

## Implementation Status

| Area | Status |
|---|---|
| Piet program parsing | ✅ |
| Image processing (codel size) | ✅ |
| Runtime execution | ✅ |
| Piet specific instructions | ✅ |
| Piet++ language support | ✅ |

## Install

```bash
dotnet add package Esolang.Piet.Generator
dotnet add package Esolang.Piet.Parser
dotnet add package Esolang.Piet.Processor
dotnet tool install -g dotnet-piet
```

## Choose Package

| Want to do | Package |
|---|---|
| Generate C# methods from Piet/Piet++ at compile time | Esolang.Piet.Generator |
| Parse source into a Piet program | Esolang.Piet.Parser |
| Execute Piet/Piet++ in-process | Esolang.Piet.Processor |
| Run Piet/Piet++ from CLI | dotnet-piet |

## Piet++ Formats

- `ascii-piet++` text: `.appp`, `.txt2`
- With Generator, pass `language: LanguageType.PietPlusPlus` when using Piet++ sources.

## License

This project is licensed under the MIT License - see the [LICENSE](./LICENSE) file for details.


## NuGet

| Project | NuGet | Summary |
|---|---|---|
| [dotnet-piet](./Interpreter/README.md) | [![NuGet: dotnet-piet](https://img.shields.io/nuget/v/dotnet-piet?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/dotnet-piet/) | Piet command-line interpreter. |
| [Esolang.Piet.Generator](./Generator/README.md) | [![NuGet: Esolang.Piet.Generator](https://img.shields.io/nuget/v/Esolang.Piet.Generator?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Piet.Generator/) | Piet source generator. |
| [Esolang.Piet.Parser](./Parser/README.md) | [![NuGet: Esolang.Piet.Parser](https://img.shields.io/nuget/v/Esolang.Piet.Parser?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Piet.Parser/) | Piet source parser. |
| [Esolang.Piet.Processor](./Processor/README.md) | [![NuGet: Esolang.Piet.Processor](https://img.shields.io/nuget/v/Esolang.Piet.Processor?logo=nuget&label=2.0.0)](https://www.nuget.org/packages/Esolang.Piet.Processor/) | Piet execution engine. |

## Framework Support

| Project | Target frameworks |
|---|---|
| Esolang.Piet.Generator | netstandard2.0 |
| Esolang.Piet.Parser | net8.0, net9.0, net10.0, netstandard2.0, netstandard2.1 |
| Esolang.Piet.Processor | net8.0, net9.0, net10.0 |
| dotnet-piet | net10.0 |

## Changelog

- [CHANGELOG](./CHANGELOG.md)

## See also

- [The official Piet page](https://www.dangermouse.net/esoteric/piet.html)
- [Piet on Esolangs wiki](https://esolangs.org/wiki/Piet)
