# Changelog

All notable changes to this repository are documented in this file.

The format is based on Keep a Changelog.

## [Unreleased]


### Added

- Piet.Parser: ascii-piet テキスト形式（.txt）、Netpbm PPM (P3, .ppm) 画像形式のパース対応。
- Source generator now reports warning `PT0012` when the consumer language version is below C# 8.0.
- Added generator tests to validate C# 12 parse-option compatibility and minimum-language warning behavior.

### Fixed

- Source generator async byte-stream path now passes a bound cancellation token to `PipeReader.ReadAsync(...)` for better cancellation responsiveness.

## [0.1.0-preview-1] - 2026-04-16

### Added

- Initial repository structure for Piet support.
- Parser, processor, and interpreter package skeletons.
