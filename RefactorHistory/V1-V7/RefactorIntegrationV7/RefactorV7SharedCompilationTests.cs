using System;
using System.Linq;
using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RefactorV7SharedCompilationTests
{
    public static void Run()
    {
        const string source = """
            class Example
            {
                void Run()
                {
                    System.Console.WriteLine("A");
                    System.Console.WriteLine("B");
                }
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);
        var otherTree = CSharpSyntaxTree.ParseText(source);

        var method = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        var statements = method.Body!.Statements;

        var first = statements[0];
        var last = statements[^1];

        var group = new RefactorGroup(
            first.GetLocation().GetLineSpan()
                .StartLinePosition.Line + 1,
            last.GetLocation().GetLineSpan()
                .EndLinePosition.Line + 1,
            statements.Count,
            statements.Count,
            "ExtractMethod",
            "ReviewRequired",
            "TestCandidate",
            "None",
            false,
            "None");

        group.SetSourceSpan(first.SpanStart, last.Span.End);

        var compilation = CSharpCompilation.Create(
            "SharedCompilationTest",
            new[] { otherTree },
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var result = RefactorSemanticBridge.Evaluate(
            tree, method, group, compilation);

        if (result.Status != RefactorSafetyStatus.Unsupported ||
            !result.Reasons.Contains("CompilationTreeMismatch"))
        {
            throw new InvalidOperationException(
                "Tree mismatch guard failed: " +
                result.Status + "/" +
                string.Join(",", result.Reasons));
        }

        Console.WriteLine("Compilation tree mismatch: PASS");
    }
}
