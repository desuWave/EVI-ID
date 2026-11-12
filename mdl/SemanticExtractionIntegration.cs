using System.Collections.Generic;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class SemanticExtractionIntegration
{
    public static ValidationResult Validate(
        CSharpCompilation compilation,
        MethodDeclarationSyntax method,
        IReadOnlyList<StatementSyntax> statements)
    {
        return SemanticExtractionValidator.Validate(
            compilation,
            method,
            statements);
    }
}
