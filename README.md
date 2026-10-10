# EVI-ID

EVI-ID is an early-stage Android application currently focused on command execution, diagnostics, logging, and basic project management.

**Version:** `0.1.0-pre-alpha.1`
**Development stage:** Pre-alpha
**Platform:** Android

The project is under active development. This repository documents the current implementation and its progress.

## Current Features

* Console interface for executing supported Android system commands.
* Display of command exit codes, standard output, and errors.
* Project directory creation and listing.
* Diagnostics activity.
* Application logging and log copying.
* Separate Console, GUI, and Projects tabs.

Available commands depend on the Android application environment and system permissions. EVI-ID does not provide unrestricted access to the Android operating system or imply root access.

## Current Limitations

* The GUI tab does not yet provide a complete graphical workspace.
* A complete workflow for creating, building, and running C# projects inside the application is not implemented.
* The application is an early prototype and is not a feature-complete development environment.

## Project Structure

* `MainActivity.cs` — main application interface.
* `DiagnosticsActivity.cs` — diagnostics interface.
* `Shell/` — command execution, logging, and storage infrastructure.
* `EVI-ID.csproj` — project configuration and application version.
* `EVI-ID.sln` and `EVI-ID.slnx` — solution files.

## Versioning

EVI-ID currently uses pre-release version numbering.

Current application display version:

`0.1.0-pre-alpha.1`

This version identifies an early development milestone and does not indicate production readiness.

## Build Requirements

Building EVI-ID requires a compatible .NET SDK and the Android workload.

See `EVI-ID.csproj` for the target framework, application identifier, version, and other build settings.

## Build Artifacts

APK files generated during development are build artifacts. They may be retained locally or archived separately by version.

Build output directories such as `bin/` and `obj/` are excluded from Git.

## Project Status

**Pre-alpha — active development.**

Features and internal structure may change as development progresses.
