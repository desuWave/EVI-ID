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
Check(missing.Diagnostics.Any(d => d.Id == "CS0246" && d.Line == 1 && d.Column > 0 && d.Path == "Smoke.cs" && d.Message.Length > 0), "diagnostics include source location and message");

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
Check(!resolved.Diagnostics.Any(d => d.Id == "CS0246"), "resolved type removed from detailed diagnostics");

// RefactorEngine only emits semantic candidate details for groups
// spanning at least 20 source lines. Exercise that path explicitly.
var engineSource = "class C\n{\n    void M()\n    {\n" +
    "        MissingWidget x = null;\n" +
    string.Concat(Enumerable.Repeat("        x = null;\n", 21)) +
    "    }\n}";
var report = new RefactorEngine().AnalyzeSource(
    "Smoke.cs", engineSource, maxMethodLines: 1,
    references: new MetadataReference[] { runtime, external.ToMetadataReference() });
Check(report.Contains("REFACTOR CANDIDATES:"), "engine finds an eligible extraction group");
Check(report.Contains("SemanticSafety:"), "engine emits semantic safety");
Check(report.Contains("SemanticReasons:"), "engine emits semantic reasons");
Check(!report.Contains("Diagnostic:CS0246"), "engine forwards references");
var unresolvedReport = new RefactorEngine().AnalyzeSource("Smoke.cs", engineSource, maxMethodLines: 1, references: new[] { runtime });
Check(unresolvedReport.Contains("SemanticDiagnostic: CS0246 (Smoke.cs:"), "engine reports located semantic diagnostics");
Check(unresolvedReport.Contains("SemanticSafety: Unsupported"), "diagnostics never relax safety");

Console.WriteLine("Roslyn smoke tests passed.");
