using System.Collections.Generic;
using System.Linq;
using Android.Content;
using EVI_ID.Shell;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorV7PreviewFormattingTests(
        List<UnitTestResult> results,
        Context androidContext)
    {
        Run(
            results,
            "RefactorV7: preview formatting",
            () =>
            {
                var directory =
                    SafReferenceDirectoryResolver.Resolve(
                        androidContext);

                var references =
                    SafMetadataReferenceLoader.Load(
                        androidContext,
                        directory.TreeUriString,
                        directory.DocumentId,
                        new[] { "System.Runtime.dll" });

                const string source =
                    "class C\n" +
                    "{\n" +
                    "    void M()\n" +
                    "    {\n" +
                    "        int x = 1;\n" +
                    "        int y = 2;\n" +
                    "    }\n" +
                    "}\n";

                var tree =
                    CSharpSyntaxTree.ParseText(source);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation =
                    CSharpCompilation.Create(
                        "EVI_V7_Preview_Formatting",
                        new[] { tree },
                        references,
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                var result = RefactorPreviewApi.Preview(
                    compilation,
                    method,
                    new[] { method.Body!.Statements[0] });

                AssertEqual(
                    "ReviewRequired",
                    result.Status);

                AssertEqual(
                    "BehavioralEquivalenceNotProven",
                    result.Reason);

                AssertTrue(
                    result.TransformedSource is not null,
                    "Preview source is missing.");

                string transformed =
                    result.TransformedSource!;

                AssertTrue(
                    transformed.Contains(
                        "        " +
                        "__EVI_ExtractionValidation_Helper();\n" +
                        "        int y = 2;"),
                    "Helper call formatting is incorrect.");

                AssertTrue(
                    transformed.Contains(
                        "    private void " +
                        "__EVI_ExtractionValidation_Helper()\n" +
                        "    {\n" +
                        "        int x = 1;\n" +
                        "    }\n" +
                        "}"),
                    "Helper indentation or braces are incorrect.");

                AssertTrue(
                    !transformed.Contains("}}"),
                    "Closing braces are glued together.");

                AssertEqual(0, result.OriginalErrors);
                AssertEqual(0, result.TransformedErrors);
            });
    }
}
