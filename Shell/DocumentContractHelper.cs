using Android.Provider;

namespace EVI_ID.Shell;

public static class DocumentsContractHelper
{
    public static string GetDocumentId(
        string documentUri)
    {
        Android.Net.Uri uri =
            Android.Net.Uri.Parse(documentUri)
            ?? throw new System.ArgumentException(
                "Invalid document URI.",
                nameof(documentUri));

        return DocumentsContract.GetDocumentId(uri)
            ?? throw new System.InvalidOperationException(
                "Android did not return a document ID.");
    }
}