using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using Android.Content;
using EVI_ID.Mdl;
using Microsoft.CodeAnalysis;

namespace EVI_ID.Shell;

public static class SafMetadataReferenceLoader
{
    public static IReadOnlyList<PortableExecutableReference> Load(
        Context context,
        string treeUriString,
        string directoryDocumentId,
        IEnumerable<string> assemblyNames)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(treeUriString);
        ArgumentException.ThrowIfNullOrWhiteSpace(directoryDocumentId);
        ArgumentNullException.ThrowIfNull(assemblyNames);

        string[] names = assemblyNames.ToArray();

        if (names.Length == 0)
        {
            throw new ArgumentException(
                "At least one assembly name is required.",
                nameof(assemblyNames));
        }

        if (names.Any(name =>
                string.IsNullOrWhiteSpace(name) ||
                !name.EndsWith(
                    ".dll",
                    StringComparison.OrdinalIgnoreCase)))
        {
            throw new ArgumentException(
                "Every assembly name must be a non-empty DLL filename.",
                nameof(assemblyNames));
        }

        if (names.Distinct(StringComparer.OrdinalIgnoreCase).Count()
            != names.Length)
        {
            throw new ArgumentException(
                "Duplicate assembly names are not allowed.",
                nameof(assemblyNames));
        }

        var entries = ExternalFolderBrowser.GetChildren(
            context,
            treeUriString,
            directoryDocumentId);

        var references =
            new List<PortableExecutableReference>(names.Length);

        foreach (string name in names)
        {
            var matches = entries
                .Where(entry =>
                    !entry.IsDirectory &&
                    string.Equals(
                        entry.Name,
                        name,
                        StringComparison.OrdinalIgnoreCase))
                .ToArray();

            if (matches.Length != 1)
            {
                throw new FileNotFoundException(
                    $"Expected exactly one DLL named '{name}', " +
                    $"but found {matches.Length}.",
                    name);
            }

            byte[] bytes = SafFileSystem.ReadBytes(
                context,
                treeUriString,
                matches[0].DocumentId);

            references.Add(
                EVI_ID.Mdl.MetadataReferenceResolver.FromBytes(
                    bytes,
                    name));
        }

        return references.AsReadOnly();
    }
}
