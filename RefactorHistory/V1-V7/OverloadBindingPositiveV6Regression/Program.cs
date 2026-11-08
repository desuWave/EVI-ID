using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var cases = new[]
{
    new
    {
        Name = "Numeric overload",
        Members = """
            private static void Pick(int value) { }
            private static void Pick(long value) { }
            """,
        Call = "Pick(42);"
    },
    new
    {
        Name = "Null overload",
        Members = """
            private static void Pick(object? value) { }
            private static void Pick(string? value) { }
            """,
        Call = "Pick(null);"
    },
    new
    {
        Name = "Optional parameter overload",
        Members = """
            private static void Pick(int value) { }
            private static void Pick(int value, int extra = 10) { }
            """,
        Call = "Pick(42);"
    }
};

var references = ((string)AppContext.GetData(
        "TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray();

int passed = 0;

Console.WriteLine(
    "=== OVERLOAD BINDING POSITIVE V6 REGRESSION ===");

foreach (var test in cases)
{
    string original = $$"""
        #nullable enable
        public class Probe
        {
            {{test.Members}}

            public void Test()
            {
                {{test.Call}}
            }
        }
        """;

    string transformed = $$"""
        #nullable enable
        public class Probe
        {
            {{test.Members}}

            public void Test()
            {
                __Extracted();
            }

            private static void __Extracted()
            {
                {{test.Call}}
            }
        }
        """;

    var originalTree = CSharpSyntaxTree.ParseText(original);
    var transformedTree = CSharpSyntaxTree.ParseText(transformed);

    CSharpCompilation Compile(SyntaxTree tree) =>
        CSharpCompilation.Create(
            "PositiveBindingRegression",
            new[] { tree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

    var originalCompilation = Compile(originalTree);
    var transformedCompilation = Compile(transformedTree);

    var originalModel =
        originalCompilation.GetSemanticModel(originalTree);

    var transformedModel =
        transformedCompilation.GetSemanticModel(transformedTree);

    InvocationExpressionSyntax FindPick(SyntaxTree tree) =>
        tree.GetRoot()
            .DescendantNodes()
            .OfType<InvocationExpressionSyntax>()
            .Single(node =>
                node.Expression is IdentifierNameSyntax name &&
                name.Identifier.ValueText == "Pick");

    var originalInvocation = FindPick(originalTree);
    var transformedInvocation = FindPick(transformedTree);

    var originalSymbol =
        originalModel.GetSymbolInfo(originalInvocation).Symbol
            as IMethodSymbol;

    var transformedSymbol =
        transformedModel.GetSymbolInfo(transformedInvocation).Symbol
            as IMethodSymbol;

    int originalErrors = originalCompilation.GetDiagnostics()
        .Count(d => d.Severity == DiagnosticSeverity.Error);

    int transformedErrors = transformedCompilation.GetDiagnostics()
        .Count(d => d.Severity == DiagnosticSeverity.Error);

    var result = SymbolBindingPreservationValidator.Validate(
        originalModel,
        transformedModel,
        new[] { originalInvocation },
        new[] { transformedInvocation });

    string Signature(IMethodSymbol? method) =>
        method is null
            ? "null"
            : $"{method.ContainingType.ToDisplayString()}::{method.Name}({string.Join(", ", method.Parameters.Select(p => p.Type.ToDisplayString()))})";

    bool ok =
        originalErrors == 0 &&
        transformedErrors == 0 &&
        originalSymbol is not null &&
        transformedSymbol is not null &&
        result.Preserved &&
        result.Reason == "SymbolBindingsPreserved" &&
        result.CheckedBindings == 1;

    if (ok)
        passed++;

    Console.WriteLine(
        $"{test.Name}: {(ok ? "PASS" : "FAIL")}");
    Console.WriteLine(
        $"  Original: {Signature(originalSymbol)}");
    Console.WriteLine(
        $"  Extracted: {Signature(transformedSymbol)}");
    Console.WriteLine(
        $"  Original errors: {originalErrors}");
    Console.WriteLine(
        $"  Transformed errors: {transformedErrors}");
    Console.WriteLine(
        $"  Preserved: {result.Preserved}");
    Console.WriteLine(
        $"  Reason: {result.Reason}");
    Console.WriteLine(
        $"  Checked bindings: {result.CheckedBindings}");
}

Console.WriteLine(
    $"RESULT: {passed}/{cases.Length} PASS");

Environment.ExitCode =
    passed == cases.Length ? 0 : 1;
