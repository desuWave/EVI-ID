using System;
using System.Collections.Generic;
using System.Linq;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.CSharp.Syntax;

internal static class ExtractionTransformationBuilder
{
    internal const string HelperName =
        "__EVI_ExtractionValidation_Helper";

    public static SyntaxTree Build(
        MethodDeclarationSyntax method,
        IReadOnlyList<StatementSyntax> statements)
    {
        ArgumentNullException.ThrowIfNull(method);
        ArgumentNullException.ThrowIfNull(statements);

        if (method.Parent is not ClassDeclarationSyntax owner ||
            method.Body is null ||
            method.SyntaxTree is null ||
            statements.Count == 0)
        {
            throw new ArgumentException(
                "Unsupported extraction input.");
        }

        var body = method.Body;
        int start = body.Statements.IndexOf(statements[0]);

        if (start < 0 ||
            statements.Count > body.Statements.Count - start)
        {
            throw new ArgumentException(
                "Invalid statement range.");
        }

        for (int i = 0; i < statements.Count; i++)
        {
            if (!ReferenceEquals(
                body.Statements[start + i],
                statements[i]))
            {
                throw new ArgumentException(
                    "Statements must be contiguous.");
            }
        }

        var helperDeclaration =
            SyntaxFactory.ParseMemberDeclaration(
                "private void " + HelperName + "() {}")
            as MethodDeclarationSyntax
            ?? throw new InvalidOperationException(
                "Failed to create helper declaration.");

        var helper = helperDeclaration.WithBody(
            SyntaxFactory.Block(statements));

        var call = SyntaxFactory.ParseStatement(
            HelperName + "();");

        var updatedStatements = body.Statements
            .Take(start)
            .Concat(new[] { call })
            .Concat(body.Statements.Skip(
                start + statements.Count))
            .ToArray();

        var updatedMethod = method.WithBody(
            body.WithStatements(
                SyntaxFactory.List(updatedStatements)));

        var updatedOwner = owner
            .ReplaceNode(method, updatedMethod)
            .AddMembers(helper);

        var tree = method.SyntaxTree;
        var root = tree.GetCompilationUnitRoot();

        var updatedRoot = root.ReplaceNode(
            owner, updatedOwner);

        return tree.WithRootAndOptions(
            updatedRoot, tree.Options);
    }
}
