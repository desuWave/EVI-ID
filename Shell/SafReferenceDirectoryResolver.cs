using System;
using System.IO;
using System.Linq;
using Android.Content;

namespace EVI_ID.Shell;

public sealed class SafReferenceDirectory
{
    public string TreeUriString { get; }

    public string DocumentId { get; }

    public SafReferenceDirectory(
        string treeUriString,
        string documentId)
    {
        TreeUriString = treeUriString;
        DocumentId = documentId;
    }
}

public static class SafReferenceDirectoryResolver
{
    public static SafReferenceDirectory Resolve(
        Context context)
    {
        ArgumentNullException.ThrowIfNull(context);

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
            throw new DirectoryNotFoundException(
                "NTIRL_DATA is not registered or accessible.");
        }

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
            var matches = ExternalFolderBrowser
                .GetChildren(
                    context,
                    folder.UriString,
                    documentId)
                .Where(entry =>
                    entry.IsDirectory &&
                    string.Equals(
                        entry.Name,
                        name,
                        StringComparison.Ordinal))
                .ToArray();

            if (matches.Length != 1)
            {
                throw new DirectoryNotFoundException(
                    $"Expected exactly one directory '{name}', " +
                    $"but found {matches.Length}.");
            }

            documentId = matches[0].DocumentId;
        }

        return new SafReferenceDirectory(
            folder.UriString,
            documentId);
    }
}
