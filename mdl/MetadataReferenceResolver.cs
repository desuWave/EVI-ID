using System;
using System.IO;
using Microsoft.CodeAnalysis;

namespace EVI_ID.Mdl;

public static class MetadataReferenceResolver
{
    public static PortableExecutableReference FromBytes(
        byte[] assemblyBytes,
        string? displayName = null)
    {
        ArgumentNullException.ThrowIfNull(assemblyBytes);

        if (assemblyBytes.Length == 0)
        {
            throw new ArgumentException(
                "Assembly image cannot be empty.",
                nameof(assemblyBytes));
        }

        return MetadataReference.CreateFromImage(
            assemblyBytes,
            filePath: displayName);
    }

    public static PortableExecutableReference FromFile(
        string path)
    {
        ArgumentException.ThrowIfNullOrWhiteSpace(path);

        if (!File.Exists(path))
        {
            throw new FileNotFoundException(
                "Metadata reference file was not found.",
                path);
        }

        return MetadataReference.CreateFromFile(path);
    }
}
