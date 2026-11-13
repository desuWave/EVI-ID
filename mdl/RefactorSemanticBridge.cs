using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_Refactor;

public static class RefactorSemanticBridge
{
    public static RefactorSafetyResult Evaluate(
        SyntaxTree tree,
        MethodDeclarationSyntax method,
        RefactorGroup group,
        IEnumerable<MetadataReference>? references = null)
    {
        if (tree == null || method?.Body == null || group == null)
            return Unsupported("InvalidInput");

        var compilation = CSharpCompilation.Create(
            "EVIRefactorSemanticAnalysis",
            new[] { tree },
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        return Evaluate(tree, method, group, compilation);
    }

    public static RefactorSafetyResult Evaluate(
        SyntaxTree tree,
        MethodDeclarationSyntax method,
        RefactorGroup group,
        CSharpCompilation compilation)
    {
        if (compilation == null)
            return Unsupported("InvalidCompilation");

        if (tree != null &&
            !compilation.SyntaxTrees.Any(t =>
                ReferenceEquals(t, tree)))
            return Unsupported("CompilationTreeMismatch");

        if (tree == null ||
            method?.Body == null ||
            group == null)
        {
            return Unsupported("InvalidInput");
        }

        if (!ReferenceEquals(method.SyntaxTree, tree))
        {
            return Unsupported("SyntaxTreeMismatch");
        }

        if (!RefactorStatementMapper.TryMap(
                method,
                group,
                out var mappedStatements,
                out string mappingReason))
        {
            return Unsupported(mappingReason);
        }

        StatementSyntax[] statements =
            mappedStatements.ToArray();
        try
        {

            var model =
                compilation.GetSemanticModel(tree);

            var result =
                RefactorSafetyAnalyzer.Analyze(
                    model,
                    statements);

            var errors = compilation.GetDiagnostics()
                .Where(d => d.Severity == DiagnosticSeverity.Error)
                .ToArray();

            if (errors.Length == 0)
            {
                var validation =
                    SemanticExtractionIntegration.Validate(
                        compilation,
                        method,
                        statements);

                return RefactorV7ResultAdapter.Merge(
                    result,
                    validation);
            }

            bool syntaxErrors = tree.GetDiagnostics()
                .Any(d => d.Severity ==
                    DiagnosticSeverity.Error);

            bool unresolvedSymbols = errors.Any(d =>
                d.Id == "CS0246" || d.Id == "CS0234" ||
                d.Id == "CS0012" || d.Id == "CS0518" ||
                d.Id == "CS1069");

            var reasons = result.Reasons
                .Concat(new[]
                {
                    "CompilationDiagnosticsPresent"
                })
                .Concat(syntaxErrors
                    ? new[] { "SyntaxErrorsPresent" }
                    : Array.Empty<string>())
                .Concat(unresolvedSymbols
                    ? new[] { "UnresolvedSymbolsOrReferences" }
                    : Array.Empty<string>())
                .Concat(errors.Select(d => "Diagnostic:" + d.Id))
                .Distinct()
                .ToArray();

            var details = errors.Select(d =>
            {
                var span = d.Location.GetLineSpan();
                bool hasSource = d.Location.IsInSource;
                return new RefactorDiagnosticDetail(
                    d.Id,
                    d.GetMessage(System.Globalization.CultureInfo.InvariantCulture),
                    hasSource ? span.Path : "<compilation>",
                    hasSource ? span.StartLinePosition.Line + 1 : 0,
                    hasSource ? span.StartLinePosition.Character + 1 : 0);
            });

            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                reasons,
                details);
        }
        catch (Exception)
        {
            return Unsupported(
                "SemanticAnalysisUnavailable");
        }
    }

    private static RefactorSafetyResult Unsupported(
        string reason)
    {
        return new RefactorSafetyResult(
            RefactorSafetyStatus.Unsupported,
            new[] { reason });
    }
}
