using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;
using EVI_Refactor;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorSemanticBridgeTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "RefactorBridge: matching statements",
            () =>
            {
                string source = MakeBridgeTestSource();

                var engine = new RefactorEngine();

                string report = engine.AnalyzeSource(
                    "BridgeTest.cs",
                    source,
                    maxMethodLines: 1);

                AssertTrue(
                    report.Contains("SemanticSafety: ReviewRequired"),
                    "Expected semantic review result.");

                AssertTrue(
                    report.Contains("SemanticReasons:"),
                    "Semantic reasons are missing.");
            });

        Run(
            results,
            "RefactorBridge: mismatched statements",
            () =>
            {
                string source = MakeBridgeTestSource();

                var tree = CSharpSyntaxTree.ParseText(
                    source,
                    path: "BridgeTest.cs");

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var statements = method.Body!.Statements;

                int start = statements[0]
                    .GetLocation()
                    .GetLineSpan()
                    .StartLinePosition.Line + 1;

                int end = statements[^1]
                    .GetLocation()
                    .GetLineSpan()
                    .EndLinePosition.Line + 1;

                var group = new RefactorGroup(
                    start,
                    end,
                    statements.Count + 1,
                    end - start + 1,
                    "CandidateForExtraction",
                    "Candidate",
                    "None",
                    "None",
                    false,
                    "None");

                var result =
                    RefactorSemanticBridge.Evaluate(
                        tree,
                        method,
                        group);

                AssertEqual(
                    RefactorSafetyStatus.Unsupported,
                    result.Status);

                AssertTrue(
                    result.Reasons.Contains(
                        "StatementMappingUnavailable"),
                    "Expected mapping failure reason.");
            });
    }

    private static string MakeBridgeTestSource()
    {
        var lines = new List<string>
        {
            "class BridgeTest",
            "{",
            "    void Run()",
            "    {"
        };

        for (int i = 0; i < 20; i++)
        {
            lines.Add(
                $"        int value{i} = {i};");
        }

        lines.Add("    }");
        lines.Add("}");

        return string.Join("\n", lines);
    }
}
