using System;
using System.Linq;
using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class RefactorV7BridgeTests
{
    public static void Run()
    {
        Check(
            "Simple extraction",
            """
            class Example
            {
                void Run()
                {
                    System.Console.WriteLine("A");
                    System.Console.WriteLine("B");
                }
            }
            """,
            "ReviewRequired",
            "BehavioralEquivalenceNotProven");

        Check(
            "Caller-info dependency",
            """
            using System.Runtime.CompilerServices;

            class Example
            {
                static void Log(
                    [CallerMemberName] string name = "")
                {
                    System.Console.WriteLine(name);
                }

                void Run()
                {
                    Log();
                    System.Console.WriteLine("B");
                }
            }
            """,
            "Unsupported",
            "ImplicitCallerInfoDependency");

        Check(
            "Compilation error",
            """
            class Example
            {
                void Run()
                {
                    MissingType.DoSomething();
                    System.Console.WriteLine("B");
                }
            }
            """,
            "Unsupported",
            "CompilationDiagnosticsPresent");
    }

    private static void Check(
        string name,
        string source,
        string expectedStatus,
        string expectedReason)
    {
        var tree = CSharpSyntaxTree.ParseText(source);
        var method = tree.GetCompilationUnitRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single(m => m.Identifier.ValueText == "Run");

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

        group.SetSourceSpan(
            first.SpanStart,
            last.Span.End);

        var references = ((string?)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(System.IO.Path.PathSeparator)
            .Select(path =>
                MetadataReference.CreateFromFile(path))
            .ToArray()
            ?? throw new InvalidOperationException(
                "Runtime references unavailable.");

        var result = RefactorSemanticBridge.Evaluate(
            tree,
            method,
            group,
            references);

        bool passed =
            result.Status.ToString() == expectedStatus &&
            result.Reasons.Contains(expectedReason);

        if (!passed)
        {
            throw new InvalidOperationException(
                name + ": expected " +
                expectedStatus + "/" + expectedReason +
                ", got " + result.Status + "/" +
                string.Join(",", result.Reasons));
        }

        Console.WriteLine(name + ": PASS");
        Console.WriteLine("  Status: " + result.Status);
        Console.WriteLine("  Reasons: " +
            string.Join(",", result.Reasons));
    }
}
