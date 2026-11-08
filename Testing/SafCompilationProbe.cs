using System;
using System.Linq;
using System.Text;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EVI_ID.Testing;

public static class SafCompilationProbe
{
    public static string Run(
        PortableExecutableReference reference)
    {
        var output = new StringBuilder();

        const string source = """
            public class Probe
            {
                public int Value => 42;
            }
            """;

        var tree = CSharpSyntaxTree.ParseText(source);

        output.AppendLine("Roslyn SAF Compilation Probe");

        Analyze(
            output,
            "Without references",
            tree,
            Array.Empty<MetadataReference>());

        Analyze(
            output,
            "With System.Runtime.dll",
            tree,
            new MetadataReference[] { reference });

        return output.ToString();
    }

    private static void Analyze(
        StringBuilder output,
        string label,
        SyntaxTree tree,
        MetadataReference[] references)
    {
        output.AppendLine(label);

        var compilation = CSharpCompilation.Create(
            "EVI_SAF_Probe",
            new[] { tree },
            references,
            new CSharpCompilationOptions(
                OutputKind.DynamicallyLinkedLibrary));

        var diagnostics = compilation.GetDiagnostics()
            .Where(d =>
                d.Severity == DiagnosticSeverity.Error)
            .GroupBy(d => d.Id)
            .OrderBy(g => g.Key)
            .ToArray();

        if (diagnostics.Length == 0)
        {
            output.AppendLine("Compilation diagnostics: NONE");
            return;
        }

        foreach (var group in diagnostics)
        {
            output.AppendLine(
                $"{group.Key}: {group.Count()}");
        }
    }
}
