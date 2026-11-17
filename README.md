# EVI-ID

**EVI-ID** is an experimental Android development environment built with C# and .NET 10.

It combines an interactive command shell, file-system operations, diagnostics, C# compilation, and Roslyn-based source analysis and refactoring.

## Project Information

| Property | Value |
|---|---|
| Version | 0.1.0 |
| Stage | Pre-Alpha |
| Platform | Android |
| Framework | .NET 10 (`net10.0-android`) |
| Language | C# |
| Refactoring | Microsoft Roslyn |
| Latest reported Android tests | 87 passed, 0 failed |

## Features

### Interactive Shell

- Built-in commands and command help.
- Internal file and directory operations.
- External storage access through Android SAF.
- Console output navigation.
- Command execution and diagnostics.

### Development Tools

- Experimental Roslyn C# compilation.
- Source-file analysis.
- Refactor(F) candidate discovery.
- Read-only refactoring previews.
- Metadata-reference loading through SAF.
- Experimental GitHub archive downloading.
- Internal regression tests.

## Development Status

EVI-ID is under active development.

The current implementation is experimental and is not production-ready. Interfaces, functionality, and compatibility may change.

No stable release has been declared.

## Getting Started

Launch EVI-ID on Android and open its command interface.

Basic commands:

    help
    pwd
    space
    ls

Switch between storage spaces:

    space internal
    space external

External storage requires an existing Android SAF connection.

## Refactor(F)

Refactor(F) provides experimental C# source analysis, candidate discovery, and read-only transformation previews.

Example workflow:

    refactor Example.cs --discover
    refactor --candidates
    refactor --preview <candidate-id>
    refactor --clear

Replace <candidate-id> with an identifier returned by discovery.

Optional metadata references:

    refactor Example.cs --refs System.Runtime.dll --discover

The --refs option accepts comma-separated DLL filenames. Reference assemblies must be accessible through the configured SAF reference directory.

### Safety and Limitations

- Discovery does not modify source files.
- Preview does not apply transformations.
- Source files are re-read during preview.
- Preview may report unsupported or review-required cases.
- Compilation success alone does not prove behavioral equivalence.
- Changing the active file space requires a new discovery session.

Refactor(F) remains experimental. All proposed transformations require independent review.


## C# Build Module

EVI-ID provides experimental Roslyn-based C# compilation.

Command:

    build <project-directory>

The module recursively scans C# source files, creates a Roslyn compilation, reports diagnostics, and writes a DLL under:

    <project-directory>/bin/evi-build/

Directories named Tools and translation-output are excluded from source discovery.

This is not equivalent to dotnet build. General MSBuild project evaluation, NuGet restore, and Android APK generation are not implemented by this module.

## GitHub Integration

Available commands:

    git version
    git clone <repository-url>

The current Git module downloads and extracts the main branch ZIP archive from GitHub.

Despite the command name, this is not a full Git clone. The operation does not create Git metadata or preserve commit history.

Git operations such as commit, pull, and push are not implemented by the module.

## Testing and Diagnostics

Internal diagnostics include:

    refdiag
    saf-test
    editor jit-test
    utst

The latest reported Android development test run completed with:

    Tests: 87
    Passed: 87
    Failed: 0

This result applies to the tested development build and environment, not necessarily to every device or configuration.

## Documentation

- [Command Reference](docs/COMMANDS.md)
- [Refactor(F) Guide](docs/REFACTOR.md)
- [Architecture](docs/ARCHITECTURE.md)
- [Testing Guide](docs/TESTING.md)

The documentation describes current experimental behavior and known limitations.

## Verified Development Checks

The latest reported development checks include:

- Android APK export through C# Shell: successful, with 0 errors and 72 warnings.
- Android internal unit-test runner: 87 passed, 0 failed.
- Standalone RefactorCi .NET 10 regression runner: 27 passed, 0 failed.
- RefactorCi Release build: 0 errors and 0 warnings.

The Android and RefactorCi test suites are separate and must not be combined into a single test count.

The RefactorCi support sources use conditional compilation with `REFACTOR_CI`. This allows the files to remain in the Android project directory without introducing duplicate test-runner definitions during APK compilation.

These results describe the tested development environment, not general production readiness.
