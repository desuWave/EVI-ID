using System;
using System.Collections.Generic;
using System.Linq;
using EVI_Refactor;

namespace EVI_ID.Shell;

public sealed class RefactorAndroidSession
{
    public RefactorDiscoverySession Discovery { get; }

    public string Path { get; }

    public string FileSpace { get; }

    public IReadOnlyList<string> AssemblyNames { get; }

    public RefactorAndroidSession(
        RefactorDiscoverySession discovery,
        string path,
        string fileSpace,
        IEnumerable<string> assemblyNames)
    {
        Discovery = discovery
            ?? throw new ArgumentNullException(nameof(discovery));

        if (string.IsNullOrWhiteSpace(path))
            throw new ArgumentException(
                "Source path is required.", nameof(path));

        if (string.IsNullOrWhiteSpace(fileSpace))
            throw new ArgumentException(
                "File space is required.", nameof(fileSpace));

        ArgumentNullException.ThrowIfNull(assemblyNames);

        Path = path;
        FileSpace = fileSpace;
        AssemblyNames = Array.AsReadOnly(
            assemblyNames.ToArray());
    }

    public void ValidateFileSpace(string currentFileSpace)
    {
        if (!string.Equals(
            FileSpace,
            currentFileSpace,
            StringComparison.Ordinal))
        {
            throw new InvalidOperationException(
                "The file space changed after discovery. " +
                "Run discovery again.");
        }
    }
}
