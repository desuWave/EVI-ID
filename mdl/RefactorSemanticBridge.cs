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

        var statements = method.Body.Statements
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

        if (statements.Length != group.StatementCount)
        {
            return Unsupported("StatementMappingUnavailable");
        }

        try
        {
            var compilation = CSharpCompilation.Create(
                "EVIRefactorSemanticAnalysis",
                new[] { tree });

            var model =
                compilation.GetSemanticModel(tree);

            return RefactorSafetyAnalyzer.Analyze(
                model,
                statements);
        }
        catch (Exception)
        {
            return Unsupported("SemanticAnalysisUnavailable");
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
