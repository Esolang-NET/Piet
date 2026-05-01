# Esolang.Piet

[![.NET](https://github.com/Esolang-NET/Piet/actions/workflows/dotnet.yml/badge.svg)](https://github.com/Esolang-NET/Piet/actions/workflows/dotnet.yml)

## Quick Start (Generator)

Write Piet once, call it as a C# method.

```cs
using Esolang.Piet;

Console.WriteLine(PietSample.RunToString());

partial class PietSample
{
		[GeneratePietMethod("hello-world.png")]
		public static partial string? RunToString();
}

// output:
// Hello, world!
```

Add the source image in your project file:

```xml
<ItemGroup>
	<PietImage Include="samples\hello-world.png" PietLogicalPath="hello-world.png" />
</ItemGroup>
```

## Generator Guide

For detailed Generator signatures and patterns (`string`, `TextReader`, `PipeReader`, `TextWriter`, `PipeWriter`, sync/async returns, byte-sequence returns), see:

- [Generator README](./Generator/README.md)

For runnable examples including `TextReader`/`PipeReader` input, `TextWriter`/`PipeWriter` output, and multiple return patterns, see:

- [UseConsole sample](./samples/Generator.UseConsole/README.md)

## Install

```bash
dotnet add package Esolang.Piet.Generator
dotnet add package Esolang.Piet.Parser
dotnet add package Esolang.Piet.Processor
dotnet tool install -g dotnet-piet --prerelease
```

## Choose Package

| Want to do | Package |
| --- | --- |
| Generate C# methods from Piet at compile time | Esolang.Piet.Generator |
| Parse image source into normalized codels | Esolang.Piet.Parser |
| Execute Piet in-process | Esolang.Piet.Processor |
| Run Piet from CLI | dotnet-piet |

## NuGet

| Project | NuGet | Summary |
| --- | --- | --- |
| [dotnet-piet](./Interpreter/README.md) | [![NuGet: dotnet-piet](https://img.shields.io/nuget/v/dotnet-piet?logo=nuget)](https://www.nuget.org/packages/dotnet-piet/) | piet command line utility dotnet-piet command. |
| [Esolang.Piet.Generator](./Generator/README.md) | [![NuGet: Esolang.Piet.Generator](https://img.shields.io/nuget/v/Esolang.Piet.Generator?logo=nuget)](https://www.nuget.org/packages/Esolang.Piet.Generator/) | piet method generator. |
| [Esolang.Piet.Parser](./Parser/README.md) | [![NuGet: Esolang.Piet.Parser](https://img.shields.io/nuget/v/Esolang.Piet.Parser?logo=nuget)](https://www.nuget.org/packages/Esolang.Piet.Parser/) | piet image parser. |
| [Esolang.Piet.Processor](./Processor/README.md) | [![NuGet: Esolang.Piet.Processor](https://img.shields.io/nuget/v/Esolang.Piet.Processor?logo=nuget)](https://www.nuget.org/packages/Esolang.Piet.Processor/) | piet processor. |

## Framework Support

| Project | Target frameworks |
| --- | --- |
| Esolang.Piet.Generator | netstandard2.0 |
| Esolang.Piet.Parser | net8.0, net9.0, net10.0, netstandard2.0, netstandard2.1 |
| Esolang.Piet.Processor | net8.0, net9.0, net10.0 |
| dotnet-piet | net8.0, net9.0, net10.0 |

## Changelog

- [CHANGELOG](./CHANGELOG.md)

## See also

- Piet language reference: https://www.dangermouse.net/esoteric/piet.html

**Note:** GIF (`.gif`) is also supported.