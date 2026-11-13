using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Security.Cryptography;
using System.Text;

namespace EVI_Refactor;

public static class RefactorSourceIdentity
{
    public static string Compute(
        IReadOnlyList<(string Name, string Content)> sources)
    {
        ArgumentNullException.ThrowIfNull(sources);

        if (sources.Any(source =>
            string.IsNullOrWhiteSpace(source.Name) ||
            source.Content is null))
        {
            throw new ArgumentException("InvalidSource");
        }

        if (sources.Select(source => source.Name)
            .Distinct(StringComparer.Ordinal)
            .Count() != sources.Count)
        {
            throw new ArgumentException("DuplicateSourceName");
        }

        using var stream = new MemoryStream();

        foreach (var source in sources.OrderBy(
            source => source.Name,
            StringComparer.Ordinal))
        {
            WriteString(stream, source.Name);
            WriteString(stream, source.Content);
        }

        return Convert.ToHexString(
            SHA256.HashData(stream.ToArray()));
    }

    private static void WriteString(
        Stream stream,
        string value)
    {
        byte[] bytes = Encoding.UTF8.GetBytes(value);
        byte[] length = BitConverter.GetBytes(bytes.Length);

        if (!BitConverter.IsLittleEndian)
            Array.Reverse(length);

        stream.Write(length);
        stream.Write(bytes);
    }
}
