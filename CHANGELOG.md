# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]


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
