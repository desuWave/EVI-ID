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
            var directory =
                SafReferenceDirectoryResolver.Resolve(context);

            output.AppendLine(
                "NTIRL_DATA: REGISTERED");

            string treeUriString = directory.TreeUriString;
            string documentId = directory.DocumentId;
            var file = ExternalFolderBrowser
                .GetChildren(
                    context,
                    treeUriString,
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

            var references =
                SafMetadataReferenceLoader.Load(
                    context,
                    treeUriString,
                    documentId,
                    new[] { "System.Runtime.dll" });

            var reference = references[0];

            output.AppendLine(
                $"MetadataReferences loaded: {references.Count}");
            if (reference is not null)
            {
                output.AppendLine();
                output.Append(
                    SafCompilationProbe.Run(reference));

                output.AppendLine();
                output.Append(
                    SafSemanticBridgeProbe.Run(reference));

                output.AppendLine();
                output.AppendLine("SAF Multi-Reference Loader Probe");

                var multiple = SafMetadataReferenceLoader.Load(
                    context,
                    treeUriString,
                    documentId,
                    new[] { "System.Runtime.dll", "System.Console.dll" });

                output.AppendLine(
                    $"Multiple references loaded: {multiple.Count}");

                try
                {
                    SafMetadataReferenceLoader.Load(
                        context,
                        treeUriString,
                        documentId,
                        new[] { "System.Runtime.dll", "EVI-ID-Missing-Test.dll" });

                    output.AppendLine("Missing DLL test: FAILED");
                }
                catch (FileNotFoundException)
                {
                    output.AppendLine("Missing DLL test: PASS");
                }
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
