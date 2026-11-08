using System;
using System.IO;
using System.Linq;
using System.Text;
using Android.Content;
using EVI_ID.Shell;
using Microsoft.CodeAnalysis;

namespace EVI_ID.Testing;

public static class SafReferenceDiagnostics
{
    public static string Run(Context context)
    {
        var output = new StringBuilder();

        output.AppendLine("SAF Reference Diagnostics");

        try
        {
            var folder = ExternalFolderManager
                .GetFolders(context)
                .FirstOrDefault(f =>
                    ExternalFolderBrowser.GetRootDocumentId(
                        f.UriString) == "primary:NTIRL_DATA" &&
                    ExternalFolderManager.HasPersistedPermission(
                        context,
                        f.UriString));

            if (folder is null)
            {
                output.AppendLine(
                    "NTIRL_DATA: NOT REGISTERED");

                return output.ToString();
            }

            output.AppendLine(
                "NTIRL_DATA: REGISTERED");

            string documentId =
                ExternalFolderBrowser.GetRootDocumentId(
                    folder.UriString);

            string[] directories =
            {
                "Projects",
                "EVI-ID",
                "EVI-ID-References",
                "net8.0"
            };

            foreach (string name in directories)
            {
                var entry = ExternalFolderBrowser
                    .GetChildren(
                        context,
                        folder.UriString,
                        documentId)
                    .FirstOrDefault(e =>
                        e.IsDirectory &&
                        e.Name.Equals(
                            name,
                            StringComparison.Ordinal));

                if (entry is null)
                {
                    output.AppendLine(
                        $"Directory missing: {name}");

                    return output.ToString();
                }

                documentId = entry.DocumentId;
            }

            var file = ExternalFolderBrowser
                .GetChildren(
                    context,
                    folder.UriString,
                    documentId)
                .FirstOrDefault(e =>
                    !e.IsDirectory &&
                    e.Name.Equals(
                        "System.Runtime.dll",
                        StringComparison.Ordinal));

            if (file is null)
            {
                output.AppendLine(
                    "System.Runtime.dll: NOT FOUND");

                return output.ToString();
            }

            var uri = SafFileSystem.BuildDocumentUri(
                folder.UriString,
                file.DocumentId);

            using var stream = context.ContentResolver!
                .OpenInputStream(uri)
                ?? throw new IOException(
                    "Cannot open reference assembly.");

            using var memory = new MemoryStream();

            stream.CopyTo(memory);

            byte[] bytes = memory.ToArray();

            output.AppendLine(
                $"DLL bytes: {bytes.Length}");

            var reference =
                MetadataReference.CreateFromImage(bytes);

            output.AppendLine(
                reference is null
                    ? "MetadataReference: NULL"
                    : "MetadataReference: SUCCESS");

            if (reference is not null)
            {
                output.AppendLine();
                output.Append(
                    SafCompilationProbe.Run(reference));

                output.AppendLine();
                output.Append(
                    SafSemanticBridgeProbe.Run(reference));
            }
        }
        catch (Exception ex)
        {
            output.AppendLine(
                $"Error: {ex.GetType().Name}");

            output.AppendLine(ex.Message);
        }

        return output.ToString();
    }
}
