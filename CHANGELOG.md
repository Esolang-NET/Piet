# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

### Changed
- `Esolang.Piet.Processor` and `Esolang.Piet.Interpreter`: Migrated to `Esolang.Abstractions` v2.0.0.

### Added
- `Esolang.Piet.Generator`: Added PT0013 diagnostic for enforcing partial method declaration.

## [1.1.2] - 2026-05-25

### Added
- `Esolang.Piet.Generator`: Implement logging support for runtime instructions.
- `Esolang.Piet.Generator.Tests`: Standardized test utility methods and improved diagnostic logging for test failures.

## [1.1.1] - 2026-05-09

### Added

- `dotnet-piet` (`Esolang.Piet.Interpreter`): added `--ascii-piet-text` option to execute inline ascii-piet text directly without requiring a file path.
- `Esolang.Piet.Interpreter.Tests`: added CLI tests for inline ascii-piet input mode and argument-validation paths (missing input source / conflicting path+inline input).
- `Esolang.Piet.Interpreter.Tests`: added hybrid conformance vectors combining inline ascii-piet checks and sample-file checks (hello-world, ascii-piet text, PPM, GIF) for execution exit-code and representative output verification.
- `Esolang.Piet.Generator.Tests`: added sample-based conformance vectors using real sample assets to verify generation succeeds without diagnostics.
- `Esolang.Piet.Generator`: `PT0008` / `PT0007` Severity `Error`→ `Hidden`

### Changed

- `Esolang.Piet.Generator`: `TextWriter` and `PipeWriter` output parameters can now be combined with `int`, `Task`, `Task<int>`, `ValueTask`, and `ValueTask<int>` return types. Only string/byte-sequence return types (`string`, `Task<string>`, `ValueTask<string>`, `IEnumerable<byte>`, `IAsyncEnumerable<byte>`) still conflict with explicit output parameters (`PT0011`).
- `Esolang.Piet.Generator`: PT0011 diagnostic message updated to clarify that only string or byte-sequence return types conflict with output parameters.
- `dotnet-piet` (`Esolang.Piet.Interpreter`): root command `path` argument is now optional and validated conditionally so either `path` or `--ascii-piet-text` must be supplied.
- Package metadata: added NuGet `PackageTags` for packable Piet packages (`Generator`, `Parser`, `Processor`, `dotnet-piet`) to improve search/discovery.

### Fixed

- `Esolang.Piet.Generator`: `Task` and `ValueTask` return types were missing the `async` modifier on the generated method signature when combined with `TextWriter` or `PipeWriter` parameters.

## [1.1.0] - 2026-05-08

### Added

- `Esolang.Processor.Abstractions` (`Esolang.Processor` namespace): shared execution abstractions package (`IProcessor<TProgram>`, `ITextProcessor<TProgram>`, `IPipeProcessor<TProgram>`).
- `Esolang.Piet.Processor.Tests`: coverage for `RunToEnd(...)` and `RunToEndAsync(...)` on `PietProcessor`.
- `Esolang.Piet.Parser`: Added `AsciiPietFormatter` to convert `PietProgram` instances to ascii-piet text format without trailing newlines.
- `dotnet-piet`: Added `parse` subcommand to parse image files and output ascii-piet text format.
- `dotnet-piet`: Added `--ascii-piet` option to output parsed program as ascii-piet text instead of executing it.

### Changed

- `Esolang.Piet.Processor`: `PietProcessor` now implements `ITextProcessor<PietProgram>` and exposes `RunToEnd(...)` / `RunToEndAsync(...)` with integer exit codes.
- `Esolang.Piet.Processor`: switched abstraction source from local `Processor/IProcessor.cs` to `Esolang.Processor.Abstractions` package.
- `dotnet-piet` (`Esolang.Piet.Interpreter`): command execution path now calls `RunToEnd(...)`.
- `Esolang.Piet.Generator/README.md` and `samples/Generator.UseConsole`: documented and added a sample for inline ascii-piet data URI usage via `GeneratePietMethod("data:text/ascii-piet;codel-size=1,l_ C")`.
- `Esolang.Piet.Generator`: added return-type support for `int`, `Task<int>`, and `ValueTask<int>` (returns `0` on normal completion).
- `Esolang.Piet.Generator`: generated runtime internal class and internal entry methods are now annotated with `[EditorBrowsable(EditorBrowsableState.Never)]`.
- Build/package baseline: incremented `AssemblyVersion` / `FileVersion` to `1.1.0.3`.

### Fixed

- `Esolang.Piet.Generator`: fixed inline data URI MIME handling to accept `text/ascii-piet` (while keeping backward compatibility for legacy `text/acii-piet`).

## [1.0.0] - 2026-05-06

### Added

- Piet.Parser / Piet.Generator: Added support for ascii-piet text format (`.txt`) and Netpbm PPM (P3, `.ppm`).
- Piet.Parser / Piet.Generator: Added GIF (`.gif`) input support.
- dotnet-piet: Added the `--codel-size` option.
- Generator: Added generated `GeneratedPietInfo` metadata attribute.
- Source generator now reports warning `PT0012` when the consumer language version is below C# 8.0.
- Added generator tests to validate C# 12 parse-option compatibility and minimum-language warning behavior.
- Expanded tests across Generator/Parser/Processor and improved overall coverage.

### Changed

- Changed `GeneratedPietInfo.path` output from absolute paths to project-relative paths.
- Standardized generator behavior to emit throwing method bodies even when diagnostics such as `PT0001` are reported.
- Refactored runtime DP branch logic into explicit branches and removed unreachable default paths.
- Translated documentation comments to English and filled missing XML comments on public APIs.
- Build/package baseline: incremented `AssemblyVersion` / `FileVersion` to `1.0.0.2`.
- `dotnet-piet`: enabled trimming/AOT analyzer-related properties and marked tool package as AOT-compatible for `net8.0+`.

### Fixed

- Source generator async byte-stream path now passes a bound cancellation token to `PipeReader.ReadAsync(...)` for better cancellation responsiveness.
- Generator tests: standardized calls to pass the shared test `CancellationToken` where supported.

## [0.1.0-preview-1] - 2026-04-16

### Added

- Initial repository structure for Piet support.
- Parser, processor, and interpreter package skeletons.

[Unreleased]: https://github.com/Esolang-NET/Piet/compare/v1.1.2...HEAD
[1.1.2]: https://github.com/Esolang-NET/Piet/tree/v1.1.2
[1.1.1]: https://github.com/Esolang-NET/Piet/tree/v1.1.1
[1.1.0]: https://github.com/Esolang-NET/Piet/tree/v1.1.0
[1.0.0]: https://github.com/Esolang-NET/Piet/tree/v1.0.0
[0.1.0-preview-1]: https://github.com/Esolang-NET/Piet/tree/v0.1.0-preview-1
