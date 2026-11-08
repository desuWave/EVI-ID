using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class V7IntegrationRegression
{
    public static bool Run()
    {
        string first = """
            public class Probe
            {
                public void Test()
                {
                    External.Ping();
                }
            }
            """;

        string second = """
            public static class External
            {
                public static void Ping() {}
            }
            """;

        var tree1 = CSharpSyntaxTree.ParseText(
            first, path: "Probe.cs");

        var tree2 = CSharpSyntaxTree.ParseText(
            second, path: "External.cs");

        var paths = ((string?)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(Path.PathSeparator)
            ?? throw new InvalidOperationException(
                "Runtime references unavailable.");

        var references = paths
            .Select(p => MetadataReference.CreateFromFile(p));

        var compilation = CSharpCompilation.Create(
            "V7IntegrationRegression",
            new[] { tree1, tree2 },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var method = tree1.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        var statement = method.Body!.Statements[0];

        var positive =
            SemanticExtractionIntegration.Validate(
                compilation,
                method,
                new[] { statement });

        bool positiveOk =
            positive.Status == "ReviewRequired" &&
            positive.Reason ==
                "BehavioralEquivalenceNotProven" &&
            positive.OriginalCompilationChecked &&
            positive.TransformedCompilationChecked &&
            positive.OriginalErrors == 0 &&
            positive.TransformedErrors == 0;

        Console.WriteLine(
            "V7 multifile extraction: " +
            (positiveOk ? "PASS" : "FAIL"));

        Console.WriteLine(
            $"  Status: {positive.Status}");

        Console.WriteLine(
            $"  Reason: {positive.Reason}");

        var foreignStatement = tree2.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single()
            .Body!.Statements
            .FirstOrDefault();

        // A separate syntax tree must not be accepted
        // as a statement belonging to Probe.Test.
        var negative =
            SemanticExtractionIntegration.Validate(
                compilation,
                method,
                new[] {
                    foreignStatement ??
                    SyntaxFactory.ParseStatement("int x = 1;")
                });

        bool negativeOk =
            negative.Status == "Unsupported" &&
            negative.Reason == "InvalidStatementRange" &&
            !negative.OriginalCompilationChecked &&
            !negative.TransformedCompilationChecked;

        Console.WriteLine(
            "V7 foreign statement rejection: " +
            (negativeOk ? "PASS" : "FAIL"));

        Console.WriteLine(
            $"  Status: {negative.Status}");

        Console.WriteLine(
            $"  Reason: {negative.Reason}");

        Console.WriteLine(
            $"V7 INTEGRATION RESULT: " +
            $"{(positiveOk ? 1 : 0) + (negativeOk ? 1 : 0)}/2 PASS");

        return positiveOk && negativeOk;
    }
}
