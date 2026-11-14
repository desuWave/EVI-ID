using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

namespace EVI_Refactor;

public enum RefactorSafetyStatus
{
    Safe,
    ReviewRequired,
    Unsupported
}

public sealed class RefactorDiagnosticDetail
{
    public string Id { get; }
    public string Message { get; }
    public string Path { get; }
    public int Line { get; }
    public int Column { get; }

    public RefactorDiagnosticDetail(string id, string message, string path, int line, int column)
    {
        Id = id;
        Message = message;
        Path = path;
        Line = line;
        Column = column;
    }

    public override string ToString() => $"{Id} ({Path}:{Line}:{Column}): {Message}";
}

public sealed class RefactorSafetyResult
{
    public RefactorSafetyStatus Status { get; }
    public IReadOnlyList<string> Reasons { get; }
    public IReadOnlyList<RefactorDiagnosticDetail> Diagnostics { get; }

    public RefactorSafetyResult(
        RefactorSafetyStatus status,
        IEnumerable<string> reasons,
        IEnumerable<RefactorDiagnosticDetail>? diagnostics = null)
    {
        Status = status;
        Reasons = reasons.ToArray();
        Diagnostics = diagnostics?.ToArray() ?? Array.Empty<RefactorDiagnosticDetail>();
    }
}

public static class RefactorSafetyAnalyzer
{
    public static RefactorSafetyResult Analyze(
        SemanticModel semanticModel,
        IReadOnlyList<StatementSyntax> statements)
    {
        var reasons = new List<string>();

        if (semanticModel == null ||
            statements == null ||
            statements.Count == 0)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "InvalidInput" });
        }

        if (statements.Any(s => s == null))
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "NullStatement" });
        }

        if (statements.Any(s =>
            !ReferenceEquals(
                s.SyntaxTree,
                semanticModel.SyntaxTree)))
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "SemanticTreeMismatch" });
        }

        if (statements.Any(s => s.Parent is not BlockSyntax) ||
            statements.Select(s => s.Parent).Distinct().Count() != 1)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "DifferentStatementBlocks" });
        }

        var block = (BlockSyntax)statements[0].Parent!;

        var ordered = statements
            .OrderBy(s => s.SpanStart)
            .ToArray();

        int firstIndex = block.Statements.IndexOf(ordered[0]);

        if (firstIndex < 0 ||
            ordered.Length > block.Statements.Count - firstIndex)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "InvalidStatementRange" });
        }

        for (int i = 0; i < ordered.Length; i++)
        {
            if (!ReferenceEquals(
                    block.Statements[firstIndex + i],
                    ordered[i]))
            {
                return new RefactorSafetyResult(
                    RefactorSafetyStatus.Unsupported,
                    new[] { "NonContiguousStatements" });
            }
        }

        var nodes = ordered
            .SelectMany(s => s.DescendantNodesAndSelf())
            .ToArray();

        if (nodes.Any(n =>
            n is ReturnStatementSyntax ||
            n is BreakStatementSyntax ||
            n is ContinueStatementSyntax ||
            n is GotoStatementSyntax ||
            n is ThrowStatementSyntax ||
            n is YieldStatementSyntax))
        {
            reasons.Add("ControlFlowRequiresReview");
        }

        if (nodes.Any(n =>
            n is AwaitExpressionSyntax ||
            n is LocalFunctionStatementSyntax ||
            n is AnonymousFunctionExpressionSyntax))
        {
            reasons.Add("NestedOrAsyncExecution");
        }

        ControlFlowAnalysis? controlFlow;

        try
        {
            controlFlow = semanticModel.AnalyzeControlFlow(
                ordered[0], ordered[^1]);
        }
        catch (ArgumentException)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "ControlFlowUnavailable" });
        }

        if (controlFlow == null || !controlFlow.Succeeded)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "ControlFlowUnavailable" });
        }

        if (controlFlow.EntryPoints.Any())
            reasons.Add("ControlFlowEntryPoints");

        if (controlFlow.ExitPoints.Any())
            reasons.Add("ControlFlowExitPoints");

        DataFlowAnalysis? flow;

        try
        {
            flow = semanticModel.AnalyzeDataFlow(
                ordered[0],
                ordered[^1]);
        }
        catch (ArgumentException)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "DataFlowUnavailable" });
        }

        if (flow == null || !flow.Succeeded)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.Unsupported,
                new[] { "DataFlowFailed" });
        }

        if (flow.DataFlowsIn.Any())
        {
            reasons.Add("ExternalInputs");
        }

        if (flow.DataFlowsOut.Any())
        {
            reasons.Add("ExternalOutputs");
        }

        if (flow.Captured.Any())
        {
            reasons.Add("CapturedVariables");
        }

        if (reasons.Count > 0)
        {
            return new RefactorSafetyResult(
                RefactorSafetyStatus.ReviewRequired,
                reasons);
        }

        // A successful data-flow check alone does not prove
        // that extraction preserves program behavior.
        return new RefactorSafetyResult(
            RefactorSafetyStatus.ReviewRequired,
            new[] { "SemanticValidationRequired" });
    }
}
