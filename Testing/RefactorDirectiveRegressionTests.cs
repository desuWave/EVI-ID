using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorDirectiveRegressionTests(
        List<UnitTestResult> results) =>
        RunRefactorDirectiveRegressionTests(results, GetRegressionReferences());

    private static void RunRefactorDirectiveRegressionTests(
        List<UnitTestResult> results, IEnumerable<MetadataReference> references)
    {
        foreach (bool debug in new[] { true, false })
        {
            bool enabled = debug;

            Run(
                results,
                "RefactorV7: directive guard DEBUG=" + enabled,
                () =>
                {
                    string source =
                        "class C { void M() {\n" +
                        "#if DEBUG\n" +
                        "int x = 1;\n" +
                        "#else\n" +
                        "int x = 2;\n" +
                        "#endif\n" +
                        "x++;\n" +
                        "} }";

                    var options = new CSharpParseOptions(
                        preprocessorSymbols: enabled
                            ? new[] { "DEBUG" }
                            : Array.Empty<string>());

                    var tree = CSharpSyntaxTree.ParseText(
                        source, options);

                    var method = tree.GetRoot()
                        .DescendantNodes()
                        .OfType<MethodDeclarationSyntax>()
                        .Single();

                    var compilation = CSharpCompilation.Create(
                        "DirectiveRegression",
                        new[] { tree },
                        references,
                        new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                    var result = SemanticExtractionValidator.Validate(
                        compilation,
                        method,
                        method.Body!.Statements);

                    AssertEqual("Unsupported", result.Status);
                    AssertEqual(
                        "PreprocessorDirectivesPresent",
                        result.Reason);

                    AssertTrue(
                        result.OriginalCompilationChecked &&
                        !result.TransformedCompilationChecked,
                        "Directive guard compilation flags incorrect.");

                    AssertEqual(0, result.OriginalErrors);
                });
        }

        Run(
            results,
            "RefactorV7: original errors precede directives",
            () =>
            {
                const string source =
                    "class C { void M() {\n" +
                    "#if DEBUG\n" +
                    "MissingType x = null;\n" +
                    "#else\n" +
                    "MissingType x = null;\n" +
                    "#endif\n" +
                    "} }";

                var tree = CSharpSyntaxTree.ParseText(
                    source,
                    new CSharpParseOptions(
                        preprocessorSymbols: new[] { "DEBUG" }));

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation = CSharpCompilation.Create(
                    "DirectivePriorityRegression",
                    new[] { tree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));

                var result = SemanticExtractionValidator.Validate(
                    compilation,
                    method,
                    method.Body!.Statements);

                AssertEqual("Unsupported", result.Status);
                AssertEqual(
                    "OriginalCompilationErrors",
                    result.Reason);

                AssertTrue(
                    result.OriginalCompilationChecked &&
                    !result.TransformedCompilationChecked,
                    "Original error priority flags incorrect.");

                AssertTrue(
                    result.OriginalErrors.GetValueOrDefault() > 0,
                    "Expected original compilation errors.");
            });

        Run(
            results,
            "RefactorV7: ordinary extraction unchanged",
            () =>
            {
                const string source =
                    "class C { void M() { int x = 1; } }";

                var tree = CSharpSyntaxTree.ParseText(source);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var compilation = CSharpCompilation.Create(
                    "OrdinaryRegression",
                    new[] { tree },
                    references,
                    new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));

                var result = SemanticExtractionValidator.Validate(
                    compilation,
                    method,
                    method.Body!.Statements);

                AssertEqual("ReviewRequired", result.Status);
                AssertEqual(
                    "BehavioralEquivalenceNotProven",
                    result.Reason);

                AssertTrue(
                    result.OriginalCompilationChecked &&
                    result.TransformedCompilationChecked,
                    "Ordinary extraction compilation flags incorrect.");

                AssertEqual(0, result.OriginalErrors);
                AssertEqual(0, result.TransformedErrors);
            });
    }

    private static MetadataReference[] GetRegressionReferences()
    {
        string? paths = (string?)AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES");

        if (string.IsNullOrEmpty(paths))
            throw new InvalidOperationException(
                "Runtime references unavailable.");

        return paths.Split(System.IO.Path.PathSeparator)
            .Select(path => MetadataReference.CreateFromFile(path))
            .ToArray();
    }
}
