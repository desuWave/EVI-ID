using System;
using System.Collections.Generic;
using System.Linq;

namespace EVI_ID.Shell;

public sealed class RefactorCommandOptions
{
    public string Path { get; }

    public IReadOnlyList<string> AssemblyNames { get; }

    public RefactorCommandOptions(
        string path,
        IReadOnlyList<string> assemblyNames)
    {
        Path = path;
        AssemblyNames = assemblyNames;
    }

    public static RefactorCommandOptions Parse(
        string[] arguments)
    {
        ArgumentNullException.ThrowIfNull(arguments);

        if (arguments.Length == 0 ||
            string.IsNullOrWhiteSpace(arguments[0]))
        {
            throw new ArgumentException(
                "Usage: refactor <file-or-directory> " +
                "[--refs name.dll,name2.dll]");
        }

        string path = arguments[0];

        if (arguments.Length == 1)
        {
            return new RefactorCommandOptions(
                path,
                Array.Empty<string>());
        }

        if (arguments.Length != 3 ||
            arguments[1] != "--refs")
        {
            throw new ArgumentException(
                "Usage: refactor <file-or-directory> " +
                "[--refs name.dll,name2.dll]");
        }

        string[] names = arguments[2]
            .Split(',', StringSplitOptions.None)
            .Select(name => name.Trim())
            .ToArray();

        if (names.Any(name =>
                string.IsNullOrWhiteSpace(name) ||
                !name.EndsWith(
                    ".dll",
                    StringComparison.OrdinalIgnoreCase) ||
                name.Contains('/') ||
                name.Contains('\\') ||
                name == "." ||
                name == ".."))
        {
            throw new ArgumentException(
                "References must be DLL filenames, " +
                "not paths.");
        }

        if (names.Distinct(
                StringComparer.OrdinalIgnoreCase).Count()
            != names.Length)
        {
            throw new ArgumentException(
                "Duplicate DLL names are not allowed.");
        }

        return new RefactorCommandOptions(
            path,
            Array.AsReadOnly(names));
    }
}
