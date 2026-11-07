using System;
using System.Collections.Generic;
using System.Linq;
using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorSafetyAnalyzerTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "RefactorSafety: independent statements",
            () =>
            {
                var result = AnalyzeSafety(
                    "class Example {" +
                    " void Run() {" +
                    " int a = 1;" +
                    " int b = 2;" +
                    " } }",
                    0,
                    2);

                AssertTrue(
                    result.Status ==
                    RefactorSafetyStatus.ReviewRequired,
                    "Unexpected safety status.");

                AssertTrue(
                    result.Reasons.Contains(
                        "SemanticValidationRequired"),
                    "Missing semantic validation reason.");
            });

        Run(
            results,
            "RefactorSafety: external output",
            () =>
            {
                var result = AnalyzeSafety(
                    "class Example {" +
                    " void Run() {" +
                    " int a = 1;" +
                    " a = a + 2;" +
                    " System.Console.WriteLine(a);" +
                    " } }",
                    0,
                    2);

                AssertTrue(
                    result.Reasons.Contains(
                        "ExternalOutputs"),
                    "External output was not detected.");
            });

        Run(
            results,
            "RefactorSafety: return statement",
            () =>
            {
                var result = AnalyzeSafety(
                    "class Example {" +
                    " int Run() {" +
                    " int a = 1;" +
                    " return a;" +
                    " } }",
                    0,
                    2);

                AssertTrue(
                    result.Reasons.Contains(
                        "ControlFlowRequiresReview"),
                    "Return statement was not detected.");
            });

        Run(
            results,
            "RefactorSafety: empty selection",
            () =>
            {
                var tree = CSharpSyntaxTree.ParseText(
                    "class Example {}");

                var compilation =
                    CSharpCompilation.Create(
                        "SafetyTest",
                        new[] { tree });

                var model =
                    compilation.GetSemanticModel(tree);

                var result =
                    RefactorSafetyAnalyzer.Analyze(
                        model,
                        Array.Empty<StatementSyntax>());

                AssertTrue(
                    result.Status ==
                    RefactorSafetyStatus.Unsupported,
                    "Empty selection should be unsupported.");
            });
    }

    private static RefactorSafetyResult AnalyzeSafety(
        string source,
        int startIndex,
        int count)
    {
        var tree =
            CSharpSyntaxTree.ParseText(
                source,
                path: "SafetyTest.cs");

        var compilation =
            CSharpCompilation.Create(
                "SafetyTest",
                new[] { tree },
                references: Array.Empty<MetadataReference>());

        var model =
            compilation.GetSemanticModel(tree);

        var method =
            tree.GetRoot()
                .DescendantNodes()
                .OfType<MethodDeclarationSyntax>()
                .First();

        var statements =
            method.Body!.Statements
                .Skip(startIndex)
                .Take(count)
                .ToArray();

        return RefactorSafetyAnalyzer.Analyze(
            model,
            statements);
    }
}
