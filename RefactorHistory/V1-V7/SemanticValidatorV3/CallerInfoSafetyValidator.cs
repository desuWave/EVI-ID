using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using Microsoft.CodeAnalysis.Operations;

internal static class CallerInfoSafetyValidator
{
    public static bool HasImplicitCallerInfo(
        SemanticModel model,
        SyntaxNode[] nodes)
    {
        foreach (var invocation in nodes
            .OfType<InvocationExpressionSyntax>())
        {
            if (model.GetOperation(invocation)
                is not IInvocationOperation operation)
            {
                // Неизвестная семантика не считается безопасной.
                return true;
            }

            foreach (var argument in operation.Arguments)
            {
                if (argument.ArgumentKind !=
                    ArgumentKind.DefaultValue)
                    continue;

                var parameter = argument.Parameter;

                if (parameter is null)
                    return true;

                bool callerInfo =
                    parameter.GetAttributes().Any(attribute =>
                    {
                        string? name =
                            attribute.AttributeClass?.ToDisplayString();

                        return name is
                            "System.Runtime.CompilerServices.CallerMemberNameAttribute" or
                            "System.Runtime.CompilerServices.CallerFilePathAttribute" or
                            "System.Runtime.CompilerServices.CallerLineNumberAttribute" or
                            "System.Runtime.CompilerServices.CallerArgumentExpressionAttribute";
                    });

                if (callerInfo)
                    return true;
            }
        }

        return false;
    }
}
