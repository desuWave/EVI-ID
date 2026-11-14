using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

internal static class NegativeRegression
{
    public static int Run()
    {
        var references =
            ((string?)AppContext.GetData(
                "TRUSTED_PLATFORM_ASSEMBLIES"))
            ?.Split(System.IO.Path.PathSeparator)
            .Select(path =>
                MetadataReference.CreateFromFile(path))
            .ToArray()
            ?? throw new InvalidOperationException(
                "Runtime references unavailable.");

        int failures = 0;

        failures += Check(
            "Independent extraction",
            """
            public class Probe
            {
                public void Test()
                {
                    Extracted();
                }

                private void Extracted()
                {
                    int value = 10;
                    value++;
                }
            }
            """,
            expectedErrors: false);

        failures += Check(
            "Local variable escapes extraction",
            """
            public class Probe
            {
                public int Test()
                {
                    Extracted();
                    return value;
                }

                private void Extracted()
                {
                    int value = 10;
                    value++;
                }
            }
            """,
            expectedErrors: true);

        failures += Check(
            "Duplicate extracted method",
            """
            public class Probe
            {
                public void Test()
                {
                    Extracted();
                }

                private void Extracted() { }

                private void Extracted() { }
            }
            """,
            expectedErrors: true);

        Console.WriteLine(
            failures == 0
                ? "NEGATIVE REGRESSION: PASS"
                : "NEGATIVE REGRESSION: FAIL");

        return failures == 0 ? 0 : 1;

        int Check(
            string name,
            string source,
            bool expectedErrors)
        {
            var tree = CSharpSyntaxTree.ParseText(source);

            var compilation = CSharpCompilation.Create(
                "NegativeRegression",
                new[] { tree },
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

            var errors = compilation.GetDiagnostics()
                .Where(d =>
                    d.Severity ==
                    DiagnosticSeverity.Error)
                .ToArray();

            bool actualErrors = errors.Length > 0;
            bool passed = actualErrors == expectedErrors;

            Console.WriteLine(
                $"{name}: {(passed ? "PASS" : "FAIL")}");

            Console.WriteLine(
                $"  Compilation errors: {errors.Length}");

            foreach (var error in errors)
                Console.WriteLine(
                    $"  {error.Id}: {error.GetMessage()}");

            return passed ? 0 : 1;
        }
    }
}
