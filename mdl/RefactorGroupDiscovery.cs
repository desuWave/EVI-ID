using System;
using System.Collections.Generic;

namespace EVI_Refactor;

public sealed record RefactorDiscoveredGroup(
    string SourceName,
    string MethodName,
    int MethodSpanStart,
    int MethodSpanEnd,
    RefactorGroup Group,
    int ExpectedSpanStart,
    int ExpectedSpanEnd,
    int ExpectedStatementCount);

public sealed class RefactorGroupDiscovery
{
    public string SourceSetHash { get; }

    public IReadOnlyList<RefactorDiscoveredGroup> Groups { get; }

    internal RefactorGroupDiscovery(
        string sourceSetHash,
        IReadOnlyList<RefactorDiscoveredGroup> groups)
    {
        ArgumentNullException.ThrowIfNull(sourceSetHash);
        ArgumentNullException.ThrowIfNull(groups);

        SourceSetHash = sourceSetHash;

        var snapshot = new List<RefactorDiscoveredGroup>(groups);
        Groups = snapshot.AsReadOnly();
    }
}
