using System;

namespace EVI_Refactor;

/// <summary>
/// Read-only result of a proposed source transformation.
/// Does not write changes to the source file.
/// </summary>
public sealed record RefactorPreviewResult(
    string Status,
    string Reason,
    string OriginalSource,
    string? TransformedSource,
    bool OriginalCompilationChecked,
    bool TransformedCompilationChecked,
    int? OriginalErrors,
    int? TransformedErrors)
{
    public bool HasPreview =>
        TransformedSource is not null &&
        !string.Equals(
            OriginalSource,
            TransformedSource,
            StringComparison.Ordinal);

    public bool RequiresReview =>
        Status == "ReviewRequired";
}
