using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal sealed record SymbolBindingCheckResult(
    bool Preserved,
    string Reason,
    int CheckedBindings);

internal static class SymbolBindingPreservationValidator
{
    public static SymbolBindingCheckResult Validate(
        SemanticModel originalModel,
        SemanticModel transformedModel,
        IReadOnlyList<InvocationExpressionSyntax> originalInvocations,
        IReadOnlyList<InvocationExpressionSyntax> transformedInvocations)
    {
        if (originalInvocations.Count != transformedInvocations.Count)
        {
            return new(
                false,
                "InvocationCountChanged",
                0);
        }

        int checkedBindings = 0;

        for (int i = 0; i < originalInvocations.Count; i++)
        {
            var originalInfo =
                originalModel.GetSymbolInfo(originalInvocations[i]);

            var transformedInfo =
                transformedModel.GetSymbolInfo(transformedInvocations[i]);

            if (originalInfo.Symbol is not IMethodSymbol originalMethod ||
                transformedInfo.Symbol is not IMethodSymbol transformedMethod)
            {
                return new(
                    false,
                    "UnresolvedInvocationBinding",
                    checkedBindings);
            }

            if (!SameMethodIdentity(originalMethod, transformedMethod))
            {
                return new(
                    false,
                    "SymbolBindingChanged",
                    checkedBindings);
            }

            checkedBindings++;
        }

        return new(
            true,
            "SymbolBindingsPreserved",
            checkedBindings);
    }

    private static bool SameMethodIdentity(
        IMethodSymbol original,
        IMethodSymbol transformed)
    {
        if (SymbolEqualityComparer.Default.Equals(
            original, transformed))
        {
            return true;
        }

        if (original.MethodKind != transformed.MethodKind)
            return false;

        if (original.IsStatic != transformed.IsStatic)
            return false;

        if (original.Arity != transformed.Arity)
            return false;

        if (original.Parameters.Length != transformed.Parameters.Length)
            return false;

        if (original.ContainingType is null ||
            transformed.ContainingType is null)
            return false;

        if (original.MethodKind == MethodKind.LocalFunction ||
            transformed.MethodKind == MethodKind.LocalFunction)
            return false;

        if (!string.Equals(original.Name, transformed.Name,
            StringComparison.Ordinal))
            return false;

        for (int i = 0; i < original.Parameters.Length; i++)
        {
            var left = original.Parameters[i];
            var right = transformed.Parameters[i];

            if (left.RefKind != right.RefKind)
                return false;

            var leftType = left.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);

            var rightType = right.Type.ToDisplayString(
                SymbolDisplayFormat.FullyQualifiedFormat);

            if (!string.Equals(leftType, rightType,
                StringComparison.Ordinal))
                return false;
        }

        var originalIdentity = original.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);

        var transformedIdentity = transformed.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);

        if (!string.Equals(
            originalIdentity,
            transformedIdentity,
            StringComparison.Ordinal))
            return false;

        var originalAssembly =
            original.ContainingAssembly?.Identity;

        var transformedAssembly =
            transformed.ContainingAssembly?.Identity;

        return Equals(originalAssembly, transformedAssembly);
    }
}
