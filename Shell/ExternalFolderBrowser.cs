using System;
using System.Collections.Generic;
using Android.Content;
using Android.Provider;

namespace EVI_ID.Shell;

public sealed class ExternalFolderEntry
{
    public string Name { get; init; } = "";
    public string DocumentId { get; init; } = "";
    public string MimeType { get; init; } = "";
    public bool IsDirectory { get; init; }
}

public static class ExternalFolderBrowser
{
    public static string GetRootDocumentId(string treeUriString)
    {
        var treeUri = Android.Net.Uri.Parse(treeUriString);

        return DocumentsContract.GetTreeDocumentId(treeUri)
            ?? throw new InvalidOperationException(
                "Android did not return the selected folder ID.");
    }

    public static IReadOnlyList<ExternalFolderEntry> GetChildren(
        Context context,
        string treeUriString,
        string parentDocumentId)
    {
        var treeUri = Android.Net.Uri.Parse(treeUriString);

        var childrenUri =
            DocumentsContract.BuildChildDocumentsUriUsingTree(
                treeUri,
                parentDocumentId);

        string[] projection =
        {
            DocumentsContract.Document.ColumnDocumentId!,
            DocumentsContract.Document.ColumnDisplayName!,
            DocumentsContract.Document.ColumnMimeType!
        };

        var result = new List<ExternalFolderEntry>();

        using var cursor = context.ContentResolver!.Query(
            childrenUri,
            projection,
            null,
            null,
            null);

        if (cursor is null)
        {
            throw new InvalidOperationException(
                "Android returned no cursor for the selected folder.");
        }

        int idIndex = cursor.GetColumnIndexOrThrow(
            DocumentsContract.Document.ColumnDocumentId);

        int nameIndex = cursor.GetColumnIndexOrThrow(
            DocumentsContract.Document.ColumnDisplayName);

        int mimeIndex = cursor.GetColumnIndexOrThrow(
            DocumentsContract.Document.ColumnMimeType);

        while (cursor.MoveToNext())
        {
            string documentId = cursor.GetString(idIndex) ?? "";
            string name = cursor.GetString(nameIndex) ?? "(unnamed)";
            string mimeType = cursor.GetString(mimeIndex) ?? "";

            result.Add(new ExternalFolderEntry
            {
                Name = name,
                DocumentId = documentId,
                MimeType = mimeType,
                IsDirectory =
                    mimeType == DocumentsContract.Document.MimeTypeDir
            });
        }

        result.Sort((a, b) =>
        {
            if (a.IsDirectory != b.IsDirectory)
                return a.IsDirectory ? -1 : 1;

            return StringComparer.OrdinalIgnoreCase.Compare(
                a.Name,
                b.Name);
        });

        return result;
    }
}