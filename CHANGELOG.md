# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]

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
