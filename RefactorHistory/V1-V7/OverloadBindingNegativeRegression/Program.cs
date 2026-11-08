using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string original = """
public class Probe
{
    private static void Pick(int value) { }
    private static void Pick(long value) { }

    public void Test()
    {
        Pick(42);
    }
}
""";

const string transformed = """
public class Probe
{
    private static void Pick(int value) { }
    private static void Pick(long value) { }

    public void Test()
    {
        Pick(42L);
    }
}
""";

var references = ((string)AppContext.GetData(
        "TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray();

var originalTree = CSharpSyntaxTree.ParseText(original);
var transformedTree = CSharpSyntaxTree.ParseText(transformed);

CSharpCompilation Compile(SyntaxTree tree) =>
    CSharpCompilation.Create(
        "OverloadBindingNegativeRegression",
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

Console.WriteLine("=== OVERLOAD BINDING NEGATIVE REGRESSION ===");
Console.WriteLine($"Original: {Signature(originalSymbol)}");
Console.WriteLine($"Transformed: {Signature(transformedSymbol)}");
Console.WriteLine($"Original errors: {originalErrors}");
Console.WriteLine($"Transformed errors: {transformedErrors}");
Console.WriteLine($"Preserved: {result.Preserved}");
Console.WriteLine($"Reason: {result.Reason}");
Console.WriteLine($"Checked bindings: {result.CheckedBindings}");

bool detected =
    originalErrors == 0 &&
    transformedErrors == 0 &&
    originalSymbol is not null &&
    transformedSymbol is not null &&
    !result.Preserved &&
    result.Reason == "SymbolBindingChanged";

Console.WriteLine(
    detected
        ? "RESULT: PASS - binding change detected"
        : "RESULT: FAIL - binding change not detected");

Environment.ExitCode = detected ? 0 : 1;
