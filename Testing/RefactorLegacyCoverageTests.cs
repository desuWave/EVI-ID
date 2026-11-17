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
    public static void RunLegacyCoverageTests(
        List<UnitTestResult> results)
    {
        const string source =
            "class C { void M() { MissingWidget x = null; x = null; } }";

        var tree = CSharpSyntaxTree.ParseText(
            source, path: "Smoke.cs");

        var method = tree.GetRoot()
            .DescendantNodes()
            .OfType<MethodDeclarationSyntax>()
            .Single();

        var statements = method.Body!.Statements;

        var group = new RefactorGroup(
            1, 1, statements.Count, 1,
            "Review", "Review", "", "", false, "");

        group.SetSourceSpan(
            statements[0].SpanStart,
            statements[^1].Span.End);

        var runtime = MetadataReference.CreateFromFile(
            typeof(object).Assembly.Location);

        Run(results,
            "RefactorBridge: diagnostic location and message",
            () =>
            {
                var result = RefactorSemanticBridge.Evaluate(
                    tree, method, group);

                AssertTrue(
                    result.Diagnostics.Any(d =>
                        d.Id == "CS0246" &&
                        d.Line == 1 &&
                        d.Column > 0 &&
                        d.Path == "Smoke.cs" &&
                        d.Message.Length > 0),
                    "Missing located CS0246 diagnostic.");
            });

        Run(results,
            "RefactorBridge: core reference retains unresolved type",
            () =>
            {
                var result = RefactorSemanticBridge.Evaluate(
                    tree, method, group,
                    new[] { runtime });

                AssertEqual(
                    RefactorSafetyStatus.Unsupported,
                    result.Status);

                AssertTrue(
                    result.Reasons.Contains("Diagnostic:CS0246"),
                    "Core reference concealed unresolved type.");
            });

        Run(results,
            "RefactorEngine: located semantic diagnostic",
            () =>
            {
                string engineSource =
                    "class C\n{\n    void M()\n    {\n" +
                    "        MissingWidget x = null;\n" +
                    string.Concat(
                        Enumerable.Repeat(
                            "        x = null;\n", 21)) +
                    "    }\n}";

                string report = new RefactorEngine()
                    .AnalyzeSource(
                        "Smoke.cs",
                        engineSource,
                        maxMethodLines: 1,
                        references: new[] { runtime });

                AssertTrue(
                    report.Contains(
                        "SemanticDiagnostic: CS0246 (Smoke.cs:"),
                    "Located diagnostic missing from engine report.");

                AssertTrue(
                    report.Contains(
                        "SemanticSafety: Unsupported"),
                    "Diagnostic unexpectedly relaxed safety.");
            });
    }
}
