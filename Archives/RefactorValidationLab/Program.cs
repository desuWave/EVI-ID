using System;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

const string source = """
public class ExtractionProbe
{
    public void Test()
    {
        int value = 10;
        value = value + 1;
    }
}
""";

var originalTree = CSharpSyntaxTree.ParseText(source);
var originalRoot = originalTree.GetCompilationUnitRoot();

var originalClass = originalRoot
    .DescendantNodes()
    .OfType<ClassDeclarationSyntax>()
    .Single();

var originalMethod = originalClass.Members
    .OfType<MethodDeclarationSyntax>()
    .Single();

var statements = originalMethod.Body!.Statements;

var extractedMethod = SyntaxFactory.ParseMemberDeclaration(
    """
    private void Extracted()
    {
        int value = 10;
        value = value + 1;
    }
    """) as MethodDeclarationSyntax;

if (extractedMethod is null)
    throw new InvalidOperationException("Method parsing failed.");

var replacementMethod = originalMethod.WithBody(
    SyntaxFactory.Block(
        SyntaxFactory.ParseStatement("Extracted();")));

var transformedClass = originalClass
    .ReplaceNode(originalMethod, replacementMethod)
    .AddMembers(extractedMethod);

var transformedRoot = originalRoot
    .ReplaceNode(originalClass, transformedClass);

var transformedTree = CSharpSyntaxTree.ParseText(
    transformedRoot.NormalizeWhitespace().ToFullString());

var references = ((string?)AppContext.GetData("TRUSTED_PLATFORM_ASSEMBLIES"))
    ?.Split(System.IO.Path.PathSeparator)
    .Select(path => MetadataReference.CreateFromFile(path))
    .ToArray()
    ?? throw new InvalidOperationException(
        "Runtime reference list unavailable.");

Diagnostic[] CompileErrors(SyntaxTree tree)
{
    var compilation = CSharpCompilation.Create(
        "ExtractionRegression",
        new[] { tree },
        references,
        new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary));

    return compilation.GetDiagnostics()
        .Where(d => d.Severity == DiagnosticSeverity.Error)
        .ToArray();
}

var before = CompileErrors(originalTree);
var after = CompileErrors(transformedTree);

Console.WriteLine("=== SEMANTIC EXTRACTION REGRESSION ===");
Console.WriteLine($"Original errors: {before.Length}");
Console.WriteLine($"Transformed errors: {after.Length}");
Console.WriteLine($"Extracted statements: {statements.Count}");
Console.WriteLine($"Original method statements: {replacementMethod.Body!.Statements.Count}");
Console.WriteLine($"Generated helper: {extractedMethod.Identifier.Text}");

foreach (var diagnostic in after)
    Console.WriteLine(diagnostic);

bool passed =
    before.Length == 0 &&
    after.Length == 0 &&
    statements.Count == 2 &&
    replacementMethod.Body.Statements.Count == 1;

Console.WriteLine(
    passed ? "RESULT: PASS" : "RESULT: FAIL");

Console.WriteLine(
    "SemanticSafety: ReviewRequired");

Console.WriteLine(
    "Reason: BehavioralEquivalenceNotProven");

Environment.ExitCode = passed ? 0 : 1;
