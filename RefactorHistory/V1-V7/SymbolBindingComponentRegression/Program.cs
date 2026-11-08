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

const string preservedSource = """
public class Probe
{
    public void Test()
    {
        System.Console.WriteLine(123);
    }
}
""";

const string changedSource = """
public class Probe
{
    private void Execute() { }

    public void Test()
    {
        void Execute() { }
        Execute();
    }
}
""";

int passed = 0;
int total = 0;

Console.WriteLine(
    "=== SYMBOL BINDING COMPONENT REGRESSION ===");

RunCase(
    "Framework binding preserved",
    preservedSource,
    0,
    false,
    true,
    "SymbolBindingsPreserved",
    1);

RunCase(
    "Local function binding changed",
    changedSource,
    1,
    false,
    false,
    "SymbolBindingChanged",
    0);

RunCase(
    "Invocation count mismatch",
    preservedSource,
    0,
    true,
    false,
    "InvocationCountChanged",
    0);

Console.WriteLine(
    $"RESULT: {passed}/{total} PASS");

Environment.ExitCode =
    passed == total ? 0 : 1;

void RunCase(
    string name,
    string source,
    int statementIndex,
    bool omitTransformedInvocation,
    bool expectedPreserved,
    string expectedReason,
    int expectedChecked)
{
    total++;

    var tree = CSharpSyntaxTree.ParseText(source);
    var root = tree.GetCompilationUnitRoot();

    var original = CSharpCompilation.Create(
        "BindingComponentRegression",
        new[] { tree },
        references,
        new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary));

    var owner = root.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();

    var method = owner.Members
        .OfType<MethodDeclarationSyntax>()
        .Single(m => m.Identifier.ValueText == "Test");

    var body = method.Body!;
    var selected = body.Statements[statementIndex];

    const string helperName =
        "__EVI_Extracted";

    var helper = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            helperName)
        .AddModifiers(
            SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
        .WithBody(SyntaxFactory.Block(selected));

    var call = SyntaxFactory.ParseStatement(
        helperName + "();");

    var updatedStatements = body.Statements
        .Take(statementIndex)
        .Concat(new[] { call })
        .Concat(body.Statements.Skip(statementIndex + 1));

    var updatedMethod = method.WithBody(
        body.WithStatements(
            SyntaxFactory.List(updatedStatements)));

    var updatedOwner = owner
        .ReplaceNode(method, updatedMethod)
        .AddMembers(helper);

    var updatedRoot = root.ReplaceNode(
        owner, updatedOwner);

    var transformedTree = tree.WithRootAndOptions(
        updatedRoot, tree.Options);

    var transformed =
        original.ReplaceSyntaxTree(
            tree, transformedTree);

    int originalErrors = original.GetDiagnostics()
        .Count(d =>
            d.Severity == DiagnosticSeverity.Error);

    int transformedErrors = transformed.GetDiagnostics()
        .Count(d =>
            d.Severity == DiagnosticSeverity.Error);

    var originalInvocations = selected
        .DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .ToArray();

    var transformedHelper = transformedTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single(m =>
            m.Identifier.ValueText == helperName);

    var transformedInvocations = transformedHelper.Body!
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .ToArray();

    if (omitTransformedInvocation)
        transformedInvocations =
            Array.Empty<InvocationExpressionSyntax>();

    var result =
        SymbolBindingPreservationValidator.Validate(
            original.GetSemanticModel(tree),
            transformed.GetSemanticModel(transformedTree),
            originalInvocations,
            transformedInvocations);

    bool ok =
        originalErrors == 0 &&
        transformedErrors == 0 &&
        result.Preserved == expectedPreserved &&
        result.Reason == expectedReason &&
        result.CheckedBindings == expectedChecked;

    Console.WriteLine(
        $"{name}: {(ok ? "PASS" : "FAIL")}");

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

    if (ok)
        passed++;
}
