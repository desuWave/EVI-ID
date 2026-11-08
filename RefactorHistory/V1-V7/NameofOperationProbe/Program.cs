using System;
using System.IO;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string source = """
public class Probe
{
    private static void Execute() { }

    public void Test()
    {
        int value = 123;
        System.Console.WriteLine(nameof(value));
        System.Console.WriteLine(nameof(Execute));
        System.Console.WriteLine(nameof(System.String));
        System.Console.WriteLine("Ordinary call");
    }
}
""";

var tree = CSharpSyntaxTree.ParseText(source);

var references = ((string)AppContext.GetData(
        "TRUSTED_PLATFORM_ASSEMBLIES")!)
    .Split(Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path));

var compilation = CSharpCompilation.Create(
    "NameofOperationProbe",
    new[] { tree },
    references,
    new CSharpCompilationOptions(
        OutputKind.DynamicallyLinkedLibrary));

var model = compilation.GetSemanticModel(tree);

Console.WriteLine("=== NAMEOF OPERATION PROBE ===");

foreach (var invocation in tree.GetRoot()
    .DescendantNodes()
    .OfType<InvocationExpressionSyntax>())
{
    var operation = model.GetOperation(invocation);

    Console.WriteLine(
        $"Expression: {invocation}");

    Console.WriteLine(
        $"  Operation kind: {operation?.Kind.ToString() ?? "null"}");

    Console.WriteLine(
        $"  Operation type: {operation?.GetType().Name ?? "null"}");

    Console.WriteLine(
        $"  Symbol: {model.GetSymbolInfo(invocation).Symbol?.ToDisplayString() ?? "null"}");
}

int errors = compilation.GetDiagnostics()
    .Count(d => d.Severity == DiagnosticSeverity.Error);

Console.WriteLine($"Compilation errors: {errors}");

Environment.ExitCode = errors == 0 ? 0 : 1;
