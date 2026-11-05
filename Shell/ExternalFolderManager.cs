using System;
using System.Collections.Generic;
using System.Linq;
using System.Text;
using Android.Content;
using Android.Net;

namespace EVI_ID.Shell;

public sealed class ExternalFolder
{
    public string Name { get; init; } = "";
    public string UriString { get; init; } = "";
}

public static class ExternalFolderManager
{
    private const string PreferencesName = "EVI_ID_ExternalFolders";
    private const string Prefix = "folder:";

    public static IReadOnlyList<ExternalFolder> GetFolders(Context context)
    {
        var preferences = context.GetSharedPreferences(
            PreferencesName,
            FileCreationMode.Private)
            ?? throw new InvalidOperationException(
                "Cannot access folder preferences.");

        var result = new List<ExternalFolder>();

        foreach (var entry in preferences.All)
        {
            if (!entry.Key.StartsWith(Prefix, StringComparison.Ordinal))
                continue;

            try
            {
                string uriString = Encoding.UTF8.GetString(
                    Convert.FromBase64String(entry.Key[Prefix.Length..]));

                result.Add(new ExternalFolder
                {
                    Name = entry.Value?.ToString() ?? "Selected folder",
                    UriString = uriString
                });
            }
            catch (FormatException)
            {
                // Ignore invalid saved entries.
            }
        }

        return result
            .OrderBy(folder => folder.Name, StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static void AddFolder(
        Context context,
        Android.Net.Uri uri,
        string name)
    {
        var preferences = context.GetSharedPreferences(
            PreferencesName,
            FileCreationMode.Private)
            ?? throw new InvalidOperationException(
                "Cannot access folder preferences.");

        string uriString = uri.ToString();

        string key = Prefix + Convert.ToBase64String(
            Encoding.UTF8.GetBytes(uriString));

        preferences.Edit()!
            .PutString(key, name)
            .Apply();
    }

    public static void RemoveFolder(
        Context context,
        string uriString)
    {
        var preferences = context.GetSharedPreferences(
            PreferencesName,
            FileCreationMode.Private)
            ?? throw new InvalidOperationException(
                "Cannot access folder preferences.");

        string key = Prefix + Convert.ToBase64String(
            Encoding.UTF8.GetBytes(uriString));

        preferences.Edit()!
            .Remove(key)
            .Apply();
    }

    public static bool HasPersistedPermission(
        Context context,
        string uriString)
    {
        var permissions =
            context.ContentResolver?.PersistedUriPermissions;

        if (permissions is null)
            return false;

        return permissions.Any(permission =>
            permission.Uri?.ToString() == uriString &&
            permission.IsReadPermission &&
            permission.IsWritePermission);
    }
}