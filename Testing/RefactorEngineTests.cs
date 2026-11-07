using System;
using System.Collections.Generic;
using EVI_Refactor;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorEngineTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "RefactorEngine: simple source",
            () =>
            {
                var engine = new RefactorEngine();

                string output = engine.AnalyzeSource(
                    "Simple.cs",
                    "class Simple { void Run() {} }");

                AssertTrue(
                    output.Contains("FILE: Simple.cs"),
                    "File name missing.");

                AssertTrue(
                    output.Contains("Classes: 1"),
                    "Class count incorrect.");

                AssertTrue(
                    output.Contains("Methods: 1"),
                    "Method count incorrect.");

                AssertTrue(
                    !output.Contains("[LARGE FILE]"),
                    "Unexpected large file diagnostic.");

                AssertTrue(
                    !output.Contains("[LARGE METHOD]"),
                    "Unexpected large method diagnostic.");
            });

        Run(
            results,
            "RefactorEngine: large file",
            () =>
            {
                var engine = new RefactorEngine();

                string source =
                    "class Example\n{\n}\n";

                string output = engine.AnalyzeSource(
                    "Large.cs",
                    source,
                    maxFileLines: 2);

                AssertTrue(
                    output.Contains("[LARGE FILE]"),
                    "Large file was not detected.");
            });

        Run(
            results,
            "RefactorEngine: large method",
            () =>
            {
                var engine = new RefactorEngine();

                string source =
                    "class Example\n" +
                    "{\n" +
                    "    void Run()\n" +
                    "    {\n" +
                    "        int a = 1;\n" +
                    "        int b = 2;\n" +
                    "        int c = 3;\n" +
                    "    }\n" +
                    "}";

                string output = engine.AnalyzeSource(
                    "Method.cs",
                    source,
                    maxMethodLines: 3);

                AssertTrue(
                    output.Contains("[LARGE METHOD]"),
                    "Large method was not detected.");

                AssertTrue(
                    output.Contains("Run:"),
                    "Method name missing.");
            });

        Run(
            results,
            "RefactorEngine: deep nesting",
            () =>
            {
                var engine = new RefactorEngine();

                string source =
                    "class Example\n" +
                    "{\n" +
                    "    void Run()\n" +
                    "    {\n" +
                    "        if (true)\n" +
                    "        {\n" +
                    "            if (true)\n" +
                    "            {\n" +
                    "                int value = 1;\n" +
                    "            }\n" +
                    "        }\n" +
                    "    }\n" +
                    "}";

                string output = engine.AnalyzeSource(
                    "Nested.cs",
                    source,
                    maxDepth: 1);

                AssertTrue(
                    output.Contains("[DEEP NESTING]"),
                    "Deep nesting was not detected.");
            });

        Run(
            results,
            "RefactorEngine: null source",
            () =>
            {
                var engine = new RefactorEngine();

                string output = engine.AnalyzeSource(
                    "Null.cs",
                    null!);

                AssertTrue(
                    output.Contains("Source is null: Null.cs"),
                    "Null source was not handled.");
            });

        Run(
            results,
            "RefactorEngine: non-CSharp source",
            () =>
            {
                var engine = new RefactorEngine();

                var sources =
                    new List<(string Name, string Content)>();

                string output = engine.AnalyzeSources(
                    "notes.txt",
                    sources,
                    isDirectory: false);

                AssertTrue(
                    output.Contains("Skipping non-C# file"),
                    "Non-CSharp source was not skipped.");
            });

        Run(
            results,
            "RefactorEngine: multiple sources",
            () =>
            {
                var engine = new RefactorEngine();

                var sources =
                    new List<(string Name, string Content)>
                    {
                        (
                            "Beta.cs",
                            "class Beta {}"
                        ),
                        (
                            "Alpha.cs",
                            "class Alpha {}"
                        )
                    };

                string output = engine.AnalyzeSources(
                    "Sources",
                    sources,
                    isDirectory: true);

                AssertTrue(
                    output.Contains("C# files: 2"),
                    "Source count incorrect.");

                int alpha = output.IndexOf(
                    "FILE: Alpha.cs",
                    StringComparison.Ordinal);

                int beta = output.IndexOf(
                    "FILE: Beta.cs",
                    StringComparison.Ordinal);

                AssertTrue(
                    alpha >= 0 && beta > alpha,
                    "Sources were not ordered correctly.");
            });
    }
}
