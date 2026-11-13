using System;
using System.Collections.Generic;
using System.Linq;
using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RefactorPreviewApi
{
    public static RefactorPreviewResult Preview(
        CSharpCompilation compilation,
        MethodDeclarationSyntax method,
        IReadOnlyList<StatementSyntax> statements)
    {
        if (method is null)
        {
            return new RefactorPreviewResult(
                "Unsupported", "InvalidInput",
                string.Empty, null,
                false, false, null, null);
        }

        var originalTree = method.SyntaxTree;

        string originalSource =
            originalTree?.GetText().ToString()
            ?? string.Empty;

        var validated =
            SemanticExtractionValidator.ValidateWithTree(
                compilation, method, statements);

        var validation = validated.Validation;

        RefactorPreviewResult Result(
            string? transformedSource) =>
            new(
                validation.Status,
                validation.Reason,
                originalSource,
                transformedSource,
                validation.OriginalCompilationChecked,
                validation.TransformedCompilationChecked,
                validation.OriginalErrors,
                validation.TransformedErrors);

        if (validation.Status != "ReviewRequired" ||
            !validation.OriginalCompilationChecked ||
            !validation.TransformedCompilationChecked ||
            validation.OriginalErrors != 0 ||
            validation.TransformedErrors != 0)
        {
            return Result(null);
        }

        if (originalTree is null)
        {
            return new RefactorPreviewResult(
                "Unsupported", "InvalidInput",
                originalSource, null,
                false, false, null, null);
        }

        if (!validated.HasValidatedTree ||
            validated.TransformedTree is not { } transformedTree)
        {
            return Result(null);
        }

        string transformedSource =
            transformedTree.GetText().ToString();

        var reparsedTree =
            CSharpSyntaxTree.ParseText(
                transformedSource,
                options: transformedTree.Options
                    as CSharpParseOptions);

        bool syntaxValid =
            !reparsedTree.GetDiagnostics().Any(
                d => d.Severity ==
                    DiagnosticSeverity.Error);

        if (!syntaxValid)
        {
            return new RefactorPreviewResult(
                "Unsupported",
                "PreviewSyntaxRoundtripFailed",
                originalSource, null,
                true, true,
                validation.OriginalErrors, null);
        }

        var reparsedCompilation =
            compilation.ReplaceSyntaxTree(
                originalTree, reparsedTree);

        int compilationErrors =
            reparsedCompilation.GetDiagnostics().Count(
                d => d.Severity ==
                    DiagnosticSeverity.Error);

        if (compilationErrors != 0)
        {
            return new RefactorPreviewResult(
                "Unsupported",
                "PreviewCompilationRoundtripFailed",
                originalSource, null,
                true, true,
                validation.OriginalErrors,
                compilationErrors);
        }

        return Result(transformedSource);
    }
}
