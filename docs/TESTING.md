# EVI-ID Testing Guide

## Overview

EVI-ID includes internal tests and diagnostic workflows for its experimental Android development environment.

Testing focuses on implemented behavior, regression detection, and validation of development subsystems.

A successful test run does not establish production readiness or complete behavioral equivalence.

## Running Internal Tests

The application provides these command aliases:

    utst
    unit_test

Run either command through the EVI-ID interactive shell.

These commands are not equivalent to invoking dotnet test from an operating-system terminal.

## Latest Reported Result

The latest reported Android development test run completed with:

    Total: 87
    Passed: 87
    Failed: 0

This is a reported development result, not an independently reproduced result for every device or build configuration.

Test counts may change as regression coverage evolves.

## Refactor(F) Testing

Refactor(F) testing includes scenarios related to:

- Candidate discovery.
- Transformation preview.
- Semantic analysis.
- Metadata-reference handling.
- Source mapping.
- Compilation diagnostics.
- Unsupported or review-required transformations.
- Preprocessor directive boundaries.

These tests exercise selected implementation paths.

They do not establish that every possible C# program can be transformed safely.

## Semantic Validation Tests

Semantic tests help identify cases where a transformation may be invalid or insufficiently justified.

Examples of relevant conditions include:

- Missing external type information.
- Invalid source spans.
- Unavailable statement mappings.
- Unsupported source structures.
- Compilation errors introduced by transformations.

Passing semantic tests demonstrates correctness only for the tested expectations and inputs.

## Compilation Versus Behavior

Compilation checks can detect syntax and semantic errors visible to the compiler.

They cannot, by themselves, prove that a transformed program preserves:

- Runtime output.
- Side effects.
- Exception behavior.
- Execution ordering.
- Concurrency behavior.
- External interactions.

Behavioral equivalence requires stronger evidence than successful compilation.

## Diagnostic Commands

Additional development diagnostics include:

    refdiag
    saf-test
    editor jit-test

These commands exercise specific diagnostic workflows.

Results may depend on Android configuration, storage permissions, and the current development build.

## Test Result Interpretation

A passing result means the corresponding test's assertions were satisfied.

A failing result requires investigation before drawing conclusions about the affected subsystem.

An unsupported transformation is not necessarily a defect.

A review-required transformation is not equivalent to a verified safe transformation.

## Known Testing Limitations

- Test coverage is incomplete.
- Device and Android-version coverage is not established.
- SAF provider behavior may vary.
- Metadata-reference availability may vary.
- Compilation success does not prove behavioral equivalence.
- Experimental interfaces and diagnostics may change.

## Regression Policy

When modifying development functionality:

1. Identify the affected subsystem.
2. Preserve a reproducible baseline.
3. Add or update focused regression tests.
4. Run the relevant tests.
5. Review diagnostics and unexpected changes.
6. Record observed results without overstating their scope.

Source changes should not be considered safe solely because unrelated tests continue to pass.

## GitHub Actions Refactor Regression CI

The repository includes a dedicated .NET 10 regression
test runner:

    Testing/RefactorCi/RefactorCi.csproj

The GitHub Actions workflow is defined in:

    .github/workflows/refactor-ci.yml

The workflow runs on Ubuntu and restores and executes
the regression suite.

The initial published CI run completed successfully
for commit 74b1eb7.

The locally verified regression suite reported:

    Total: 27
    Passed: 27
    Failed: 0

This CI suite is separate from the Android application's
internal unit-test runner.

The Android result of 87 passed tests and the CI result
of 27 passed tests must not be combined into a single
test count.

CI success establishes that the selected regression
scenarios passed in the tested environment. It does not
establish full Android compatibility or behavioral
equivalence for arbitrary transformations.

## RefactorCi Conditional Compilation

The standalone regression project defines `REFACTOR_CI` in `Testing/RefactorCi/RefactorCi.csproj`.

The files `Program.cs`, `TestEntry.cs`, and `TestSupport.cs` in that directory are guarded by `#if REFACTOR_CI`.

This prevents their test-runner definitions from conflicting with Android application sources when C# Shell processes files in the project directory.

A verified local Release build completed with zero errors and warnings. Running `RefactorCi.dll` through the .NET host produced 27 passing tests.

The subsequent Android APK export completed with zero errors and 72 warnings while all CI source files remained in place. The installed APK reported 87 passing tests.

Some Android test groups are represented by aggregate result entries. The displayed test count should therefore be interpreted as the number of reported results, not necessarily the number of individual internal assertions.
