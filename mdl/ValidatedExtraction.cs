using Microsoft.CodeAnalysis;

internal sealed record ValidatedExtraction(
    ValidationResult Validation,
    SyntaxTree? TransformedTree)
{
    public bool HasValidatedTree =>
        Validation.Status == "ReviewRequired" &&
        Validation.OriginalCompilationChecked &&
        Validation.TransformedCompilationChecked &&
        Validation.OriginalErrors == 0 &&
        Validation.TransformedErrors == 0 &&
        TransformedTree is not null;
}
