using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorDirectiveBoundaryTests(
        List<UnitTestResult> results) =>
        RunRefactorDirectiveBoundaryTests(results, GetRegressionReferences());

    private static void RunRefactorDirectiveBoundaryTests(
        List<UnitTestResult> results, IEnumerable<MetadataReference> references)
    {
        const string before =
            "class C { void M() {\n" +
            "#if DEBUG\n" +
            "int a = 1;\n" +
            "#endif\n" +
            "int b = 2;\n" +
            "} }";

        const string after =
            "class C { void M() {\n" +
            "int a = 1;\n" +
            "#if DEBUG\n" +
            "int b = 2;\n" +
            "#endif\n" +
            "} }";

        const string boundary =
            "class C { void M() {\n" +
            "#if DEBUG\n" +
            "int a = 1;\n" +
            "#endif\n" +
            "} }";

        const string outside =
            "#region OutsideMethod\n" +
            "#endregion\n" +
            "class C { void M() { int a = 1; } }";

        RunBoundaryCase(
            results, "directives before selection",
            before, 1, true, false, references);

        RunBoundaryCase(
            results, "directives after selection",
            after, 0, true, false, references);

        RunBoundaryCase(
            results, "directives at selection boundary",
            boundary, 0, true, false, references);

        RunBoundaryCase(
            results, "directives outside method",
            outside, 0, false, false, references);

        RunBoundaryCase(
            results, "directive preview rejection",
            boundary, 0, true, true, references);
    }

    private static void RunBoundaryCase(
        List<UnitTestResult> results,
        string name,
        string source,
        int statementIndex,
        bool expectGuard,
        bool checkPreview, IEnumerable<MetadataReference> references)
    {
        Run(
            results,
            "RefactorV7: " + name,
            () =>
            {
                var options = new CSharpParseOptions(
                    preprocessorSymbols: new[] { "DEBUG" });

                var tree = CSharpSyntaxTree.ParseText(
                    source, options);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation = CSharpCompilation.Create(
                    "DirectiveBoundaryRegression",
                    new[] { tree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));

                AssertEqual(
                    0,
                    compilation.GetDiagnostics().Count(
                        d => d.Severity ==
                            DiagnosticSeverity.Error));

                var selected = new[]
                {
                    method.Body!.Statements[statementIndex]
                };

                var result =
                    SemanticExtractionValidator.Validate(
                        compilation, method, selected);

                AssertEqual(
                    expectGuard
                        ? "Unsupported"
                        : "ReviewRequired",
                    result.Status);

                AssertEqual(
                    expectGuard
                        ? "PreprocessorDirectivesPresent"
                        : "BehavioralEquivalenceNotProven",
                    result.Reason);

                AssertTrue(
                    result.OriginalCompilationChecked,
                    "Original compilation was not checked.");

                AssertEqual(0, result.OriginalErrors);

                AssertEqual(
                    !expectGuard,
                    result.TransformedCompilationChecked);

                if (expectGuard)
                {
                    AssertTrue(
                        result.TransformedErrors is null,
                        "Rejected extraction was transformed.");
                }
                else
                {
                    AssertEqual(0, result.TransformedErrors);
                }

                if (!checkPreview)
                    return;

                var preview = RefactorPreviewApi.Preview(
                    compilation, method, selected);

                AssertEqual("Unsupported", preview.Status);
                AssertEqual(
                    "PreprocessorDirectivesPresent",
                    preview.Reason);

                AssertTrue(
                    preview.TransformedSource is null,
                    "Rejected preview contains transformed source.");

                AssertEqual(source, preview.OriginalSource);

                AssertTrue(
                    preview.OriginalCompilationChecked &&
                    !preview.TransformedCompilationChecked,
                    "Preview compilation flags incorrect.");
            });
    }
}
