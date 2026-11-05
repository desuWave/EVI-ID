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
