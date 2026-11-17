# EVI-ID Command Reference

This document describes the experimental Android command interface.

EVI-ID is not a complete POSIX shell. Commands with familiar names may have different behavior.

## General Commands

| Command | Description |
|---|---|
| `help` | Display built-in command help |
| `help <command>` | Display help for a specific command |
| `pwd` | Show the current directory |
| `space` | Show the active file space |
| `space internal` | Select internal storage |
| `space external` | Select configured SAF storage |
| `ls [path]` | List directory entries |
| `cd [path]` | Change or display the current directory |
| `clear` | Clear console output |
| `cls` | Alias of `clear` |

Switching storage spaces resets the current directory to the selected root.

External storage requires an existing Android SAF connection.

## File Operations

| Command | Description |
|---|---|
| `touch <file>` | Create a file |
| `mkdir <directory>` | Create a directory |
| `write <file> <text>` | Write text to a file |
| `append <file> <text>` | Append text to a file |
| `rename <path> <new-name>` | Rename a file or directory |
| `rm <path>` | Delete a file or directory |

Examples:

    mkdir Demo
    cd Demo
    touch notes.txt
    write notes.txt Hello
    append notes.txt world
    rename notes.txt journal.txt
    ls

File operations use EVI-ID's file-system abstraction.

Exact path behavior depends on the active storage implementation. Destructive operations should be used carefully.

## File Reading

### type

    type <file>
    type --tail N <file>

The optional --tail argument displays the final N lines, where N must be a positive integer.

### cat

    cat <file>

Both commands currently use internal-path resolution and standard .NET file APIs. They are not general SAF document readers.

## Console Navigation

| Command | Description |
|---|---|
| `head` | Navigate to the beginning of output |
| `tail` | Navigate to the end of output |
| `lets go <line>` | Navigate to a specified output line |
| `page up` | Navigate one page upward |
| `page down` | Navigate one page downward |

These commands navigate console output rather than modifying source files.

## C# Build

    build <project-directory>

The experimental build module uses Roslyn to compile C# source files.

Current behavior:

- Accepts a directory rather than a .csproj file.
- Recursively discovers *.cs files.
- Excludes directories named Tools and translation-output.
- Parses sources with the latest supported C# language version.
- Collects available runtime metadata references.
- Creates a Release-mode console-application compilation.
- Reports compiler diagnostics.
- Writes a DLL when compilation succeeds.

Output location:

    <project-directory>/bin/evi-build/<directory-name>.dll

The command does not implement general MSBuild project evaluation, NuGet restore, or Android APK packaging.

## GitHub Integration

    git version
    git clone <repository-url>

The current Git module supports GitHub repository snapshot downloads.

The clone command:

- Accepts a GitHub repository URL.
- Downloads the main branch ZIP archive.
- Extracts the archive into application-managed storage.
- Rejects a nonempty destination directory.
- Does not create a .git directory.
- Does not preserve commit history.

The version command reports the internal EVI-ID Git module version, not the system Git version.

Commit, pull, push, fetch, and checkout are not implemented by this module.

## Refactor(F)

### Analyze

    refactor <file-or-directory>

Analyze C# sources without creating a discovery session.

### Metadata References

    refactor <path> --refs System.Runtime.dll
    refactor <path> --refs System.Runtime.dll,System.Console.dll

Reference names must:

- End with .dll.
- Be filenames, not paths.
- Contain no directory separators.
- Be unique without regard to case.

The application loads these assemblies through its configured SAF reference directory.

### Discover Candidates

    refactor <path> --discover
    refactor <path> --refs System.Runtime.dll --discover

The --discover option must be the final argument.

Discovery creates a session and reports candidate identifiers, source names, methods, line ranges, and statement counts.

Discovery does not modify source files.

### List Candidates

    refactor --candidates

Display candidates from the active discovery session.

### Preview

    refactor --preview <candidate-id>

Preview re-reads the source files and reloads the configured metadata references when necessary.

The output may include:

- Validation status and reason.
- Original compilation check result.
- Transformed compilation check result.
- Original and transformed error counts.
- Proposed transformed source.

Preview never writes the transformed source to disk.

### Clear Session

    refactor --clear

Discard the current discovery session.

### Session Restrictions

A discovery session belongs to the file space in which it was created.

Switching between Internal and External storage requires running discovery again before listing or previewing candidates.

Candidate identifiers are session-specific.

Source changes between discovery and preview may invalidate the proposed transformation.

### Exit Reporting

The command prints a Refactor exit code.

Exceptions and Unsupported preview results are reported with code 1.

Other handled preview statuses, including review-required outcomes, may be reported with code 0.

These are command-level status messages, not Android process exit codes.

## Diagnostics and Testing

### Refactor Diagnostics

    refdiag

Run the built-in Refactor(F) diagnostic workflow.

This command is intended for development and investigation rather than production use.

### SAF Diagnostics

    saf-test

Run diagnostics related to Android Storage Access Framework integration.

The result depends on the current Android storage configuration and granted permissions.

### Editor JIT Diagnostics

    editor jit-test

Run the built-in editor JIT diagnostic workflow.

### Unit Tests

    utst
    unit_test

Run the application's internal unit-test workflow.

The test runner reports individual test results and an aggregate summary.

A passing test run verifies the exercised scenarios only. It does not establish complete behavioral equivalence or production readiness.

## Execution Notes

EVI-ID commands execute inside the application's own command environment.

They should not be assumed to have the same semantics as similarly named commands in Termux, PowerShell, Bash, or Git.

Some commands operate on Android SAF documents, while others use ordinary .NET file APIs.

Consult the implementation and diagnostics when behavior differs between Internal and External file spaces.

## Known Limitations

- The command interface is experimental.
- Not all commands support Android SAF paths.
- The build module is not a replacement for dotnet build.
- The Git module is not a full Git implementation.
- Refactor(F) preview does not apply source modifications.
- A successful compilation is not proof of behavioral equivalence.
- Built-in help text may not yet document every supported option.

## Verified Android Test Result

The latest reported installed Android development build completed `utst` with 87 reported test results, all passing.

The separate RefactorCi regression runner completed 27 tests, all passing. It is executed through .NET rather than through the EVI-ID command shell.

These are development verification results and do not imply that every possible command, source program, or Android storage provider is supported.
