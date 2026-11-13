using System.Collections.Generic;
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
        foreach (var node in nodes)
        {
            if (node is InvocationExpressionSyntax invocation)
            {
                if (model.GetOperation(invocation)
                    is not IInvocationOperation operation)
                    return true;

                if (HasCallerInfoDefault(operation.Arguments))
                    return true;
            }
            else if (node is ObjectCreationExpressionSyntax creation)
            {
                if (model.GetOperation(creation)
                    is not IObjectCreationOperation operation)
                    return true;

                if (HasCallerInfoDefault(operation.Arguments))
                    return true;
            }
            else if (node is ImplicitObjectCreationExpressionSyntax implicitCreation)
            {
                if (model.GetOperation(implicitCreation)
                    is not IObjectCreationOperation operation)
                    return true;

                if (HasCallerInfoDefault(operation.Arguments))
                    return true;
            }
        }

        return false;
    }

    private static bool HasCallerInfoDefault(
        IEnumerable<IArgumentOperation> arguments)
    {
        foreach (var argument in arguments)
        {
            if (argument.ArgumentKind != ArgumentKind.DefaultValue)
                continue;

            var parameter = argument.Parameter;

            if (parameter is null)
                return true;

            bool callerInfo = parameter.GetAttributes().Any(attribute =>
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

        return false;
    }
}
