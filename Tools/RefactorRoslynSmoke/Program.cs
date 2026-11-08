using EVI_Refactor;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

static void Check(bool condition, string message)
{
    if (!condition) throw new Exception(message);
    Console.WriteLine("PASS: " + message);
}

const string source = "class C { void M() { MissingWidget x = null; x = null; } }";
var tree = CSharpSyntaxTree.ParseText(source, path: "Smoke.cs");
var method = tree.GetRoot().DescendantNodes().OfType<MethodDeclarationSyntax>().Single();
var statements = method.Body!.Statements;
var group = new RefactorGroup(1, 1, statements.Count, 1, "Review", "Review", "", "", false, "");
group.SetSourceSpan(statements[0].SpanStart, statements[^1].Span.End);

var missing = RefactorSemanticBridge.Evaluate(tree, method, group);
Check(missing.Status == RefactorSafetyStatus.Unsupported, "unresolved symbols block semantic safety");
Check(missing.Reasons.Contains("Diagnostic:CS0246"), "unresolved type diagnostic retained");
Check(missing.Reasons.Contains("UnresolvedSymbolsOrReferences"), "unresolved reference reason retained");

var runtime = MetadataReference.CreateFromFile(typeof(object).Assembly.Location);
var supplied = RefactorSemanticBridge.Evaluate(tree, method, group, new[] { runtime });
Check(supplied.Status == RefactorSafetyStatus.Unsupported, "core reference does not conceal unresolved type");

var external = CSharpCompilation.Create(
    "External", new[] { CSharpSyntaxTree.ParseText("public class MissingWidget {}") },
    references: new[] { runtime },
    options: new CSharpCompilationOptions(OutputKind.DynamicallyLinkedLibrary));
var resolved = RefactorSemanticBridge.Evaluate(
    tree, method, group, new MetadataReference[] { runtime, external.ToMetadataReference() });
Check(!resolved.Reasons.Contains("Diagnostic:CS0246"), "explicit reference resolves external type");

var report = new RefactorEngine().AnalyzeSource(
    "Smoke.cs", source, maxMethodLines: 1,
    references: new MetadataReference[] { runtime, external.ToMetadataReference() });
Check(report.Contains("SemanticSafety:"), "engine emits semantic safety");
Check(report.Contains("SemanticReasons:"), "engine emits semantic reasons");
Check(!report.Contains("Diagnostic:CS0246"), "engine forwards references");

Console.WriteLine("Roslyn smoke tests passed.");
