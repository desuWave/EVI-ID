using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

var references =
    ((string?)AppContext.GetData(
        "TRUSTED_PLATFORM_ASSEMBLIES"))
    ?.Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray()
    ?? throw new InvalidOperationException(
        "Runtime references unavailable.");

(string? symbol, int errors) Analyze(
    string source,
    string assemblyName)
{
    var tree = CSharpSyntaxTree.ParseText(source);

    var compilation = CSharpCompilation.Create(
        assemblyName,
        new[] { tree },
        references,
        new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary));

    int errors = compilation.GetDiagnostics()
        .Count(d =>
            d.Severity == DiagnosticSeverity.Error);

    if (errors != 0)
        return (null, errors);

    var model = compilation.GetSemanticModel(tree);

    var invocation = tree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .FirstOrDefault(i =>
            i.Expression.ToString().Contains(
                "WriteLine"));

    if (invocation is null)
        return (null, errors);

    var symbol = model.GetSymbolInfo(invocation).Symbol;

    return (
        symbol?.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat),
        errors);
}

const string original = """
public class Probe
{
    public void Test()
    {
        System.Console.WriteLine(123);
    }
}
""";

const string transformed = """
public class Probe
{
    public void Test()
    {
        Extracted();
    }

    private void Extracted()
    {
        System.Console.WriteLine(123);
    }
}
""";

var before = Analyze(original, "OriginalSymbols");
var after = Analyze(transformed, "TransformedSymbols");

bool passed =
    before.errors == 0 &&
    after.errors == 0 &&
    before.symbol is not null &&
    before.symbol == after.symbol;

Console.WriteLine("=== SYMBOL IDENTITY REGRESSION ===");
Console.WriteLine($"Original errors: {before.errors}");
Console.WriteLine($"Transformed errors: {after.errors}");
Console.WriteLine($"Original symbol: {before.symbol}");
Console.WriteLine($"Transformed symbol: {after.symbol}");
Console.WriteLine($"Symbol identity preserved: {passed}");
Console.WriteLine(
    passed ? "RESULT: PASS" : "RESULT: FAIL");

Environment.ExitCode = passed ? 0 : 1;
