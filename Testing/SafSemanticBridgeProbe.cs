using System;
using System.Linq;
using System.Text;
using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_ID.Testing;

public static class SafSemanticBridgeProbe
{
    public static string Run(
        PortableExecutableReference reference)
    {
        var output = new StringBuilder();

        output.AppendLine(
            "Roslyn SAF Semantic Bridge Probe");

        const string source =
            "class C { void M() { int x = 1; x++; } }";

        var tree = CSharpSyntaxTree.ParseText(source);

        var method = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        var statements = method.Body!.Statements;

        var group = new RefactorGroup(
            1, 1, statements.Count, 1,
            "CandidateForExtraction", "Candidate",
            "None", "None", false, "None");

        group.SetSourceSpan(
            statements[0].SpanStart,
            statements[^1].Span.End);

        var withoutReferences =
            RefactorSemanticBridge.Evaluate(
                tree, method, group);

        var withReference =
            RefactorSemanticBridge.Evaluate(
                tree, method, group,
                new MetadataReference[] { reference });

        PrintResult(
            output,
            "Without references",
            withoutReferences);

        PrintResult(
            output,
            "With System.Runtime.dll",
            withReference);

        return output.ToString();
    }

    private static void PrintResult(
        StringBuilder output,
        string label,
        RefactorSafetyResult result)
    {
        output.AppendLine(label);
        output.AppendLine(
            $"Status: {result.Status}");

        output.AppendLine(
            result.Reasons.Count == 0
                ? "Reasons: NONE"
                : "Reasons: " +
                  string.Join(", ", result.Reasons));
    }
}
