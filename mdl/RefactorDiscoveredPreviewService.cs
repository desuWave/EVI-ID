using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_Refactor;

public static class RefactorDiscoveredPreviewService
{
    public static RefactorPreviewResult PreviewDiscoveredGroupVerified(
        IReadOnlyList<(string Name, string Content)> sources,
        RefactorGroupDiscovery discovery,
        RefactorDiscoveredGroup candidate,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (discovery is null || candidate is null)
            return Rejected("InvalidDiscovery");

        if (!discovery.Groups.Any(g =>
            ReferenceEquals(g, candidate)))
        {
            return Rejected("CandidateNotInDiscovery");
        }

        if (string.IsNullOrWhiteSpace(discovery.SourceSetHash))
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
            discovery.SourceSetHash,
            StringComparison.Ordinal))
        {
            return Rejected("SourceSetMismatch");
        }

        if (string.IsNullOrWhiteSpace(candidate.SourceName) ||
            string.IsNullOrWhiteSpace(candidate.MethodName))
        {
            return Rejected("InvalidInput");
        }

        if (candidate.MethodSpanStart < 0 ||
            candidate.MethodSpanEnd <= candidate.MethodSpanStart)
        {
            return Rejected("InvalidExpectedMethod");
        }

        if (candidate.ExpectedSpanStart < 0 ||
            candidate.ExpectedSpanEnd <= candidate.ExpectedSpanStart ||
            candidate.ExpectedStatementCount <= 0)
        {
            return Rejected("InvalidExpectedGroup");
        }

        if (candidate.Group is null)
            return Rejected("InvalidGroup");

        if (candidate.Group.SpanStart != candidate.ExpectedSpanStart ||
            candidate.Group.SpanEnd != candidate.ExpectedSpanEnd ||
            candidate.Group.StatementCount !=
                candidate.ExpectedStatementCount)
        {
            return Rejected("GroupMismatch");
        }

        var matchingSources = sources
            .Where(s => string.Equals(
                s.Name,
                candidate.SourceName,
                StringComparison.Ordinal))
            .ToArray();

        if (matchingSources.Length != 1)
        {
            return Rejected(
                matchingSources.Length == 0
                    ? "SourceNotFound"
                    : "DuplicateSourceName");
        }

        var tree = CSharpSyntaxTree.ParseText(
            matchingSources[0].Content,
            path: candidate.SourceName);

        var methods = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Where(method =>
                method.Identifier.ValueText == candidate.MethodName &&
                method.SpanStart == candidate.MethodSpanStart &&
                method.Span.End == candidate.MethodSpanEnd)
            .ToArray();

        if (methods.Length != 1)
        {
            return Rejected(
                methods.Length == 0
                    ? "MethodIdentityMismatch"
                    : "AmbiguousMethod");
        }

        var methodNode = methods[0];

        if (methodNode.Body is null ||
            methodNode.Body.SpanStart > candidate.ExpectedSpanStart ||
            methodNode.Body.Span.End < candidate.ExpectedSpanEnd)
        {
            return Rejected("GroupOutsideMethod");
        }

        return RefactorPreviewService.PreviewGroupVerified(
            sources,
            discovery.SourceSetHash,
            candidate.SourceName,
            candidate.MethodName,
            candidate.Group,
            candidate.ExpectedSpanStart,
            candidate.ExpectedSpanEnd,
            candidate.ExpectedStatementCount,
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
}
