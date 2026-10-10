# EVI-ID

EVI-ID is an Android application and an initial development
environment for EVI Shell.

## Current features

- Console interface for executing Android system commands.
- Display of command exit codes, standard output, and errors.
- Project directory creation and listing.
- Diagnostics activity.
- Application logging and log copying.
- Separate Console, GUI, and Projects tabs.

## Project structure

- `MainActivity.cs` — main EVI Shell interface.
- `DiagnosticsActivity.cs` — diagnostics interface.
- `Shell/` — shell infrastructure.
- `EVI-ID.csproj` — project configuration.
- `EVI-ID.sln` and `EVI-ID.slnx` — solution files.

## Status

Early development prototype. The graphical workspace
and full C# project execution workflow are not implemented yet.

## Build

Requires a compatible .NET SDK and the Android workload.
See `EVI-ID.csproj` for the target framework and application settings.
