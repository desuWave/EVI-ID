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
    private static void RunRefactorDirectiveBridgeTests(
        List<UnitTestResult> results) =>
        RunRefactorDirectiveBridgeTests(results, GetRegressionReferences());

    private static void RunRefactorDirectiveBridgeTests(
        List<UnitTestResult> results, IEnumerable<MetadataReference> references)
    {
        Run(
            results,
            "RefactorBridge: directive rejection propagation",
            () =>
            {
                const string source =
                    "class C\n" +
                    "{\n" +
                    "    void M()\n" +
                    "    {\n" +
                    "#if DEBUG\n" +
                    "        int a = 1;\n" +
                    "#endif\n" +
                    "        int b = 2;\n" +
                    "    }\n" +
                    "}\n";

                var options = new CSharpParseOptions(
                    preprocessorSymbols: new[] { "DEBUG" });

                var tree = CSharpSyntaxTree.ParseText(
                    source, options);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation = CSharpCompilation.Create(
                    "DirectiveBridgeRegression",
                    new[] { tree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));

                AssertEqual(
                    0,
                    compilation.GetDiagnostics().Count(
                        d => d.Severity ==
                            DiagnosticSeverity.Error));

                var statement = method.Body!.Statements[1];

                var group = new RefactorGroup(
                    1,
                    10,
                    1,
                    10,
                    "ExtractMethod",
                    "Candidate",
                    "None",
                    "None",
                    false,
                    "None");

                group.SetSourceSpan(
                    statement.SpanStart,
                    statement.Span.End);

                var result = RefactorSemanticBridge.Evaluate(
                    tree,
                    method,
                    group,
                    compilation);

                AssertEqual(
                    RefactorSafetyStatus.Unsupported,
                    result.Status);

                AssertTrue(
                    result.Reasons.Contains(
                        "PreprocessorDirectivesPresent"),
                    "Directive rejection reason was lost.");

                AssertTrue(
                    !result.Reasons.Contains(
                        "SemanticAnalysisUnavailable"),
                    "Bridge failed before returning validation.");
            });
    }
}
