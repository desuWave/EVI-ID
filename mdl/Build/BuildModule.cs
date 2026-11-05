using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;
using System.Threading.Tasks;
using Microsoft.CodeAnalysis;
using Microsoft.CodeAnalysis.CSharp;

namespace EVI_ID.Mdl.Build;

public sealed class BuildModule
{
    public async Task<(int ExitCode, string Output)> BuildAsync(
        string projectDirectory)
    {
        string? logPath = null;

        try
        {
            if (string.IsNullOrWhiteSpace(projectDirectory))
                return (1, "Build error: project directory is empty.\n");

            projectDirectory =
                Path.GetFullPath(projectDirectory);

            logPath = FindBuildLogPath(projectDirectory);

            WriteLog(
                logPath,
                "==================================================");

            WriteLog(
                logPath,
                $"[BuildModule] ENTER {DateTime.Now:O}");

            WriteLog(
                logPath,
                $"[BuildModule] Project: {projectDirectory}");

            if (!Directory.Exists(projectDirectory))
            {
                WriteLog(
                    logPath,
                    "[BuildModule] Project directory does not exist.");

                return (
                    1,
                    $"Build error: directory not found:\n{projectDirectory}\n");
            }

            WriteLog(
                logPath,
                "[BuildModule] Source scan started.");

            string[] sourceFiles =
                Directory
                    .GetFiles(
                        projectDirectory,
                        "*.cs",
                        SearchOption.AllDirectories)
                    .Where(
                        path => !IsExcluded(
                            projectDirectory,
                            path))
                    .OrderBy(path => path)
                    .ToArray();

            WriteLog(
                logPath,
                $"[BuildModule] Source files found: {sourceFiles.Length}");

            if (sourceFiles.Length == 0)
            {
                WriteLog(
                    logPath,
                    "[BuildModule] ERROR: no C# source files.");

                return (
                    1,
                    "Build error: no C# source files found.\n");
            }

            WriteLog(
                logPath,
                "[BuildModule] Parsing started.");

            var syntaxTrees =
                new List<SyntaxTree>();

            foreach (string sourceFile in sourceFiles)
            {
                WriteLog(
                    logPath,
                    $"[BuildModule] Parsing: {sourceFile}");

                string source =
                    await File.ReadAllTextAsync(
                        sourceFile);

                SyntaxTree tree =
                    CSharpSyntaxTree.ParseText(
                        source,
                        new CSharpParseOptions(
                            LanguageVersion.Latest),
                        sourceFile);

                syntaxTrees.Add(tree);
            }

            WriteLog(
                logPath,
                $"[BuildModule] Syntax trees created: {syntaxTrees.Count}");

            WriteLog(
                logPath,
                "[BuildModule] Metadata reference collection started.");

            MetadataReference[] references =
                CollectReferences(logPath);

            WriteLog(
                logPath,
                $"[BuildModule] Metadata references: {references.Length}");

            WriteLog(
                logPath,
                "[BuildModule] Creating Roslyn compilation.");

            CSharpCompilation compilation =
                CSharpCompilation.Create(
                    assemblyName:
                        Path.GetFileName(projectDirectory),
                    syntaxTrees:
                        syntaxTrees,
                    references:
                        references,
                    options:
                        new CSharpCompilationOptions(
                            OutputKind.ConsoleApplication,
                            optimizationLevel:
                                OptimizationLevel.Release,
                            nullableContextOptions:
                                NullableContextOptions.Enable));

            WriteLog(
                logPath,
                "[BuildModule] Roslyn compilation created.");

            using var output =
                new MemoryStream();

            WriteLog(
                logPath,
                "[BuildModule] Emit started.");

            Microsoft.CodeAnalysis.Emit.EmitResult result =
                compilation.Emit(output);

            WriteLog(
                logPath,
                "[BuildModule] Emit completed.");

            var lines =
                new List<string>();

            lines.Add(
                $"Project: {projectDirectory}");

            lines.Add(
                $"Source files: {sourceFiles.Length}");

            lines.Add(
                $"References: {references.Length}");

            lines.Add("");

            foreach (Diagnostic diagnostic in
                     result.Diagnostics
                         .OrderBy(
                             d => d.Location.SourceTree?.FilePath)
                         .ThenBy(
                             d => d.Location
                                 .GetLineSpan()
                                 .StartLinePosition
                                 .Line))
            {
                if (diagnostic.Severity ==
                    DiagnosticSeverity.Hidden)
                {
                    continue;
                }

                lines.Add(
                    FormatDiagnostic(diagnostic));
            }

            lines.Add("");

            if (!result.Success)
{
    WriteLog(
        logPath,
        "[BuildModule] Compilation FAILED.");

    foreach (Diagnostic diagnostic in result.Diagnostics)
    {
        if (diagnostic.Severity == DiagnosticSeverity.Hidden)
            continue;

        WriteLog(
            logPath,
            "[BuildModule] Diagnostic: " +
            FormatDiagnostic(diagnostic));
    }

    return (
        1,
        string.Join(
            Environment.NewLine,
            lines) +
        Environment.NewLine);
}

            WriteLog(
                logPath,
                "[BuildModule] Compilation succeeded.");

            string outputDirectory =
                Path.Combine(
                    projectDirectory,
                    "bin",
                    "evi-build");

            WriteLog(
                logPath,
                $"[BuildModule] Creating output directory: {outputDirectory}");

            Directory.CreateDirectory(
                outputDirectory);

            string outputAssembly =
                Path.Combine(
                    outputDirectory,
                    $"{Path.GetFileName(projectDirectory)}.dll");

            WriteLog(
                logPath,
                $"[BuildModule] Writing assembly: {outputAssembly}");

            await File.WriteAllBytesAsync(
                outputAssembly,
                output.ToArray());

            WriteLog(
                logPath,
                "[BuildModule] Assembly written.");

            lines.Add("Build succeeded.");

            lines.Add(
                $"Assembly: {outputAssembly}");

            WriteLog(
                logPath,
                "[BuildModule] EXIT success.");

            return (
                0,
                string.Join(
                    Environment.NewLine,
                    lines) +
                Environment.NewLine);
        }
        catch (Exception ex)
        {
            WriteLog(
                logPath,
                $"[BuildModule] EXCEPTION: {ex.GetType().FullName}: {ex.Message}");

            WriteLog(
                logPath,
                $"[BuildModule] StackTrace: {ex.StackTrace}");

            return (
                1,
                $"Build exception: {ex.GetType().Name}: " +
                $"{ex.Message}\n");
        }
    }

    private static bool IsExcluded(
        string projectDirectory,
        string filePath)
    {
        string relative =
            Path.GetRelativePath(
                projectDirectory,
                filePath);

        string[] parts =
            relative.Split(
                Path.DirectorySeparatorChar,
                Path.AltDirectorySeparatorChar);

        return parts.Any(
            part =>
                part.Equals(
                    "Tools",
                    StringComparison.OrdinalIgnoreCase) ||
                part.Equals(
                    "translation-output",
                    StringComparison.OrdinalIgnoreCase));
    }

    private static MetadataReference[] CollectReferences(
        string? logPath)
    {
        var paths =
            new List<string>();

        AddReference(
            paths,
            typeof(object).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(Console).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(Enumerable).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(Task).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(List<>).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(Encoding).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(System.Diagnostics.Process).Assembly.Location,
            logPath);

        AddReference(
            paths,
            typeof(System.Runtime.InteropServices.Marshal).Assembly.Location,
            logPath);

        WriteLog(
            logPath,
            $"[BuildModule] Valid reference paths: {paths.Count}");

        var references =
            new List<MetadataReference>();

        foreach (string path in paths)
        {
            WriteLog(
                logPath,
                $"[BuildModule] CreateFromFile: {path}");

            references.Add(
                MetadataReference.CreateFromFile(path));
        }

        return references.ToArray();
    }

    private static void AddReference(
        List<string> paths,
        string? path,
        string? logPath)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            WriteLog(
                logPath,
                "[BuildModule] Reference has empty Location.");

            return;
        }

        WriteLog(
            logPath,
            $"[BuildModule] Reference candidate: {path}");

        if (!File.Exists(path))
        {
            WriteLog(
                logPath,
                "[BuildModule] Reference file does not exist.");

            return;
        }

        if (!paths.Contains(
                path,
                StringComparer.OrdinalIgnoreCase))
        {
            paths.Add(path);
        }
    }

    private static string FindBuildLogPath(
        string projectDirectory)
    {
        DirectoryInfo? current =
            new DirectoryInfo(projectDirectory);

        for (int i = 0; i < 8 && current != null; i++)
        {
            string logsDirectory =
                Path.Combine(
                    current.FullName,
                    "Logs");

            if (Directory.Exists(logsDirectory))
            {
                return Path.Combine(
                    logsDirectory,
                    "build-module.log");
            }

            current = current.Parent;
        }

        string fallbackDirectory =
            Path.Combine(
                projectDirectory,
                "build-logs");

        Directory.CreateDirectory(
            fallbackDirectory);

        return Path.Combine(
            fallbackDirectory,
            "build-module.log");
    }

    private static void WriteLog(
        string? logPath,
        string message)
    {
        if (string.IsNullOrWhiteSpace(logPath))
            return;

        try
        {
            File.AppendAllText(
                logPath,
                message +
                Environment.NewLine,
                Encoding.UTF8);
        }
        catch
        {
            // Diagnostic logging must never crash BuildModule.
        }
    }

    private static string FormatDiagnostic(
        Diagnostic diagnostic)
    {
        string location = "";

        if (diagnostic.Location.IsInSource)
        {
            FileLinePositionSpan span =
                diagnostic.Location.GetLineSpan();

            location =
                $"{span.Path}:" +
                $"{span.StartLinePosition.Line + 1}:" +
                $"{span.StartLinePosition.Character + 1}: ";
        }

        return
            $"{location}" +
            $"{diagnostic.Severity} " +
            $"{diagnostic.Id}: " +
            $"{diagnostic.GetMessage()}";
    }
}