using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_Refactor;

public static class RefactorPreviewService
{
    public static RefactorPreviewResult PreviewGroupVerified(
        IReadOnlyList<(string Name, string Content)> sources,
        string expectedSourceSetHash,
        string sourceName,
        string methodName,
        RefactorGroup group,
        int expectedSpanStart,
        int expectedSpanEnd,
        int expectedStatementCount,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (group is null)
            return Rejected("InvalidGroup");

        if (string.IsNullOrWhiteSpace(expectedSourceSetHash))
            return Rejected("InvalidSourceSetHash");

        string actualHash;

        try
        {
            actualHash = RefactorSourceIdentity.Compute(sources);
        }
        catch (ArgumentException)
        {
            return Rejected("InvalidSourceSet");
        }

        if (!string.Equals(
            actualHash,
            expectedSourceSetHash,
            StringComparison.Ordinal))
            return Rejected("SourceSetMismatch");

        if (expectedSpanStart < 0 ||
            expectedSpanEnd <= expectedSpanStart ||
            expectedStatementCount <= 0)
            return Rejected("InvalidExpectedGroup");

        if (group.SpanStart != expectedSpanStart ||
            group.SpanEnd != expectedSpanEnd ||
            group.StatementCount != expectedStatementCount)
            return Rejected("GroupMismatch");

        if (string.IsNullOrWhiteSpace(sourceName) ||
            string.IsNullOrWhiteSpace(methodName))
            return Rejected("InvalidInput");

        var orderedSources = sources
            .OrderBy(s => s.Name, StringComparer.Ordinal)
            .ToArray();

        int index = Array.FindIndex(
            orderedSources,
            s => string.Equals(
                s.Name, sourceName, StringComparison.Ordinal));

        if (index < 0)
            return Rejected("SourceNotFound");

        var trees = orderedSources
            .Select(s => CSharpSyntaxTree.ParseText(
                s.Content, path: s.Name))
            .ToArray();

        var tree = trees[index];

        var methods = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(m =>
                m.Identifier.ValueText == methodName &&
                m.Body is not null &&
                m.Body.SpanStart <= expectedSpanStart &&
                m.Body.Span.End >= expectedSpanEnd)
            .ToArray();

        if (methods.Length != 1)
            return Rejected(methods.Length == 0
                ? "MethodNotFound"
                : "AmbiguousMethod");

        if (!RefactorStatementMapper.TryMap(
            methods[0],
            group,
            out var statements,
            out string mappingReason))
            return Rejected(mappingReason);

        var compilation = CSharpCompilation.Create(
            "EVIRefactorGroupPreview",
            trees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        return RefactorPreviewApi.Preview(
            compilation,
            methods[0],
            statements);
    }

    public static RefactorPreviewResult PreviewVerified(
        IReadOnlyList<(string Name, string Content)> sources,
        string expectedSourceSetHash,
        string sourceName,
        string methodName,
        int spanStart,
        int spanEnd,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (string.IsNullOrWhiteSpace(expectedSourceSetHash))
            return Rejected("InvalidSourceSetHash");

        string actualHash;

        try
        {
            actualHash = RefactorSourceIdentity.Compute(sources);
        }
        catch (ArgumentException)
        {
            return Rejected("InvalidSourceSet");
        }

        if (!string.Equals(
            actualHash,
            expectedSourceSetHash,
            StringComparison.Ordinal))
        {
            return Rejected("SourceSetMismatch");
        }

        return Preview(
            sources,
            sourceName,
            methodName,
            spanStart,
            spanEnd,
            references);
    }

    private static RefactorPreviewResult Rejected(string reason) =>
        new(
            "Unsupported",
            reason,
            string.Empty,
            null,
            false,
            false,
            null,
            null);

    public static RefactorPreviewResult Preview(
        IReadOnlyList<(string Name, string Content)> sources,
        string sourceName,
        string methodName,
        int spanStart,
        int spanEnd,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        RefactorPreviewResult Unsupported(string reason) =>
            new(
                "Unsupported",
                reason,
                string.Empty,
                null,
                false,
                false,
                null,
                null);

        if (sources.Count == 0 ||
            string.IsNullOrWhiteSpace(sourceName) ||
            string.IsNullOrWhiteSpace(methodName) ||
            spanStart < 0 ||
            spanEnd <= spanStart)
        {
            return Unsupported("InvalidInput");
        }

        if (sources.Any(source =>
                string.IsNullOrWhiteSpace(source.Name) ||
                source.Content is null))
        {
            return Unsupported("InvalidSource");
        }

        if (sources.Select(source => source.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != sources.Count)
        {
            return Unsupported("DuplicateSourceName");
        }

        var orderedSources = sources
            .OrderBy(source => source.Name, StringComparer.Ordinal)
            .ToArray();

        var trees = orderedSources
            .Select(source => CSharpSyntaxTree.ParseText(
                source.Content,
                path: source.Name))
            .ToArray();

        var compilation = CSharpCompilation.Create(
            "EVIRefactorPreview",
            trees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        int sourceIndex = Array.FindIndex(
            orderedSources,
            source => string.Equals(
                source.Name,
                sourceName,
                StringComparison.Ordinal));

        if (sourceIndex < 0)
            return Unsupported("SourceNotFound");

        var tree = trees[sourceIndex];

        var methods = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
                method.Identifier.ValueText == methodName &&
                method.Body is not null &&
                method.Body.SpanStart <= spanStart &&
                method.Body.Span.End >= spanEnd)
            .ToArray();

        if (methods.Length != 1)
        {
            return Unsupported(
                methods.Length == 0
                    ? "MethodNotFound"
                    : "AmbiguousMethod");
        }

        var methodNode = methods[0];

        var statements = methodNode.Body!.Statements
            .Where(statement =>
                statement.SpanStart >= spanStart &&
                statement.Span.End <= spanEnd)
            .ToArray();

        if (statements.Length == 0 ||
            statements[0].SpanStart != spanStart ||
            statements[^1].Span.End != spanEnd)
        {
            return Unsupported("StatementMappingUnavailable");
        }

        int firstIndex =
            methodNode.Body.Statements.IndexOf(statements[0]);

        for (int i = 0; i < statements.Length; i++)
        {
            if (firstIndex < 0 ||
                firstIndex + i >= methodNode.Body.Statements.Count ||
                !ReferenceEquals(
                    methodNode.Body.Statements[firstIndex + i],
                    statements[i]))
            {
                return Unsupported("StatementMappingUnavailable");
            }
        }

        return RefactorPreviewApi.Preview(
            compilation,
            methodNode,
            statements);
    }
}
