# Refactor(F) — Technical Guide

## Overview

Refactor(F) is an experimental Roslyn-based C# source-analysis and refactoring subsystem within EVI-ID.

Its current workflow separates source discovery, candidate inspection, and transformation preview.

The subsystem prioritizes conservative validation over automatic modification.

Refactor(F) is not a general-purpose, language-independent refactoring engine.

## Workflow

A typical session consists of four operations:

    refactor Example.cs --discover
    refactor --candidates
    refactor --preview <candidate-id>
    refactor --clear

The discovery command identifies candidate regions.

The candidates command lists the candidates associated with the active session.

The preview command evaluates a selected candidate and may produce proposed transformed source.

The clear command discards the active session.

No command in this workflow applies a transformation to the original source file.

## Metadata References

Additional assembly references can be supplied during analysis or discovery:

    refactor Example.cs --refs System.Runtime.dll --discover

Multiple references are separated by commas:

    refactor Example.cs --refs System.Runtime.dll,System.Console.dll --discover

Reference names must identify DLL filenames rather than arbitrary filesystem paths.

The assemblies are resolved through the configured Android SAF reference directory.

Reference availability can affect semantic analysis and compilation diagnostics.

## Discovery Sessions

Discovery creates an in-memory session containing information about the analyzed sources and candidate regions.

Candidate identifiers are meaningful within the session that produced them.

The session is associated with the active EVI-ID file space.

Switching between Internal and External storage requires a new discovery session.

Discovery itself does not rewrite the analyzed source files.

## Preview Semantics

Preview is a read-only operation.

Before evaluating a candidate, the implementation re-reads source content and may reload metadata references.

A preview can be rejected or marked as requiring review when validation cannot establish the required conditions.

A proposed transformation must not be interpreted as a verified behavior-preserving rewrite merely because it compiles.

## Safety Principles

- Preserve original source files during discovery and preview.
- Reject unsupported transformations conservatively.
- Report validation limitations rather than silently assuming correctness.
- Treat compiler diagnostics as evidence, not proof of behavioral equivalence.
- Require independent review before applying proposed source changes.

## Experimental Status

Refactor(F) is under active development.

Its interfaces, diagnostics, supported transformations, and validation behavior may change.

The subsystem is not yet intended for unattended source modification.

## Semantic Validation

Refactor(F) uses Roslyn-based analysis to evaluate candidate transformations.

Semantic validation is intended to detect cases where a proposed transformation cannot be justified using the available source and compilation context.

Potential obstacles include:

- Unresolved external types or members.
- Missing metadata references.
- Invalid or unavailable source mappings.
- Unsupported statement structures.
- Preprocessor directive boundaries.
- Differences in compilation diagnostics.

A rejected or review-required candidate is not necessarily an incorrect program. It may represent a case that the current validator cannot establish as safe.

## Compilation Checks

The preview workflow can compare compilation diagnostics associated with original and proposed transformed source.

These checks help detect transformations that introduce compilation errors.

However, compilation checks do not establish that both versions:

- Produce identical runtime results.
- Preserve side-effect ordering.
- Throw the same exceptions.
- Have identical concurrency behavior.
- Preserve externally observable interactions.

Compilation is therefore a necessary check for many transformations, but not a sufficient proof of behavioral equivalence.

## Behavioral Equivalence

Behavioral equivalence is a stronger requirement than successful compilation.

Two programs may compile successfully while producing different results for the same inputs or environmental conditions.

Refactor(F) must not report a transformation as proven behavior-preserving solely because both versions compile.

When the available analysis cannot establish the required guarantees, the preview should communicate that limitation.

## Validation Outcomes

Preview results may include statuses indicating:

- A candidate passed the implemented checks.
- Additional review is required.
- The candidate or transformation is unsupported.
- A validation or compilation problem occurred.

The exact status identifiers and diagnostic fields are implementation-specific and may evolve.

A successful command-level exit status must not be interpreted as proof that a transformation is safe to apply.

## Preprocessor Directives

C# preprocessor directives can affect which source regions are compiled and how statements are interpreted.

Transformations involving directive boundaries require particular caution.

The current development tests include directive-related regression scenarios.

These tests are evidence for their specific cases, not proof that all directive configurations are supported.

## Validation Boundaries

Refactor(F) is an experimental implementation, not a formal verification system.

Its current validation is bounded by:

- Available source files.
- Resolved metadata references.
- Implemented syntax and semantic checks.
- Supported transformation patterns.
- Test coverage.
- The compilation context used for analysis.

Unknown or unsupported cases should remain visible to the user rather than being silently treated as safe.
