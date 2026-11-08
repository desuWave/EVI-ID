using System;
using System.Collections.Generic;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using EVI_Refactor;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorEngineReferenceTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "RefactorEngine: explicit reference forwarding",
            () =>
            {
                var externalTree =
                    CSharpSyntaxTree.ParseText(
                        "public class ExternalWidget {}");

                var externalCompilation =
                    CSharpCompilation.Create(
                        "ExternalLibrary",
                        new[] { externalTree },
                        options: new CSharpCompilationOptions(
                            OutputKind.DynamicallyLinkedLibrary));

                var sourceBuilder = new StringBuilder();

                sourceBuilder.AppendLine("class C");
                sourceBuilder.AppendLine("{");
                sourceBuilder.AppendLine("    void M()");
                sourceBuilder.AppendLine("    {");

                sourceBuilder.AppendLine(
                    "        ExternalWidget widget = null;");

                for (int i = 0; i < 23; i++)
                {
                    sourceBuilder.AppendLine(
                        "        widget = null;");
                }

                sourceBuilder.AppendLine("    }");
                sourceBuilder.AppendLine("}");

                string source = sourceBuilder.ToString();

                var engine = new RefactorEngine();

                string withoutReference =
                    engine.AnalyzeSource(
                        "ReferenceTest.cs",
                        source,
                        maxMethodLines: 1);

                string withReference =
                    engine.AnalyzeSource(
                        "ReferenceTest.cs",
                        source,
                        maxMethodLines: 1,
                        references: new MetadataReference[]
                        {
                            externalCompilation.ToMetadataReference()
                        });

                AssertTrue(
                    withoutReference.Contains(
                        "REFACTOR CANDIDATES:"),
                    "Baseline candidate was not generated. Report: " +
                    withoutReference);

                AssertTrue(
                    withReference.Contains(
                        "REFACTOR CANDIDATES:"),
                    "Referenced candidate was not generated.");

                AssertTrue(
                    withoutReference.Contains(
                        "Diagnostic:CS0246"),
                    "Expected unresolved ExternalWidget type.");

                AssertTrue(
                    !withReference.Contains(
                        "Diagnostic:CS0246"),
                    "ExternalWidget remains unresolved; " +
                    "MetadataReference may not have been forwarded.");

                AssertTrue(
                    withoutReference.Contains(
                        "SemanticSafety: Unsupported"),
                    "Expected unsupported baseline.");

                AssertTrue(
                    withReference.Contains(
                        "SemanticSafety: Unsupported"),
                    "Expected unsupported result without core references.");
            });
    }
}
