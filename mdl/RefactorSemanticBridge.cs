using System;
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
        RefactorGroup group)
    {
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

        StatementSyntax[] statements;

        if (group.SpanStart >= 0 ||
            group.SpanEnd >= 0)
        {
            if (group.SpanStart < 0 ||
                group.SpanEnd <= group.SpanStart)
            {
                return Unsupported("InvalidSourceSpan");
            }

            statements = method.Body.Statements
                .Where(s =>
                    s.SpanStart >= group.SpanStart &&
                    s.Span.End <= group.SpanEnd)
                .ToArray();

            if (statements.Length == 0 ||
                statements[0].SpanStart != group.SpanStart ||
                statements[^1].Span.End != group.SpanEnd)
            {
                return Unsupported(
                    "StatementMappingUnavailable");
            }
        }
        else
        {
            statements = method.Body.Statements
                .Where(statement =>
                {
                    var span = statement
                        .GetLocation()
                        .GetLineSpan();

                    int start =
                        span.StartLinePosition.Line + 1;

                    int end =
                        span.EndLinePosition.Line + 1;

                    return start >= group.StartLine &&
                           end <= group.EndLine;
                })
                .ToArray();
        }

        if (statements.Length != group.StatementCount)
        {
            return Unsupported(
                "StatementMappingUnavailable");
        }

        try
        {
            var compilation = CSharpCompilation.Create(
                "EVIRefactorSemanticAnalysis",
                new[] { tree });

            var model =
                compilation.GetSemanticModel(tree);

            var result =
                RefactorSafetyAnalyzer.Analyze(
                    model,
                    statements);

            bool hasCompilationErrors =
                compilation.GetDiagnostics()
                    .Any(d =>
                        d.Severity ==
                        DiagnosticSeverity.Error);

            if (!hasCompilationErrors)
            {
                return result;
            }

            bool syntaxErrors = tree.GetDiagnostics()
                .Any(d => d.Severity ==
                    DiagnosticSeverity.Error);

            bool unresolvedSymbols =
                compilation.GetDiagnostics().Any(d =>
                    d.Severity == DiagnosticSeverity.Error &&
                    (d.Id == "CS0246" ||
                     d.Id == "CS0234" ||
                     d.Id == "CS0012" ||
                     d.Id == "CS0518" ||
                     d.Id == "CS1069"));

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
                .Distinct()
                .ToArray();

            return new RefactorSafetyResult(
                result.Status ==
                    RefactorSafetyStatus.Unsupported
                    ? RefactorSafetyStatus.Unsupported
                    : RefactorSafetyStatus.ReviewRequired,
                reasons);
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
