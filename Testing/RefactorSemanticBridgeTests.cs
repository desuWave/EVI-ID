using System;
using System.IO;
using Microsoft.CodeAnalysis;
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
        RunBridgeDiagnosticsTests(results);
        Run(results,
            "RefactorBridge: compilation reference",
            () =>
            {
                var externalTree =
                    CSharpSyntaxTree.ParseText(
                        "public class ExternalWidget {} ");
                var externalCompilation =
                    CSharpCompilation.Create(
                        "ExternalLibrary",
                        new[] { externalTree },
                        options: new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                var sourceTree =
                    CSharpSyntaxTree.ParseText(
                        "class C { ExternalWidget field; }");

                var withoutReference =
                    CSharpCompilation.Create(
                        "WithoutReference",
                        new[] { sourceTree });
                var withReference =
                    CSharpCompilation.Create(
                        "WithReference",
                        new[] { sourceTree },
                        references: new MetadataReference[]
                        {
                            externalCompilation.ToMetadataReference()
                        });

                AssertTrue(
                    withoutReference.GetTypeByMetadataName(
                        "ExternalWidget") == null,
                    "External type unexpectedly available.");
                AssertTrue(
                    withReference.GetTypeByMetadataName(
                        "ExternalWidget") != null,
                    "External type not resolved.");

                var typeSyntax = sourceTree.GetRoot()
                    .DescendantNodes()
                    .OfType<VariableDeclarationSyntax>()
                    .Single().Type;

                var missingSymbol = withoutReference
                    .GetSemanticModel(sourceTree)
                    .GetSymbolInfo(typeSyntax).Symbol;

                var resolvedSymbol = withReference
                    .GetSemanticModel(sourceTree)
                    .GetSymbolInfo(typeSyntax).Symbol;

                AssertTrue(missingSymbol == null,
                    "External type resolved without reference.");
                AssertTrue(resolvedSymbol != null,
                    "External type usage was not bound.");
                AssertEqual("ExternalWidget",
                    resolvedSymbol!.Name);
                AssertEqual("ExternalLibrary",
                    resolvedSymbol.ContainingAssembly.Name);

                var missingTypeErrors = withoutReference
                    .GetDiagnostics()
                    .Where(d => d.Id == "CS0246")
                    .ToArray();

                var resolvedTypeErrors = withReference
                    .GetDiagnostics()
                    .Where(d => d.Id == "CS0246")
                    .ToArray();

                AssertTrue(missingTypeErrors.Any(d =>
                    d.GetMessage().Contains("ExternalWidget")),
                    "Missing expected CS0246 for ExternalWidget.");

                AssertTrue(!resolvedTypeErrors.Any(d =>
                    d.GetMessage().Contains("ExternalWidget")),
                    "ExternalWidget remains unresolved.");
            });
        Run(results,
            "RefactorBridge: compilation reference through bridge",
            () =>
            {
                var externalTree = CSharpSyntaxTree.ParseText(
                    "public class ExternalWidget {}");
                var externalCompilation = CSharpCompilation.Create(
                    "ExternalLibrary",
                    new[] { externalTree },
                    options: new CSharpCompilationOptions(
                        OutputKind.DynamicallyLinkedLibrary));

                var tree = CSharpSyntaxTree.ParseText(
                    "class C { void M() { ExternalWidget x = null; } }");
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

                var result = RefactorSemanticBridge.Evaluate(
                    tree, method, group,
                    new MetadataReference[]
                    {
                        externalCompilation.ToMetadataReference()
                    });

                AssertEqual(RefactorSafetyStatus.Unsupported,
                    result.Status);
                AssertTrue(!result.Reasons.Contains(
                    "SemanticAnalysisUnavailable"),
                    "Bridge could not process compilation reference.");
                AssertTrue(result.Reasons.Contains(
                    "CompilationDiagnosticsPresent"),
                    "Expected diagnostics without core references.");
            });
        Run(results,
            "RefactorBridge: explicit references",
            () =>
            {
                string source =
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

                var implicitResult =
                    RefactorSemanticBridge.Evaluate(
                        tree, method, group);
                var explicitResult =
                    RefactorSemanticBridge.Evaluate(
                        tree, method, group,
                        Array.Empty<MetadataReference>());

                AssertEqual(implicitResult.Status,
                    explicitResult.Status);
                AssertEqual(RefactorSafetyStatus.Unsupported,
                    explicitResult.Status);
                AssertTrue(explicitResult.Reasons.Contains(
                    "CompilationDiagnosticsPresent"),
                    "Missing compilation diagnostics.");
            });
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
                    report.Contains(
                        "SemanticSafety: Unsupported"),
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

        Run(
            results,
            "RefactorBridge: same-line span mapping",
            () =>
            {
                string source =
                    "class C { void M() { " +
                    "int a = 1; int b = 2; " +
                    "int c = a + b; } }";

                var tree = CSharpSyntaxTree.ParseText(
                    source);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var statements =
                    method.Body!.Statements;

                var group = new RefactorGroup(
                    1, 1, 2, 1,
                    "CandidateForExtraction",
                    "Candidate",
                    "None",
                    "None",
                    false,
                    "None");

                group.SetSourceSpan(
                    statements[1].SpanStart,
                    statements[2].Span.End);

                var result =
                    RefactorSemanticBridge.Evaluate(
                        tree,
                        method,
                        group);

                AssertEqual(
                    RefactorSafetyStatus.Unsupported,
                    result.Status);
            });

        Run(
            results,
            "RefactorBridge: invalid source span",
            () =>
            {
                string source = MakeBridgeTestSource();

                var tree = CSharpSyntaxTree.ParseText(
                    source);

                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();

                var group = new RefactorGroup(
                    1, 1, 1, 1,
                    "CandidateForExtraction",
                    "Candidate",
                    "None",
                    "None",
                    false,
                    "None");

                group.SetSourceSpan(100, 50);

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
                        "InvalidSourceSpan"),
                    "Expected invalid span reason.");
            });
    }

    private static void RunBridgeDiagnosticsTests(
        List<UnitTestResult> results)
    {
        Run(results,
            "RefactorBridge: syntax errors unsupported",
            () =>
            {
                string source = MakeBridgeTestSource()
                    .Replace("int value0 = 0;",
                             "int value0 = ;");
                var tree = CSharpSyntaxTree.ParseText(source);
                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();
                var statements = method.Body!.Statements;
                var group = new RefactorGroup(
                    1, 100, statements.Count, 100,
                    "CandidateForExtraction", "Candidate",
                    "None", "None", false, "None");
                group.SetSourceSpan(
                    statements[0].SpanStart,
                    statements[^1].Span.End);
                var result = RefactorSemanticBridge.Evaluate(
                    tree, method, group);
                AssertEqual(RefactorSafetyStatus.Unsupported,
                    result.Status);
                AssertTrue(result.Reasons.Contains(
                    "SyntaxErrorsPresent"),
                    "Missing syntax error reason.");
            });

        Run(results,
            "RefactorBridge: unresolved type unsupported",
            () =>
            {
                string source = MakeBridgeTestSource()
                    .Replace("int value0 = 0;",
                             "MissingType value0 = null;");
                var tree = CSharpSyntaxTree.ParseText(source);
                var method = tree.GetRoot()
                    .DescendantNodes()
                    .OfType<MethodDeclarationSyntax>()
                    .Single();
                var statements = method.Body!.Statements;
                var group = new RefactorGroup(
                    1, 100, statements.Count, 100,
                    "CandidateForExtraction", "Candidate",
                    "None", "None", false, "None");
                group.SetSourceSpan(
                    statements[0].SpanStart,
                    statements[^1].Span.End);
                var result = RefactorSemanticBridge.Evaluate(
                    tree, method, group);
                AssertEqual(RefactorSafetyStatus.Unsupported,
                    result.Status);
                AssertTrue(result.Reasons.Contains(
                    "UnresolvedSymbolsOrReferences"),
                    "Missing unresolved type reason.");
                AssertTrue(result.Reasons.Contains(
                    "Diagnostic:CS0246"),
                    "Missing CS0246 diagnostic reason.");
            });

        Run(results,
            "RefactorBridge: syntax error classification",
            () =>
            {
                string source = MakeBridgeTestSource()
                    .Replace("int value0 = 0;",
                             "int value0 = ;");

                string report = new RefactorEngine()
                    .AnalyzeSource("Syntax.cs", source,
                                   maxMethodLines: 1);

                AssertTrue(report.Contains(
                    "SyntaxErrorsPresent"),
                    "Syntax errors were not classified.");
            });

        Run(results,
            "RefactorBridge: unresolved symbol classification",
            () =>
            {
                string source = MakeBridgeTestSource()
                    .Replace("int value0 = 0;",
                             "MissingType value0 = null;");

                string report = new RefactorEngine()
                    .AnalyzeSource("Missing.cs", source,
                                   maxMethodLines: 1);

                AssertTrue(report.Contains(
                    "UnresolvedSymbolsOrReferences"),
                    "Unresolved symbols were not classified.");
            });

        Run(results,
            "RefactorBridge: missing type diagnostics",
            () =>
            {
                string source = MakeBridgeTestSource()
                    .Replace("int value0 = 0;",
                             "MissingType value0 = null;");

                string report = new RefactorEngine()
                    .AnalyzeSource("Missing.cs", source,
                                   maxMethodLines: 1);

                AssertTrue(report.Contains(
                    "CompilationDiagnosticsPresent"),
                    "Missing compilation diagnostic.");
            });

        Run(results,
            "RefactorBridge: diagnostics retain safety",
            () =>
            {
                string report = new RefactorEngine()
                    .AnalyzeSource("BridgeTest.cs",
                        MakeBridgeTestSource(),
                        maxMethodLines: 1);

                AssertTrue(report.Contains(
                    "SemanticSafety: Unsupported"),
                    "Safety status was lost.");
                AssertTrue(report.Contains(
                    "CompilationDiagnosticsPresent"),
                    "Expected incomplete compilation context.");
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
