using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_Refactor;

public sealed class RefactorCandidate
{
    public string Kind { get; }
    public string Target { get; }
    public int Line { get; }
    public int CurrentSize { get; }
    public int Limit { get; }
    public string Action { get; }

    public RefactorCandidate(
        string kind,
        string target,
        int line,
        int currentSize,
        int limit,
        string action)
    {
        Kind = kind;
        Target = target;
        Line = line;
        CurrentSize = currentSize;
        Limit = limit;
        Action = action;
    }
}

public sealed class RefactorGroup
{
    public int StartLine { get; }

    public int EndLine { get; }

    public int StatementCount { get; }

    public int Size { get; }

    public string Action { get; }

    public string Safety { get; }
    
    public string Reason { get; }
    
    public string ExitLines { get; }

    public bool ContainsLambda { get; }
    
    public string LambdaKind { get; }

    public int SpanStart { get; private set; } = -1;
    public int SpanEnd { get; private set; } = -1;

    public void SetSourceSpan(int start, int end)
    {
        SpanStart = start;
        SpanEnd = end;
    }

    public RefactorGroup(
        int startLine,
        int endLine,
        int statementCount,
        int size,
        string action,
        string safety,
        string reason,
        string exitLines,
        bool containsLambda,
        string lambdaKind)
    {
        StartLine = startLine;
        EndLine = endLine;
        StatementCount = statementCount;
        Size = size;
        Action = action;
        Safety = safety;
        Reason = reason;
        ExitLines = exitLines;
        ContainsLambda = containsLambda;
        LambdaKind = lambdaKind;
    }
}

public sealed class RefactorPlan
{
    private readonly List<RefactorCandidate> _candidates =
        new();

    public IReadOnlyList<RefactorCandidate> Candidates =>
        _candidates;

    public void Add(
        RefactorCandidate candidate)
    {
        _candidates.Add(candidate);
    }
}

public sealed class RefactorEngine
{
    public RefactorGroupDiscovery DiscoverGroups(
        IReadOnlyList<(string Name, string Content)> sources,
        int maxFileLines = DefaultMaxFileLines,
        int maxMethodLines = DefaultMaxMethodLines,
        int maxDepth = DefaultMaxDepth,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);
        string hash = RefactorSourceIdentity.Compute(sources);
        var discovered = new List<RefactorDiscoveredGroup>();
        AnalyzeSourcesCore(
            "<memory>", sources, maxFileLines,
            maxMethodLines, maxDepth,
            references, discovered);
        return new RefactorGroupDiscovery(
            hash, discovered.AsReadOnly());
    }

    public const int DefaultMaxFileLines = 500;
    public const int DefaultMaxMethodLines = 100;
    public const int DefaultMaxDepth = 6;

    public int Analyze(
        string sourcePath,
        int maxFileLines = DefaultMaxFileLines,
        int maxMethodLines = DefaultMaxMethodLines,
        int maxDepth = DefaultMaxDepth)
    {
        if (string.IsNullOrWhiteSpace(sourcePath))
        {
            Console.WriteLine(
                "Source path is empty.");

            return 1;
        }

        if (File.Exists(sourcePath))
        {
            if (!sourcePath.EndsWith(
                ".cs",
                StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"Skipping non-C# file: {sourcePath}");

                return 0;
            }

            string source =
                File.ReadAllText(sourcePath);

            string output =
                AnalyzeSource(
                    sourcePath,
                    source,
                    maxFileLines,
                    maxMethodLines,
                    maxDepth);

            Console.Write(output);

            return 0;
        }

        if (Directory.Exists(sourcePath))
        {
            string[] files =
                Directory.GetFiles(
                    sourcePath,
                    "*.cs",
                    SearchOption.AllDirectories);

            Array.Sort(
                files,
                StringComparer.OrdinalIgnoreCase);

            var sources = files
                .Select(file => (
                    Name: file,
                    Content: File.ReadAllText(file)))
                .ToArray();

            string output = AnalyzeSources(
                sourcePath,
                sources,
                isDirectory: true,
                maxFileLines: maxFileLines,
                maxMethodLines: maxMethodLines,
                maxDepth: maxDepth);

            Console.Write(output);

            return 0;
        }

        Console.WriteLine(
            $"Path not found: {sourcePath}");

        return 1;
    }

    public string AnalyzeSources(
        string sourcePath,
        IReadOnlyList<(string Name, string Content)> sources,
        bool isDirectory,
        int maxFileLines = DefaultMaxFileLines,
        int maxMethodLines = DefaultMaxMethodLines,
        int maxDepth = DefaultMaxDepth,
        IEnumerable<MetadataReference>? references = null)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (!isDirectory)
        {
            if (sources.Count == 0)
            {
                return $"Skipping non-C# file: {sourcePath}"
                    + Environment.NewLine;
            }

            var source = sources[0];

            return AnalyzeSource(
                source.Name,
                source.Content,
                maxFileLines,
                maxMethodLines,
                maxDepth,
                references);
        }

        return AnalyzeSourcesCore(
            sourcePath, sources, maxFileLines,
            maxMethodLines, maxDepth,
            references, null);
    }

    private static string AnalyzeSourcesCore(
        string sourcePath,
        IReadOnlyList<(string Name, string Content)> sources,
        int maxFileLines,
        int maxMethodLines,
        int maxDepth,
        IEnumerable<MetadataReference>? references,
        List<RefactorDiscoveredGroup>? discovered)
    {
        StringBuilder output = new();

        output.AppendLine("EVI-Refactor Analysis");
        output.AppendLine();
        output.AppendLine($"Root: {sourcePath}");
        output.AppendLine($"C# files: {sources.Count}");
        output.AppendLine();

        var orderedSources = sources.OrderBy(
            x => x.Name,
            StringComparer.OrdinalIgnoreCase).ToArray();

        var trees = orderedSources.Select(source =>
            CSharpSyntaxTree.ParseText(
                source.Content,
                path: source.Name)).ToArray();

        var compilation = CSharpCompilation.Create(
            "EVIRefactorSemanticAnalysis",
            trees,
            references: references,
            options: new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        for (int i = 0; i < orderedSources.Length; i++)
        {
            var source = orderedSources[i];
            AnalyzeSourceCore(
                source.Name,
                source.Content,
                maxFileLines,
                maxMethodLines,
                maxDepth,
                output,
                references,
                trees[i],
                compilation, discovered);
        }

        return output.ToString();
    }

    public string AnalyzeSource(
        string sourceName,
        string source,
        int maxFileLines = DefaultMaxFileLines,
        int maxMethodLines = DefaultMaxMethodLines,
        int maxDepth = DefaultMaxDepth,
        IEnumerable<MetadataReference>? references = null)
    {
        if (string.IsNullOrWhiteSpace(sourceName))
        {
            sourceName = "<memory>";
        }

        if (source == null)
        {
            return
                $"Source is null: {sourceName}"
                + Environment.NewLine;
        }

        StringBuilder output =
            new();

        AnalyzeSourceCore(
            sourceName,
            source,
            maxFileLines,
            maxMethodLines,
            maxDepth,
            output,
            references);

        return output.ToString();
    }

    private static void AnalyzeSourceCore(
string sourceName,
string source,
int maxFileLines,
int maxMethodLines,
int maxDepth,
StringBuilder output,
IEnumerable<MetadataReference>? references,
SyntaxTree? sharedTree = null,
    CSharpCompilation? sharedCompilation = null,
    List<RefactorDiscoveredGroup>? discovered = null)
{
string normalizedSource =
source
.Replace("\r\n", "\n")
.Replace('\r', '\n');

string[] lines =  
        normalizedSource.Split('\n');  

    SyntaxTree tree = sharedTree ??
        CSharpSyntaxTree.ParseText(  
            source,  
            path: sourceName);  

    CompilationUnitSyntax root =  
        tree.GetCompilationUnitRoot();  

    List<ClassDeclarationSyntax> classes =  
        root.DescendantNodes()  
            .OfType<ClassDeclarationSyntax>()  
            .ToList();  

    List<StructDeclarationSyntax> structs =  
        root.DescendantNodes()  
            .OfType<StructDeclarationSyntax>()  
            .ToList();  

    List<InterfaceDeclarationSyntax> interfaces =  
        root.DescendantNodes()  
            .OfType<InterfaceDeclarationSyntax>()  
            .ToList();  

    List<MethodDeclarationSyntax> methods =  
        root.DescendantNodes()  
            .OfType<MethodDeclarationSyntax>()  
            .ToList();  

    int maxNestingDepth =  
        GetMaximumNestingDepth(root);  

    List<string> diagnostics =  
        new();  

    RefactorPlan plan =  
        new();  

    List<MethodDeclarationSyntax> largeMethods =  
        new();  

    if (lines.Length > maxFileLines)  
    {  
        diagnostics.Add(  
            $"[LARGE FILE] {lines.Length} > " +  
            $"{maxFileLines}");  

        plan.Add(  
            new RefactorCandidate(  
                "LargeFile",  
                Path.GetFileName(sourceName),  
                1,  
                lines.Length,  
                maxFileLines,  
                "CandidateForResponsibilityDecomposition"));  
    }  

    if (maxNestingDepth > maxDepth)  
    {  
        diagnostics.Add(  
            $"[DEEP NESTING] {maxNestingDepth} > " +  
            $"{maxDepth}");  

        plan.Add(  
            new RefactorCandidate(  
                "DeepNesting",  
                Path.GetFileName(sourceName),  
                1,  
                maxNestingDepth,  
                maxDepth,  
                "CandidateForNestingSimplification"));  
    }  

    foreach (MethodDeclarationSyntax method in methods)  
    {  
        int methodLines =  
            GetNodeLineCount(method);  

        if (methodLines > maxMethodLines)  
        {  
            int line =  
                GetStartLine(method);  

            largeMethods.Add(method);  

            diagnostics.Add(  
                $"[LARGE METHOD] " +  
                $"{method.Identifier.Text}: " +  
                $"{methodLines} > " +  
                $"{maxMethodLines} " +  
                $"(line {line})");  

            plan.Add(  
                new RefactorCandidate(  
                    "LargeMethod",  
                    method.Identifier.Text,  
                    line,  
                    methodLines,  
                    maxMethodLines,  
                    "CandidateForMethodDecomposition"));  
        }  
    }  

    output.AppendLine(  
        $"FILE: {Path.GetFileName(sourceName)}");  

    output.AppendLine(  
        $"  Path: {sourceName}");  

    output.AppendLine(  
        $"  Lines: {lines.Length}");  

    output.AppendLine(  
        $"  Classes: {classes.Count}");  

    output.AppendLine(  
        $"  Structs: {structs.Count}");  

    output.AppendLine(  
        $"  Interfaces: {interfaces.Count}");  

    output.AppendLine(  
        $"  Methods: {methods.Count}");  

    output.AppendLine(  
        $"  Max nesting depth: {maxNestingDepth}");  

    if (diagnostics.Count > 0)  
    {  
        output.AppendLine(  
            "  Diagnostics:");  

        foreach (string diagnostic in diagnostics)  
        {  
            output.AppendLine(  
                $"    {diagnostic}");  
        }  
    }  

    if (plan.Candidates.Count > 0)  
    {  
        output.AppendLine(  
            "  REFACTOR PLAN:");  

        foreach (RefactorCandidate candidate  
                 in plan.Candidates)  
        {  
            output.AppendLine(  
                $"    {candidate.Target}");  

            output.AppendLine(  
                $"      Kind: " +  
                $"{candidate.Kind}");  

            output.AppendLine(  
                $"      Line: " +  
                $"{candidate.Line}");  

            output.AppendLine(  
                $"      Current size: " +  
                $"{candidate.CurrentSize}");  

            output.AppendLine(  
                $"      Limit: " +  
                $"{candidate.Limit}");  

            output.AppendLine(  
                $"      Action: " +  
                $"{candidate.Action}");  
        }  
    }  

    if (largeMethods.Count > 0)  
    {  
        output.AppendLine(  
            "  LARGE METHOD STRUCTURE:");  

        foreach (MethodDeclarationSyntax method  
                 in largeMethods)  
        {  
            output.AppendLine(  
                $"    {method.Identifier.Text}");  

            List<SyntaxNode> blocks =  
                GetControlBlocks(method);  

            if (blocks.Count == 0)  
            {  
                output.AppendLine(  
                    "      No control blocks found.");  
            }  
            else  
            {  
                foreach (SyntaxNode block in blocks)  
                {  
                    output.AppendLine(  
                        $"      Block: " +  
                        $"{GetBlockKind(block)}");  

                    output.AppendLine(  
                        $"        Line: " +  
                        $"{GetStartLine(block)}");  

                    output.AppendLine(  
                        $"        Lines: " +  
                        $"{GetNodeLineCount(block)}");  
                }  
            }  

            List<SyntaxNode> statements =  
                GetTopLevelStatements(method);  

            output.AppendLine(  
                "      Statements:");  

            if (statements.Count == 0)  
            {  
                output.AppendLine(  
                    "        No statements found.");  
            }  
            else  
            {  
                foreach (SyntaxNode statement  
                         in statements)  
                {  
                    output.AppendLine(  
                        $"        {GetStatementKind(statement)}");  

                    output.AppendLine(  
                        $"          Line: " +  
                        $"{GetStartLine(statement)}");  

                    output.AppendLine(  
                        $"          Lines: " +  
                        $"{GetNodeLineCount(statement)}");  
                }  
            }  
              
            List<RefactorGroup> groups =  
GetRefactorGroups(  
    statements,  
    source);

if (groups.Count > 0)
{
output.AppendLine(
"      REFACTOR CANDIDATES:");

foreach (RefactorGroup group in groups)  
{  
    discovered?.Add(new RefactorDiscoveredGroup(
        sourceName,
        method.Identifier.ValueText,
        method.SpanStart,
        method.Span.End,
        group,
        group.SpanStart,
        group.SpanEnd,
        group.StatementCount));
    var semanticResult = sharedCompilation is null
        ? RefactorSemanticBridge.Evaluate(
            tree, method, group, references)
        : RefactorSemanticBridge.Evaluate(
            tree, method, group, sharedCompilation);

    output.AppendLine(
        $"          SemanticSafety: {semanticResult.Status}");

    output.AppendLine(
        $"          SemanticReasons: {string.Join(",", semanticResult.Reasons)}");
    foreach (var diagnostic in semanticResult.Diagnostics)
    {
        output.AppendLine($"          SemanticDiagnostic: {diagnostic}");
    }
    output.AppendLine(  
        $"        Lines: " +  
        $"{group.StartLine}-{group.EndLine}");  

    output.AppendLine(  
        $"          Statements: " +  
        $"{group.StatementCount}");  

    output.AppendLine(  
        $"          Size: " +  
        $"{group.Size}");  

    output.AppendLine(  
        $"          Action: " +  
        $"{group.Action}");  
          
    output.AppendLine(  
       $"          Safety: " +  
       $"{group.Safety}");  
         
       output.AppendLine(  
       $"          Reason: " +  
       $"{group.Reason}");  
         
       if (group.ExitLines != "None")  
       {  
       output.AppendLine(  
       $"          ExitLines: " +  
       $"{group.ExitLines}");  
         
       List<string> lambdaDiagnostics =  
       GetLambdaDiagnostics(  
       statements);  

       if (lambdaDiagnostics.Count > 0)

{
output.AppendLine(
"      LAMBDAS:");

foreach (string diagnostic in
lambdaDiagnostics)
{
output.AppendLine(
$"        {diagnostic}");

if (group.ContainsLambda)    
   {    
   output.AppendLine(    
   $"          ContainsLambda: Yes");    
       
   if (group.LambdaKind != "None")    
   {    
   output.AppendLine(    
   $"          LambdaKind: " +    
   $"{group.LambdaKind}");    
   }    
  }  
   }  
 }  
       }  
     }  
   }  
 }
}

        if (classes.Count > 0)
        {
            output.AppendLine(
                "  Types:");

            foreach (ClassDeclarationSyntax item in classes)
            {
                output.AppendLine(
                    $"    class {item.Identifier.Text} " +
                    $"(line {GetStartLine(item)})");
            }
        }

        if (methods.Count > 0)
        {
            output.AppendLine(
                "  Methods:");

            foreach (MethodDeclarationSyntax method in methods)
            {
                int methodLines =
                    GetNodeLineCount(method);

                string marker =
                    methodLines > maxMethodLines
                        ? " [LARGE METHOD]"
                        : "";

                output.AppendLine(
                    $"    {method.Identifier.Text} " +
                    $"— {methodLines} lines " +
                    $"(line {GetStartLine(method)})" +
                    marker);
            }
        }

        output.AppendLine();
    }

    private static List<SyntaxNode> GetControlBlocks(
        MethodDeclarationSyntax method)
    {
        return method
            .DescendantNodes()
            .Where(node =>
                node is IfStatementSyntax ||
                node is ElseClauseSyntax ||
                node is ForStatementSyntax ||
                node is ForEachStatementSyntax ||
                node is WhileStatementSyntax ||
                node is DoStatementSyntax ||
                node is SwitchStatementSyntax ||
                node is TryStatementSyntax ||
                node is CatchClauseSyntax ||
                node is FinallyClauseSyntax)
            .OrderBy(GetStartLine)
            .ToList();
    }

    private static List<SyntaxNode> GetTopLevelStatements(
        MethodDeclarationSyntax method)
    {
        if (method.Body == null)
        {
            return new List<SyntaxNode>();
        }

        return method.Body
            .ChildNodes()
            .ToList();
    }

    private static string GetBlockKind(
        SyntaxNode node)
    {
        return node switch
        {
            IfStatementSyntax =>
                "If",

            ElseClauseSyntax =>
                "Else",

            ForStatementSyntax =>
                "For",

            ForEachStatementSyntax =>
                "ForEach",

            WhileStatementSyntax =>
                "While",

            DoStatementSyntax =>
                "Do",

            SwitchStatementSyntax =>
                "Switch",

            TryStatementSyntax =>
                "Try",

            CatchClauseSyntax =>
                "Catch",

            FinallyClauseSyntax =>
                "Finally",

            _ =>
                node.Kind().ToString()
        };
    }

    private static string GetStatementKind(
        SyntaxNode node)
    {
        return node.Kind().ToString();
    }

    private static int GetStartLine(
        SyntaxNode node)
    {
        return node
            .GetLocation()
            .GetLineSpan()
            .StartLinePosition
            .Line + 1;
    }

    private static int GetNodeLineCount(
        SyntaxNode node)
    {
        FileLinePositionSpan span =
            node.GetLocation()
                .GetLineSpan();

        return span.EndLinePosition.Line
             - span.StartLinePosition.Line
             + 1;
    }

    private static int GetMaximumNestingDepth(
        SyntaxNode root)
    {
        int maximum = 0;

        Visit(
            root,
            0,
            ref maximum);

        return maximum;
    }

    private static void Visit(
        SyntaxNode node,
        int depth,
        ref int maximum)
    {
        if (node is BlockSyntax ||
            node is ClassDeclarationSyntax ||
            node is StructDeclarationSyntax ||
            node is InterfaceDeclarationSyntax ||
            node is MethodDeclarationSyntax ||
            node is NamespaceDeclarationSyntax ||
            node is FileScopedNamespaceDeclarationSyntax)
        {
            depth++;

            if (depth > maximum)
            {
                maximum = depth;
            }
        }

        foreach (SyntaxNode child in node.ChildNodes())
        {
            Visit(
                child,
                depth,
                ref maximum);
        }
    }
    
    private static List<RefactorGroup> GetRefactorGroups(
    List<SyntaxNode> statements,
    string source)
{
    const int minimumGroupSize = 20;

    List<RefactorGroup> groups =
        new();

    if (statements.Count == 0)
    {
        return groups;
    }

    int startLine = 0;
    int endLine = 0;
    int statementCount = 0;

    List<SyntaxNode> groupStatements =
        new();

    foreach (SyntaxNode statement in statements)
    {
        int currentStart =
            GetStartLine(statement);

        int currentEnd =
            GetEndLine(statement);

        bool structuralBoundary =
            IsStructuralBoundary(statement);

        bool lambdaBoundary =
            IsLambdaStatement(statement);

        if (statementCount == 0)
        {
            startLine = currentStart;
            endLine = currentEnd;
            statementCount = 1;

            groupStatements.Clear();
            groupStatements.Add(statement);

            if (structuralBoundary ||
                lambdaBoundary)
            {
                AddRefactorGroup(
                    groups,
                    startLine,
                    endLine,
                    groupStatements);

                startLine = 0;
                endLine = 0;
                statementCount = 0;
                groupStatements.Clear();
            }

            continue;
        }

        if (structuralBoundary ||
            lambdaBoundary)
        {
            AddRefactorGroup(
                groups,
                startLine,
                endLine,
                groupStatements);

            startLine = currentStart;
            endLine = currentEnd;
            statementCount = 1;

            groupStatements.Clear();
            groupStatements.Add(statement);

            if (structuralBoundary ||
                lambdaBoundary)
            {
                AddRefactorGroup(
                    groups,
                    startLine,
                    endLine,
                    groupStatements);

                startLine = 0;
                endLine = 0;
                statementCount = 0;
                groupStatements.Clear();
            }

            continue;
        }

        endLine = currentEnd;
        statementCount++;

        groupStatements.Add(statement);

        int currentSize =
            endLine - startLine + 1;

        if (currentSize >= minimumGroupSize)
        {
            AddRefactorGroup(
                groups,
                startLine,
                endLine,
                groupStatements);

            startLine = 0;
            endLine = 0;
            statementCount = 0;
            groupStatements.Clear();
        }
    }

    if (statementCount > 0)
    {
        AddRefactorGroup(
            groups,
            startLine,
            endLine,
            groupStatements);
    }

    return groups;
}

private static bool IsLambdaStatement(
    SyntaxNode node)
{
    return
        node is ExpressionStatementSyntax expression &&
        expression
            .DescendantNodesAndSelf()
            .OfType<LambdaExpressionSyntax>()
            .Any();
}

private static string GetLambdaKind(
    LambdaExpressionSyntax lambda)
{
    if (lambda.Parent is AssignmentExpressionSyntax assignment &&
        assignment.Kind() ==
            SyntaxKind.AddAssignmentExpression &&
        assignment.Right == lambda)
    {
        return "EventHandlerLambda";
    }

    return "Lambda";
}

private static List<string> GetLambdaDiagnostics(
    List<SyntaxNode> statements)
{
    List<string> diagnostics =
        new();

    foreach (SyntaxNode statement in statements)
    {
        if (statement is not ExpressionStatementSyntax expression)
        {
            continue;
        }

        List<LambdaExpressionSyntax> lambdas =
            expression
                .DescendantNodesAndSelf()
                .OfType<LambdaExpressionSyntax>()
                .ToList();

        foreach (LambdaExpressionSyntax lambda in lambdas)
        {
            int startLine =
                GetStartLine(lambda);

            int endLine =
                GetEndLine(lambda);

            string lambdaKind =
                GetLambdaKind(
                    lambda);

            diagnostics.Add(
                $"{lambdaKind}: {startLine}-{endLine}");
        }
    }

    return diagnostics;
}

private static bool IsStructuralBoundary(
    SyntaxNode node)
{
    return
        node is IfStatementSyntax ||
        node is ElseClauseSyntax ||
        node is ForStatementSyntax ||
        node is ForEachStatementSyntax ||
        node is WhileStatementSyntax ||
        node is DoStatementSyntax ||
        node is SwitchStatementSyntax ||
        node is TryStatementSyntax ||
        node is CatchClauseSyntax ||
        node is FinallyClauseSyntax ||
        node is LocalFunctionStatementSyntax;
}

private static void AddRefactorGroup(
    List<RefactorGroup> groups,
    int startLine,
    int endLine,
    List<SyntaxNode> statements)
{
    const int minimumGroupSize = 20;

    if (startLine <= 0 ||
        endLine < startLine ||
        statements.Count == 0)
    {
        return;
    }

    int size =
        endLine - startLine + 1;

    if (size < minimumGroupSize)
    {
        return;
    }

    string reason =
    GetControlFlowExitReason(
        statements);

    string safety =
    reason == "None"
        ? "Candidate"
        : "ReviewRequired";

    bool containsLambda =
    statements.Any(
        IsLambdaStatement);

string exitLines =
    GetControlFlowExitLines(
        statements);

List<string> lambdaKinds =
    statements
        .SelectMany(
            statement =>
                statement
                    .DescendantNodesAndSelf()
                    .OfType<LambdaExpressionSyntax>())
        .Select(GetLambdaKind)
        .Distinct()
        .ToList();

string lambdaKind =
    lambdaKinds.Count switch
    {
        0 => "None",
        1 => lambdaKinds[0],
        _ => "Mixed"
    };

groups.Add(
        new RefactorGroup(
            startLine,
            endLine,
            statements.Count,
            size,
            "CandidateForExtraction",
            safety,
            reason,
            exitLines,
            containsLambda,
            lambdaKind));

    groups[groups.Count - 1].SetSourceSpan(
        statements[0].SpanStart,
        statements[statements.Count - 1].Span.End);
}

private static string GetControlFlowExitReason(
    List<SyntaxNode> statements)
{
    bool hasReturn = false;
    bool hasBreak = false;
    bool hasContinue = false;

    foreach (SyntaxNode statement in statements)
    {
        if (statement is ReturnStatementSyntax)
        {
            hasReturn = true;
        }

        if (statement is BreakStatementSyntax)
        {
            hasBreak = true;
        }

        if (statement is ContinueStatementSyntax)
        {
            hasContinue = true;
        }

        foreach (SyntaxNode node in
            statement.DescendantNodes())
        {
            if (node is ReturnStatementSyntax)
            {
                hasReturn = true;
            }

            if (node is BreakStatementSyntax)
            {
                hasBreak = true;
            }

            if (node is ContinueStatementSyntax)
            {
                hasContinue = true;
            }
        }
    }

    List<string> reasons =
        new();

    if (hasReturn)
    {
        reasons.Add("ContainsReturn");
    }

    if (hasBreak)
    {
        reasons.Add("ContainsBreak");
    }

    if (hasContinue)
    {
        reasons.Add("ContainsContinue");
    }

    if (reasons.Count == 0)
    {
        return "None";
    }

    return string.Join(
        ",",
        reasons);
}

private static string GetControlFlowExitLines(
    List<SyntaxNode> statements)
{
    List<int> lines =
        new();

    foreach (SyntaxNode statement in statements)
    {
        foreach (SyntaxNode node in
            statement.DescendantNodesAndSelf())
        {
            if (node is not ReturnStatementSyntax &&
                node is not BreakStatementSyntax &&
                node is not ContinueStatementSyntax)
            {
                continue;
            }

            FileLinePositionSpan span =
                node.GetLocation().GetLineSpan();

            int line =
                span.StartLinePosition.Line + 1;

            if (!lines.Contains(line))
            {
                lines.Add(line);
            }
        }
    }

    lines.Sort();

    if (lines.Count == 0)
    {
        return "None";
    }

    return string.Join(
        ",",
        lines);
}

private static bool HasBlankLineBetween(
    string source,
    int previousEndLine,
    int currentStartLine)
{
    if (currentStartLine <= previousEndLine + 1)
    {
        return false;
    }

    string normalized =
        source
            .Replace("\r\n", "\n")
            .Replace('\r', '\n');

    string[] lines =
        normalized.Split('\n');

    int firstIndex =
        previousEndLine;

    int lastIndex =
        currentStartLine - 1;

    for (int i = firstIndex;
         i < lastIndex && i < lines.Length;
         i++)
    {
        if (string.IsNullOrWhiteSpace(lines[i]))
        {
            return true;
        }
    }

    return false;
}

private static int GetEndLine(
    SyntaxNode node)
{
    return node
        .GetLocation()
        .GetLineSpan()
        .EndLinePosition
        .Line + 1;
}
    
}
