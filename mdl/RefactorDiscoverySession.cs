using System;
using System.Collections.Generic;
using Microsoft.CodeAnalysis;

namespace EVI_Refactor;

public sealed record RefactorCandidateDescriptor(
    string Id,
    string SourceName,
    string MethodName,
    int MethodSpanStart,
    int MethodSpanEnd,
    int GroupSpanStart,
    int GroupSpanEnd,
    int StatementCount,
    int StartLine,
    int EndLine);

public sealed class RefactorDiscoverySession
{
    private readonly RefactorGroupDiscovery _discovery;

    private readonly Dictionary<string, (
        RefactorDiscoveredGroup Candidate,
        RefactorCandidateDescriptor Descriptor)>
        _candidates = new(StringComparer.Ordinal);

    public string SessionId { get; }

    public string SourceSetHash =>
        _discovery.SourceSetHash;

    public IReadOnlyList<RefactorCandidateDescriptor>
        Candidates { get; }

    public RefactorDiscoverySession(
        RefactorGroupDiscovery discovery)
    {
        ArgumentNullException.ThrowIfNull(discovery);

        _discovery = discovery;
        SessionId = Guid.NewGuid().ToString("N");

        var descriptors =
            new List<RefactorCandidateDescriptor>();

        for (int index = 0;
             index < discovery.Groups.Count;
             index++)
        {
            var candidate = discovery.Groups[index];

            string id = $"{SessionId}:{index}";

            var descriptor =
                new RefactorCandidateDescriptor(
                    id,
                    candidate.SourceName,
                    candidate.MethodName,
                    candidate.MethodSpanStart,
                    candidate.MethodSpanEnd,
                    candidate.ExpectedSpanStart,
                    candidate.ExpectedSpanEnd,
                    candidate.ExpectedStatementCount,
                    candidate.Group.StartLine,
                    candidate.Group.EndLine);

            _candidates.Add(
                id, (candidate, descriptor));

            descriptors.Add(descriptor);
        }

        Candidates = descriptors.AsReadOnly();
    }

    public RefactorPreviewResult PreviewById(
        IReadOnlyList<(string Name, string Content)> sources,
        string candidateId,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (string.IsNullOrWhiteSpace(candidateId) ||
            !_candidates.TryGetValue(
                candidateId, out var entry))
        {
            return Rejected("CandidateIdNotFound");
        }

        var candidate = entry.Candidate;
        var descriptor = entry.Descriptor;
        var group = candidate.Group;

        if (group is null ||
            candidate.SourceName != descriptor.SourceName ||
            candidate.MethodName != descriptor.MethodName ||
            candidate.MethodSpanStart !=
                descriptor.MethodSpanStart ||
            candidate.MethodSpanEnd !=
                descriptor.MethodSpanEnd ||
            candidate.ExpectedSpanStart !=
                descriptor.GroupSpanStart ||
            candidate.ExpectedSpanEnd !=
                descriptor.GroupSpanEnd ||
            candidate.ExpectedStatementCount !=
                descriptor.StatementCount ||
            group.SpanStart != descriptor.GroupSpanStart ||
            group.SpanEnd != descriptor.GroupSpanEnd ||
            group.StatementCount != descriptor.StatementCount ||
            group.StartLine != descriptor.StartLine ||
            group.EndLine != descriptor.EndLine)
        {
            return Rejected("SessionCandidateStateMismatch");
        }

        return RefactorDiscoveredPreviewService
            .PreviewDiscoveredGroupVerified(
                sources,
                _discovery,
                candidate,
                references);
    }

    private static RefactorPreviewResult Rejected(
        string reason) =>
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
