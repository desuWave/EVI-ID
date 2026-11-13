using System;
using System.Collections.Generic;
using System.Linq;
using EVI_Refactor;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RefactorStatementMapper
{
    public static bool TryMap(
        MethodDeclarationSyntax? method,
        RefactorGroup? group,
        out IReadOnlyList<StatementSyntax> statements,
        out string reason)
    {
        statements = Array.Empty<StatementSyntax>();
        reason = "InvalidInput";

        if (method?.Body is null || group is null)
            return false;

        var body = method.Body.Statements;

        if (group.SpanStart >= 0 ||
            group.SpanEnd >= 0)
        {
            if (group.SpanStart < 0 ||
                group.SpanEnd <= group.SpanStart)
            {
                reason = "InvalidSourceSpan";
                return false;
            }

            var selected = body
                .Where(s =>
                    s.SpanStart >= group.SpanStart &&
                    s.Span.End <= group.SpanEnd)
                .ToArray();

            if (selected.Length == 0 ||
                selected[0].SpanStart != group.SpanStart ||
                selected[^1].Span.End != group.SpanEnd)
            {
                reason = "StatementMappingUnavailable";
                return false;
            }

            statements = selected;
        }
        else
        {
            statements = body
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

        if (statements.Count != group.StatementCount)
        {
            statements = Array.Empty<StatementSyntax>();
            reason = "StatementMappingUnavailable";
            return false;
        }

        if (statements.Count == 0)
        {
            reason = "None";
            return true;
        }

        int firstIndex = body.IndexOf(statements[0]);

        for (int i = 0; i < statements.Count; i++)
        {
            if (firstIndex < 0 ||
                firstIndex + i >= body.Count ||
                !ReferenceEquals(
                    body[firstIndex + i],
                    statements[i]))
            {
                statements = Array.Empty<StatementSyntax>();
                reason = "StatementMappingUnavailable";
                return false;
            }
        }

        reason = "None";
        return true;
    }
}
