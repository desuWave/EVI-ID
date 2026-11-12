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
    private static void RunRefactorV7SemanticRuntimeTests(
        List<UnitTestResult> results,
        Context androidContext)
    {
        Run(
            results,
            "RefactorV7: Android semantic validation",
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
                    "class C { void M() { int x = 1; } }";

                var tree =
                    CSharpSyntaxTree.ParseText(source);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation =
                    CSharpCompilation.Create(
                        "EVI_V7_Android_Regression",
                        new[] { tree },
                        references,
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                var result =
                    SemanticExtractionValidator.Validate(
                        compilation,
                        method,
                        method.Body!.Statements);

                AssertEqual(
                    "ReviewRequired",
                    result.Status);

                AssertEqual(
                    "BehavioralEquivalenceNotProven",
                    result.Reason);

                AssertTrue(
                    result.OriginalCompilationChecked,
                    "Original compilation was not checked.");

                AssertTrue(
                    result.TransformedCompilationChecked,
                    "Transformed compilation was not checked.");

                AssertEqual(0, result.OriginalErrors);
                AssertEqual(0, result.TransformedErrors);
            });
    }
}
