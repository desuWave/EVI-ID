# EVI-ID

EVI-ID is an experimental Android application focused on command execution, file management, software development tools, and diagnostics.

- **Version:** `0.1.0`
- **Development stage:** Pre-Alpha
- **Platform:** Android
- **Target framework:** .NET 10 (`net10.0-android`)

## Overview

EVI-ID is an evolving Android-based development environment combining a command interface with project management, file-system operations, diagnostics, and experimental development modules.

The project is under active development. Features, interfaces, and internal architecture may change.

## Current Implementation

### Console and Commands

- Interactive command interface.
- Built-in file and directory operations.
- Command output and error reporting.
- Command help system.
- Internal working-directory navigation.

### File System and Projects

- Internal project directory management.
- External folder access through Android's Storage Access Framework (SAF).
- File creation, reading, writing, renaming, and deletion.
- Internal and external directory navigation.

### Diagnostics and Logging

- Dedicated diagnostics activity.
- Application logging.
- Log viewing and copying.
- Startup diagnostics.
- Operation tracking.

### Experimental Development Modules

The source tree includes modules for:

- C# compilation and build operations.
- Editor buffers and editing sessions.
- Runtime type loading and method invocation.
- Git integration.
- Source-code structure and refactoring analysis.
- Build-output navigation.
- Internal unit testing.

These components are under development and should not be considered production-ready.

## Project Structure

| Path | Purpose |
|---|---|
| `MainActivity.cs` | Main Android application interface and command handling |
| `DiagnosticsActivity.cs` | Diagnostics interface |
| `Shell/` | Command execution, file systems, storage, logging and shell services |
| `mdl/Build/` | Build functionality |
| `mdl/Editor/` | Experimental editor and compilation functionality |
| `mdl/Git/` | Git-related functionality |
| `mdl/RefactorEngine.cs` | Source-code analysis |
| `Testing/` | Internal testing utilities |

## Build Requirements

EVI-ID targets .NET 10 for Android.

Building requires a compatible .NET SDK, Android development tooling, and the appropriate Android workload or supported build environment.

Some development configurations may require locally supplied reference assemblies and SDK paths.

## Development Status

**Pre-Alpha — active development.**

The current implementation is experimental. Not all features have been fully tested, and some functionality may be incomplete or platform-dependent.

## Future Development

Development may include improvements to the editor, build tools, project management, Git integration, diagnostics, and the overall interactive development experience.

No stable release is currently declared.

## Android Refactor(F) — Experimental

EVI-ID includes an experimental C# source-code refactoring workflow
powered by Roslyn and integrated into the Android command interface.

### Current Capabilities

- C# source analysis with explicitly supplied metadata references.
- Refactoring candidate discovery for supported statement groups.
- Session-based candidate identifiers.
- Read-only preview of proposed method extraction.
- Original and transformed compilation validation.
- Semantic and safety checks for supported extraction scenarios.
- Preview formatting for generated helper methods.
- Android Storage Access Framework (SAF) integration.

### Verified Android Tests

The current development build completed 76 of 76 internal unit
tests successfully, including Android semantic validation and
preview-formatting regression coverage.

A separate Android verification using
`SemanticProbeIndependent.cs` discovered 11 extraction candidates.

Preview of candidate 10 completed with:

- Original compilation errors: 0.
- Transformed compilation errors: 0.
- Status: `ReviewRequired`.
- Reason: `BehavioralEquivalenceNotProven`.
- No source files modified.

### Safety and Limitations

Refactor(F) is experimental and is not production-ready.

Successful compilation does not establish behavioral equivalence.
The current preview workflow does not automatically apply
transformations when behavioral equivalence remains unproven.

Candidate identifiers are session-specific and must be obtained
from the current discovery session.

Preservation of comments, custom formatting, and additional
language constructs requires further regression testing.

The implementation and its safety checks are under active development.
