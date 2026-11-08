# EVI-ID Refactor(F): Semantic Validation History V1–V7

Historical archive of the isolated C# / Roslyn semantic
validation experiments developed for EVI-ID Refactor(F).

## Generations

- V1: Initial semantic extraction validation.
- V2: Symbol-binding preservation integration.
- V3: Caller-info safety validation.
- V4: Constructor caller-info regression coverage.
- V5: nameof-related semantic regression coverage.
- V6: Overload-binding preservation regressions.
- V7: Semantic integration and advanced regression coverage.

## Contents

- SemanticValidatorV1–V7: Historical validator generations.
- RefactorIntegrationV7: Isolated Refactor(F) integration tests.
- RefactorAndroidCompatibility: Compatibility test project.
- Other directories: Focused semantic regression experiments.
- *.before-* files: Intermediate historical snapshots.
- *.sha256 files: Historical integrity manifests.

## Verified V7 integration

The isolated RefactorIntegrationV7 implementation passed
14 integration regression checks.

The compatibility project also passed the same checks
using the RefactorSafetyAnalyzer.cs from Android EVI-ID.

These results do not establish full behavioral equivalence
or prove Android runtime compatibility.

## Build environment

The experiments were developed and tested using .NET SDK 8
and Roslyn assemblies supplied with the SDK.

Most projects reference Roslyn through MSBuildSDKsPath.

RefactorAndroidCompatibility.csproj retains its original
Termux-specific Roslyn HintPath entries and an external
reference to the Android EVI-ID source tree.

It is preserved as a historical artifact and is not
standalone or portable without configuration changes.

## Safety

V7 does not automatically promote extraction candidates
to Safe based solely on semantic validation.

Results remain subject to the existing Refactor(F)
safety classification and conservative review policy.

## Archive policy

This archive preserves original source files, project
files, regression experiments, and intermediate snapshots.

Build output directories bin/ and obj/ are excluded.

The archive is not an automatic replacement for the
Refactor(F) implementation in the main EVI-ID application.
