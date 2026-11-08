using System;
using System.Linq;
using EVI_Refactor;

internal static class RefactorV7ResultAdapter
{
    public static RefactorSafetyResult Merge(
        RefactorSafetyResult existing,
        ValidationResult validation)
    {
        ArgumentNullException.ThrowIfNull(existing);
        ArgumentNullException.ThrowIfNull(validation);

        RefactorSafetyStatus status;

        if (existing.Status == RefactorSafetyStatus.Unsupported ||
            validation.Status == "Unsupported")
        {
            status = RefactorSafetyStatus.Unsupported;
        }
        else if (validation.Status == "ReviewRequired" ||
                 validation.Status == "Safe")
        {
            status = RefactorSafetyStatus.ReviewRequired;
        }
        else
        {
            status = RefactorSafetyStatus.Unsupported;
        }

        var reasons = existing.Reasons
            .Concat(new[] { validation.Reason })
            .Where(reason => !string.IsNullOrWhiteSpace(reason))
            .Distinct()
            .ToArray();

        if (validation.Status != "Unsupported" &&
            validation.Status != "ReviewRequired" &&
            validation.Status != "Safe")
        {
            reasons = reasons
                .Concat(new[] { "UnknownValidationStatus" })
                .ToArray();
        }

        return new RefactorSafetyResult(
            status,
            reasons,
            existing.Diagnostics);
    }
}
