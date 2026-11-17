# EVI-ID Architecture

## Overview

EVI-ID is an experimental Android development environment implemented in C# and targeting .NET 10 for Android.

The application combines an interactive command interface, storage abstractions, diagnostics, Roslyn-based compilation, and source-refactoring experiments.

The architecture is evolving and should not be treated as a stable public API.

## Android Application Layer

The Android application provides the user-facing interface and platform integration.

Principal source files include:

- MainActivity.cs
- DiagnosticsActivity.cs

These components connect the Android application lifecycle and interface to development functionality.

## Interactive Shell

The Shell directory contains command execution, session management, storage integration, and diagnostic components.

Representative components include:

- CommandRunner.cs
- ShellSession.cs
- Logger.cs
- ProcessManager.cs
- StorageManager.cs
- HelpSystem.cs

The command environment is application-specific rather than a complete operating-system shell.

## Storage Model

EVI-ID distinguishes between internal application storage and external storage accessed through Android's Storage Access Framework (SAF).

Storage-related components include:

- EVIFileSystem.cs
- SafFileSystem.cs
- ExternalFolderManager.cs
- ExternalFolderBrowser.cs
- DocumentContractHelper.cs

SAF operations are based on Android document permissions and document identifiers.

Not every command supports both storage models identically.

## Development Subsystems

EVI-ID includes experimental development capabilities.

These include:

- Roslyn-based C# source analysis.
- C# compilation through the build command.
- Refactor(F) discovery and preview.
- Metadata-reference resolution.
- Internal diagnostics and regression tests.

The mdl directory contains development and analysis implementation components.

The Testing directory contains test-related source files.

## Refactor(F) Integration

Refactor(F) separates candidate discovery from transformation preview.

Discovery identifies source regions and establishes an active session.

Preview re-reads source material, performs supported validation, and may produce proposed transformed source.

Preview does not write transformed source files.

Semantic analysis depends on the available compilation context and metadata references.

## Design Constraints

Current architectural priorities include:

- Conservative source transformation.
- Explicit diagnostic reporting.
- Separation of preview from source modification.
- Android-compatible storage access.
- Incremental development and regression testing.
- Clear documentation of unsupported behavior.

## Stability

The current architecture is experimental.

Component boundaries, internal interfaces, and implementation details may change as development continues.

## Command Processing Model

The following describes the logical command-processing model rather than an exhaustive implementation call graph.

A typical interaction involves:

1. The user enters a command through the Android interface.
2. The application passes the command to its shell functionality.
3. The command is interpreted according to the supported command set.
4. The appropriate subsystem performs the requested operation.
5. Results and diagnostics are presented through the application interface.

Commands may interact with storage, compilation, refactoring, or diagnostic components.

The exact internal execution path depends on the command.

## Storage Boundaries

Internal and External file spaces represent different storage-access mechanisms.

Internal storage uses application-accessible filesystem paths.

External storage uses Android SAF permissions and document-oriented operations.

A path valid in one storage space is not necessarily meaningful in the other.

Components must not assume that an SAF document can always be accessed through ordinary System.IO operations.

The current type and cat implementations have narrower storage support than general SAF-aware commands.

## Refactor(F) Data Flow

The conceptual Refactor(F) workflow is:

    Source selection
          |
          v
    Source analysis
          |
          v
    Candidate discovery
          |
          v
    Discovery session
          |
          v
    Candidate selection
          |
          v
    Source re-reading
          |
          v
    Semantic validation
          |
          v
    Read-only preview

This diagram omits implementation-specific branches, diagnostics, and failure paths.

Metadata references may participate in analysis and preview validation.

The discovery session is associated with its originating file space.

## Validation and Trust Boundaries

Refactor(F) processes source files and optional metadata references supplied through the application's storage mechanisms.

Source contents may change after discovery.

Metadata references may be missing or unsuitable for a particular compilation context.

The implementation therefore cannot treat previously discovered candidates as permanently valid.

Validation results must be interpreted within the available analysis context.

Compilation success does not establish behavioral equivalence.

## Development and Testing Boundaries

EVI-ID contains internal development tests and diagnostic workflows.

These provide evidence about implemented behavior under tested conditions.

They do not guarantee correctness across every Android device, storage provider, source project, or metadata-reference configuration.

Changes to command parsing, storage behavior, and semantic validation should be accompanied by focused regression tests.

## Future Evolution

Potential architectural extensions should be evaluated against actual implementation requirements.

In particular:

- New commands should document their supported storage model.
- Refactoring features should retain explicit validation boundaries.
- Compilation capabilities should distinguish Roslyn compilation from MSBuild.
- GitHub integration should distinguish archive downloads from full Git operations.
- Public documentation should be updated when observed behavior changes.

These are development considerations, not claims that the corresponding extensions are already implemented.
