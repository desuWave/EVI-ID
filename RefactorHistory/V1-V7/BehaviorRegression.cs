using System;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Runtime.Loader;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

const string original = """
public static class Probe
{
    public static int Test()
    {
        int x = 10;
        x++;
        return x;
    }
}
""";

const string transformed = """
public static class Probe
{
    public static int Test()
    {
        int x = 10;
        Extracted();
        return x;
    }

    private static void Extracted()
    {
        int x = 10;
        x++;
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

(int errors, int? result) Evaluate(
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

    using var stream = new MemoryStream();

    var emit = compilation.Emit(stream);

    var errors = emit.Diagnostics
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToArray();

    if (!emit.Success)
    {
        foreach (var error in errors)
            Console.WriteLine(error);

        return (errors.Length, null);
    }

    stream.Position = 0;

    var context = new AssemblyLoadContext(
        assemblyName,
        isCollectible: true);

    try
    {
        var assembly = context.LoadFromStream(stream);

        var type = assembly.GetType("Probe")
            ?? throw new InvalidOperationException(
                "Probe type not found.");

        var method = type.GetMethod(
            "Test",
            BindingFlags.Public | BindingFlags.Static)
            ?? throw new InvalidOperationException(
                "Test method not found.");

        var value = (int?)method.Invoke(null, null);

        return (0, value);
    }
    finally
    {
        context.Unload();
    }
}

var before = Evaluate(original, "ProbeOriginal");
var after = Evaluate(transformed, "ProbeTransformed");

Console.WriteLine("=== BEHAVIOR REGRESSION ===");
Console.WriteLine($"Original errors: {before.errors}");
Console.WriteLine($"Transformed errors: {after.errors}");
Console.WriteLine($"Original result: {before.result}");
Console.WriteLine($"Transformed result: {after.result}");

bool passed =
    before.errors == 0 &&
    after.errors == 0 &&
    before.result == 11 &&
    after.result == 10 &&
    before.result != after.result;

Console.WriteLine(
    passed ? "BEHAVIOR REGRESSION: PASS"
           : "BEHAVIOR REGRESSION: FAIL");

Console.WriteLine(
    "SemanticSafety: ReviewRequired");

Console.WriteLine(
    "Reason: BehavioralDifferenceDetected");

Environment.ExitCode = passed ? 0 : 1;
