using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;

using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;
using Microsoft.CodeAnalysis.Emit;

namespace EVI_ID.Mdl.Editor;

public sealed class EditorCompileResult
{
    public bool Success { get; }

    public Assembly? Assembly { get; }

    public IReadOnlyList<string> Diagnostics { get; }

    public EditorCompileResult(
        bool success,
        Assembly? assembly,
        IReadOnlyList<string> diagnostics)
    {
        Success = success;
        Assembly = assembly;
        Diagnostics = diagnostics;
    }
}

public sealed class EditorCompiler
{
    public EditorCompileResult Compile(
        string source,
        string assemblyName)
    {
        SyntaxTree syntaxTree =
            CSharpSyntaxTree.ParseText(
                source);

        List<MetadataReference> references =
            GetReferences();

        CSharpCompilation compilation =
            CSharpCompilation.Create(
                assemblyName,
                new[]
                {
                    syntaxTree
                },
                references,
                new CSharpCompilationOptions(
                    OutputKind.DynamicallyLinkedLibrary));

        using MemoryStream stream =
            new();

        EmitResult result =
            compilation.Emit(stream);

        List<string> diagnostics =
            result.Diagnostics
                .Where(
                    diagnostic =>
                        diagnostic.Severity ==
                            DiagnosticSeverity.Error)
                .Select(
                    diagnostic =>
                        diagnostic.ToString())
                .ToList();

        if (!result.Success)
        {
            return new EditorCompileResult(
                false,
                null,
                diagnostics);
        }

        stream.Position = 0;

        Assembly assembly =
            Assembly.Load(
                stream.ToArray());

        return new EditorCompileResult(
            true,
            assembly,
            diagnostics);
    }

    private static List<MetadataReference>
    GetReferences()
{
    List<MetadataReference> references =
        new();

    HashSet<string> added =
        new(
            StringComparer.OrdinalIgnoreCase);

    void AddReference(
        string? path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return;

        if (!File.Exists(path))
            return;

        if (!added.Add(path))
            return;

        try
        {
            references.Add(
                MetadataReference.CreateFromFile(
                    path));
        }
        catch
        {
        }
    }

    string? trustedPlatformAssemblies =
        AppContext.GetData(
            "TRUSTED_PLATFORM_ASSEMBLIES")
        as string;

    if (!string.IsNullOrWhiteSpace(
            trustedPlatformAssemblies))
    {
        string[] paths =
            trustedPlatformAssemblies.Split(
                Path.PathSeparator,
                StringSplitOptions.RemoveEmptyEntries);

        foreach (string path in paths)
        {
            AddReference(path);
        }
    }

    foreach (Assembly assembly in
             AppDomain.CurrentDomain.GetAssemblies())
    {
        if (assembly.IsDynamic)
            continue;

        AddReference(
            assembly.Location);
    }

    string? baseDirectory =
        AppContext.BaseDirectory;

    if (Directory.Exists(baseDirectory))
    {
        foreach (string path in
                 Directory.GetFiles(
                     baseDirectory,
                     "*.dll",
                     SearchOption.AllDirectories))
        {
            AddReference(path);
        }
    }

    return references;
}

}