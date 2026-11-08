using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string original = """
public static class Alpha
{
    public static void Execute() { }
}

public static class Beta
{
    public static void Execute() { }
}

public static class Probe
{
    public static void Test()
    {
        Alpha.Execute();
    }
}
""";

const string transformed = """
public static class Alpha
{
    public static void Execute() { }
}

public static class Beta
{
    public static void Execute() { }
}

public static class Probe
{
    public static void Test()
    {
        Beta.Execute();
    }
}
""";

var references =
    ((string?)AppContext.GetData(
        "TRUSTED_PLATFORM_ASSEMBLIES"))
    ?.Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray()
    ?? throw new InvalidOperationException(
        "Runtime references unavailable.");

(string? name, string? identity, int errors) Analyze(
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
        return (null, null, errors);

    var model = compilation.GetSemanticModel(tree);

    var invocation = tree.GetRoot()
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();

    var symbol = model.GetSymbolInfo(invocation).Symbol
        as IMethodSymbol;

    if (symbol is null)
        return (null, null, errors);

    string identity =
        symbol.ContainingType.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat)
        + "::"
        + symbol.ToDisplayString(
            SymbolDisplayFormat.FullyQualifiedFormat);

    return (symbol.Name, identity, errors);
}

var before = Analyze(original, "IdentityOriginal");
var after = Analyze(transformed, "IdentityTransformed");

bool naiveMatch =
    before.name is not null &&
    before.name == after.name;

bool identityChanged =
    before.identity is not null &&
    after.identity is not null &&
    before.identity != after.identity;

bool passed =
    before.errors == 0 &&
    after.errors == 0 &&
    naiveMatch &&
    identityChanged;

Console.WriteLine(
    "=== SYMBOL IDENTITY NEGATIVE REGRESSION ===");

Console.WriteLine(
    $"Original errors: {before.errors}");

Console.WriteLine(
    $"Transformed errors: {after.errors}");

Console.WriteLine(
    $"Original name: {before.name}");

Console.WriteLine(
    $"Transformed name: {after.name}");

Console.WriteLine(
    $"Naive name match: {naiveMatch}");

Console.WriteLine(
    $"Original identity: {before.identity}");

Console.WriteLine(
    $"Transformed identity: {after.identity}");

Console.WriteLine(
    $"Identity changed: {identityChanged}");

Console.WriteLine(
    passed ? "RESULT: PASS" : "RESULT: FAIL");

Environment.ExitCode = passed ? 0 : 1;
