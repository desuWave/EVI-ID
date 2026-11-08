using System;
using System.Linq;
using EVI_Refactor;

internal static class RefactorV7ResultAdapterTests
{
    public static void Run()
    {
        var diagnostic = new RefactorDiagnosticDetail(
            "CS0246", "Missing type", "Example.cs", 4, 2);

        var existing = new RefactorSafetyResult(
            RefactorSafetyStatus.ReviewRequired,
            new[] { "SemanticValidationRequired" },
            new[] { diagnostic });

        Check(
            "V7 review",
            existing,
            "ReviewRequired",
            RefactorSafetyStatus.ReviewRequired);

        Check(
            "V7 rejection",
            existing,
            "Unsupported",
            RefactorSafetyStatus.Unsupported);

        Check(
            "V7 safe cannot promote",
            existing,
            "Safe",
            RefactorSafetyStatus.ReviewRequired);

        Check(
            "Unknown status",
            existing,
            "Unexpected",
            RefactorSafetyStatus.Unsupported);

        var unsupported = new RefactorSafetyResult(
            RefactorSafetyStatus.Unsupported,
            new[] { "ExistingRejection" });

        Check(
            "Existing rejection preserved",
            unsupported,
            "ReviewRequired",
            RefactorSafetyStatus.Unsupported);
    }

    private static void Check(
        string name,
        RefactorSafetyResult existing,
        string validationStatus,
        RefactorSafetyStatus expected)
    {
        var validation = new ValidationResult(
            validationStatus,
            "V7Reason",
            true,
            true,
            0,
            0);

        var result = RefactorV7ResultAdapter.Merge(
            existing,
            validation);

        bool passed =
            result.Status == expected &&
            result.Reasons.Contains("V7Reason") &&
            existing.Reasons.All(result.Reasons.Contains) &&
            existing.Diagnostics.SequenceEqual(result.Diagnostics);

        if (!passed)
            throw new InvalidOperationException(
                "FAILED: " + name);

        Console.WriteLine(name + ": PASS");
    }
}
