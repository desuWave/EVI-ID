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

var cases = new[]
{
    new
    {
        Name = "Preserved framework method",
        Source = """
        public class Probe
        {
            public void Test()
            {
                System.Console.WriteLine(123);
            }
        }
        """,
        StatementIndex = 0,
        ExpectPreserved = true
    },
    new
    {
        Name = "Local function binding changed",
        Source = """
        public class Probe
        {
            private void Execute()
            {
                System.Console.WriteLine("class");
            }

            public void Test()
            {
                void Execute()
                {
                    System.Console.WriteLine("local");
                }

                Execute();
            }
        }
        """,
        StatementIndex = 1,
        ExpectPreserved = false
    }
};

int passed = 0;

Console.WriteLine(
    "=== SYMBOL BINDING PRESERVATION REGRESSION ===");

foreach (var test in cases)
{
    var result = Analyze(
        test.Source,
        test.StatementIndex);

    bool ok =
        result.OriginalErrors == 0 &&
        result.TransformedErrors == 0 &&
        result.OriginalSymbol is not null &&
        result.TransformedSymbol is not null &&
        result.Preserved == test.ExpectPreserved;

    Console.WriteLine(
        $"{test.Name}: {(ok ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"  Original errors: {result.OriginalErrors}");

    Console.WriteLine(
        $"  Transformed errors: {result.TransformedErrors}");

    Console.WriteLine(
        $"  Original symbol: {result.OriginalSymbol}");

    Console.WriteLine(
        $"  Transformed symbol: {result.TransformedSymbol}");

    Console.WriteLine(
        $"  Binding preserved: {result.Preserved}");

    if (ok)
        passed++;
}

Console.WriteLine(
    $"RESULT: {passed}/{cases.Length} PASS");

Environment.ExitCode =
    passed == cases.Length ? 0 : 1;

Result Analyze(string source, int statementIndex)
{
    var tree = CSharpSyntaxTree.ParseText(source);
    var root = tree.GetCompilationUnitRoot();

    var compilation = CSharpCompilation.Create(
        "BindingPreservation",
        new[] { tree },
        references,
        new CSharpCompilationOptions(
            OutputKind.DynamicallyLinkedLibrary));

    int originalErrors = CountErrors(compilation);

    if (originalErrors != 0)
        return new(
            originalErrors, -1, null, null, false);

    var owner = root.DescendantNodes()
        .OfType<ClassDeclarationSyntax>()
        .Single();

    var method = owner.Members
        .OfType<MethodDeclarationSyntax>()
        .Single(m => m.Identifier.ValueText == "Test");

    var body = method.Body
        ?? throw new InvalidOperationException(
            "Method body unavailable.");

    var selected = body.Statements[statementIndex];

    var originalInvocation = selected
        .DescendantNodesAndSelf()
        .OfType<InvocationExpressionSyntax>()
        .Single();

    var originalModel = compilation.GetSemanticModel(tree);

    var originalSymbol =
        originalModel.GetSymbolInfo(originalInvocation)
            .Symbol as IMethodSymbol;

    const string helperName =
        "__EVI_BindingPreservation_Helper";

    var helper = SyntaxFactory.MethodDeclaration(
            SyntaxFactory.PredefinedType(
                SyntaxFactory.Token(SyntaxKind.VoidKeyword)),
            helperName)
        .AddModifiers(
            SyntaxFactory.Token(SyntaxKind.PrivateKeyword))
        .WithBody(
            SyntaxFactory.Block(selected));

    var call = SyntaxFactory.ParseStatement(
        helperName + "();");

    var updatedStatements = body.Statements
        .Take(statementIndex)
        .Concat(new[] { call })
        .Concat(body.Statements.Skip(statementIndex + 1))
        .ToArray();

    var updatedMethod = method.WithBody(
        body.WithStatements(
            SyntaxFactory.List(updatedStatements)));

    var updatedOwner = owner
        .ReplaceNode(method, updatedMethod)
        .AddMembers(helper);

    var updatedRoot = root.ReplaceNode(
        owner, updatedOwner);

    var transformedTree = tree.WithRootAndOptions(
        updatedRoot,
        tree.Options);

    var transformedCompilation =
        compilation.ReplaceSyntaxTree(
            tree, transformedTree);

    int transformedErrors =
        CountErrors(transformedCompilation);

    if (transformedErrors != 0)
        return new(
            originalErrors,
            transformedErrors,
            Describe(originalSymbol),
            null,
            false);

    var transformedModel =
        transformedCompilation.GetSemanticModel(
            transformedTree);

    var transformedHelper = transformedTree.GetRoot()
        .DescendantNodes()
        .OfType<MethodDeclarationSyntax>()
        .Single(m =>
            m.Identifier.ValueText == helperName);

    var transformedInvocation =
        transformedHelper.Body!
        .DescendantNodes()
        .OfType<InvocationExpressionSyntax>()
        .Single();

    var transformedSymbol =
        transformedModel.GetSymbolInfo(
            transformedInvocation).Symbol as IMethodSymbol;

    bool preserved =
        originalSymbol is not null &&
        transformedSymbol is not null &&
        SymbolEqualityComparer.Default.Equals(
            originalSymbol, transformedSymbol);

    return new(
        originalErrors,
        transformedErrors,
        Describe(originalSymbol),
        Describe(transformedSymbol),
        preserved);
}

int CountErrors(CSharpCompilation compilation)
{
    return compilation.GetDiagnostics()
        .Count(d =>
            d.Severity == DiagnosticSeverity.Error);
}

string? Describe(IMethodSymbol? symbol)
{
    if (symbol is null)
        return null;

    return symbol.ToDisplayString(
        SymbolDisplayFormat.FullyQualifiedFormat);
}

internal sealed record Result(
    int OriginalErrors,
    int TransformedErrors,
    string? OriginalSymbol,
    string? TransformedSymbol,
    bool Preserved);
