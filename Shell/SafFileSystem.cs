using System;
using System.Collections.Generic;
using System.IO;
using System.Text;
using Android.Content;
using Android.Provider;

namespace EVI_ID.Shell;

public static class SafFileSystem
{
    public static Android.Net.Uri BuildDocumentUri(
        string treeUriString,
        string documentId)
    {
        var treeUri = Android.Net.Uri.Parse(treeUriString);

        return DocumentsContract.BuildDocumentUriUsingTree(
            treeUri,
            documentId);
    }

    public static byte[] ReadBytes(
        Context context,
        string treeUriString,
        string documentId)
    {
        ArgumentNullException.ThrowIfNull(context);
        ArgumentException.ThrowIfNullOrWhiteSpace(treeUriString);
        ArgumentException.ThrowIfNullOrWhiteSpace(documentId);

        var documentUri = BuildDocumentUri(
            treeUriString,
            documentId);

        using var stream =
            context.ContentResolver!.OpenInputStream(documentUri)
            ?? throw new IOException(
                $"Cannot open document for reading: {documentId}");

        using var memory = new MemoryStream();
        stream.CopyTo(memory);
        return memory.ToArray();
    }

    public static string ReadTextFile(
        Context context,
        string treeUriString,
        string documentId)
    {
        var documentUri = BuildDocumentUri(
            treeUriString,
            documentId);

        using var stream =
            context.ContentResolver!.OpenInputStream(documentUri)
            ?? throw new IOException(
                $"Cannot open document for reading: {documentId}");

        using var reader = new StreamReader(
            stream,
            Encoding.UTF8,
            detectEncodingFromByteOrderMarks: true);

        return reader.ReadToEnd();
    }

    public static void WriteTextFile(
        Context context,
        string treeUriString,
        string documentId,
        string content)
    {
        var documentUri = BuildDocumentUri(
            treeUriString,
            documentId);

        using var stream =
            context.ContentResolver!.OpenOutputStream(
                documentUri,
                "wt")
            ?? throw new IOException(
                $"Cannot open document for writing: {documentId}");

        using var writer = new StreamWriter(
            stream,
            new UTF8Encoding(false));

        writer.Write(content);
        writer.Flush();
    }

    public static string CreateFile(
        Context context,
        string treeUriString,
        string parentDocumentId,
        string fileName,
        string mimeType = "text/plain")
    {
        var parentUri = BuildDocumentUri(
            treeUriString,
            parentDocumentId);

        var createdUri =
            DocumentsContract.CreateDocument(
                context.ContentResolver!,
                parentUri,
                mimeType,
                fileName);

        if (createdUri is null)
        {
            throw new IOException(
                $"Cannot create file: {fileName}");
        }

        return createdUri.ToString()!;
    }

    public static string CreateDirectory(
        Context context,
        string treeUriString,
        string parentDocumentId,
        string directoryName)
    {
        var parentUri = BuildDocumentUri(
            treeUriString,
            parentDocumentId);

        var createdUri =
            DocumentsContract.CreateDocument(
                context.ContentResolver!,
                parentUri,
                DocumentsContract.Document.MimeTypeDir,
                directoryName);

        if (createdUri is null)
        {
            throw new IOException(
                $"Cannot create directory: {directoryName}");
        }

        return createdUri.ToString()!;
    }

    public static string Rename(
        Context context,
        string treeUriString,
        string documentId,
        string newName)
    {
        var documentUri = BuildDocumentUri(
            treeUriString,
            documentId);

        var renamedUri =
            DocumentsContract.RenameDocument(
                context.ContentResolver!,
                documentUri,
                newName);

        if (renamedUri is null)
        {
            throw new IOException(
                $"Cannot rename document: {documentId}");
        }

        return renamedUri.ToString()!;
    }

    public static void Delete(
        Context context,
        string treeUriString,
        string documentId)
    {
        var documentUri = BuildDocumentUri(
            treeUriString,
            documentId);

        if (!DocumentsContract.DeleteDocument(
                context.ContentResolver!,
                documentUri))
        {
            throw new IOException(
                $"Cannot delete document: {documentId}");
        }
    }

    public static IReadOnlyList<ExternalFolderEntry> ListDirectory(
        Context context,
        string treeUriString,
        string documentId)
    {
        return ExternalFolderBrowser.GetChildren(
            context,
            treeUriString,
            documentId);
    }
}
