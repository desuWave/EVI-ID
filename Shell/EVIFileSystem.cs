using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Text;

using Android.Content;

namespace EVI_ID.Shell;

public enum EVIFileSpace
{
    Internal,
    External
}

public sealed class EVIFileSystem
{
    private readonly Context _context;
    private readonly string _internalRoot;

    private string? _externalTreeUri;
    private string? _externalRootDocumentId;
    private string? _externalCurrentDocumentId;

    private readonly List<string> _externalDirectoryNames = new();

    public EVIFileSpace Space { get; private set; }

    public EVIFileSystem(
        Context context,
        string internalRoot)
    {
        _context = context
            ?? throw new ArgumentNullException(nameof(context));

        if (string.IsNullOrWhiteSpace(internalRoot))
        {
            throw new ArgumentException(
                "Internal root is empty.",
                nameof(internalRoot));
        }

        _internalRoot =
            Path.GetFullPath(internalRoot);

        Directory.CreateDirectory(_internalRoot);

        Space = EVIFileSpace.Internal;
    }

    public void ConfigureExternal(
        string treeUri,
        string documentId)
    {
        if (string.IsNullOrWhiteSpace(treeUri))
        {
            throw new ArgumentException(
                "External tree URI is empty.",
                nameof(treeUri));
        }

        if (string.IsNullOrWhiteSpace(documentId))
        {
            throw new ArgumentException(
                "External document ID is empty.",
                nameof(documentId));
        }

        _externalTreeUri = treeUri;
        _externalRootDocumentId = documentId;
        _externalCurrentDocumentId = documentId;

        _externalDirectoryNames.Clear();
    }

    public void UseInternal()
    {
        Space = EVIFileSpace.Internal;
    }

    public void UseExternal()
    {
        EnsureExternalConfigured();

        Space = EVIFileSpace.External;
    }

    public string GetCurrentRoot()
    {
        if (Space == EVIFileSpace.Internal)
            return _internalRoot;

        EnsureExternalConfigured();

        return _externalCurrentDocumentId!;
    }

    public string GetExternalRootDocumentId()
    {
        EnsureExternalConfigured();

        return _externalRootDocumentId!;
    }

    public string GetExternalCurrentDocumentId()
    {
        EnsureExternalConfigured();

        return _externalCurrentDocumentId!;
    }

    public string GetExternalCurrentPath()
    {
        EnsureExternalConfigured();

        if (_externalDirectoryNames.Count == 0)
            return "/";

        return "/" +
            string.Join(
                "/",
                _externalDirectoryNames);
    }

    public bool IsExternalConfigured()
    {
        return
            !string.IsNullOrWhiteSpace(_externalTreeUri) &&
            !string.IsNullOrWhiteSpace(_externalRootDocumentId) &&
            !string.IsNullOrWhiteSpace(_externalCurrentDocumentId);
    }

    public IReadOnlyList<ExternalFolderEntry> ListExternal(
        string documentId)
    {
        EnsureExternalConfigured();

        return SafFileSystem.ListDirectory(
            _context,
            _externalTreeUri!,
            documentId);
    }

    public IReadOnlyList<ExternalFolderEntry> ListCurrentDirectory()
    {
        EnsureExternalConfigured();

        return SafFileSystem.ListDirectory(
            _context,
            _externalTreeUri!,
            _externalCurrentDocumentId!);
    }

    public bool ChangeExternalDirectory(
        string path,
        out string message)
    {
        EnsureExternalConfigured();

        if (string.IsNullOrWhiteSpace(path))
        {
            message =
                GetExternalCurrentPath();

            return true;
        }

        string normalized =
            path.Replace('\\', '/').Trim();

        if (normalized == ".")
        {
            message =
                GetExternalCurrentPath();

            return true;
        }

        if (normalized == "/")
        {
            MoveExternalToRoot();

            message =
                GetExternalCurrentPath();

            return true;
        }

        string[] parts =
            normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        string currentId;
        var names = new List<string>();

        if (normalized.StartsWith(
                "/",
                StringComparison.Ordinal))
        {
            currentId =
                _externalRootDocumentId!;
        }
        else
        {
            currentId =
                _externalCurrentDocumentId!;

            names.AddRange(
                _externalDirectoryNames);
        }

        try
        {
            foreach (string part in parts)
            {
                if (part == ".")
                    continue;

                if (part == "..")
                {
                    if (names.Count == 0)
                    {
                        currentId =
                            _externalRootDocumentId!;
                    }
                    else
                    {
                        names.RemoveAt(
                            names.Count - 1);

                        currentId =
                            ResolveExternalDirectoryFromNames(
                                names);
                    }

                    continue;
                }

                var entries =
                    SafFileSystem.ListDirectory(
                        _context,
                        _externalTreeUri!,
                        currentId);

                var entry =
                    entries.FirstOrDefault(
                        x =>
                            x.IsDirectory &&
                            string.Equals(
                                x.Name,
                                part,
                                StringComparison.OrdinalIgnoreCase));

                if (entry is null)
                {
                    message =
                        $"cd: directory not found: {part}";

                    return false;
                }

                currentId =
                    entry.DocumentId;

                names.Add(entry.Name);
            }

            _externalCurrentDocumentId =
                currentId;

            _externalDirectoryNames.Clear();

            _externalDirectoryNames.AddRange(
                names);

            message =
                GetExternalCurrentPath();

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"cd: {ex.Message}";

            return false;
        }
    }

    public void MoveExternalToRoot()
    {
        EnsureExternalConfigured();

        _externalCurrentDocumentId =
            _externalRootDocumentId!;

        _externalDirectoryNames.Clear();
    }

    public string ReadText(string path)
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            return File.ReadAllText(
                fullPath,
                Encoding.UTF8);
        }

        return ReadExternalText(path);
    }

    public void WriteText(
        string path,
        string content)
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            string? directory =
                Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            File.WriteAllText(
                fullPath,
                content,
                new UTF8Encoding(false));

            return;
        }

        WriteExternalText(
            path,
            content);
    }

    public void AppendText(
        string path,
        string content)
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            string? directory =
                Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            File.AppendAllText(
                fullPath,
                content,
                new UTF8Encoding(false));

            return;
        }

        string existing =
            ReadExternalText(path);

        WriteExternalText(
            path,
            existing + content);
    }

    public void CreateDirectory(
        string path)
    {
        if (Space == EVIFileSpace.Internal)
        {
            Directory.CreateDirectory(
                ResolveInternalPath(path));

            return;
        }

        CreateExternalDirectory(path);
    }

    public void CreateFile(
        string path,
        string content = "")
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            string? directory =
                Path.GetDirectoryName(fullPath);

            if (!string.IsNullOrWhiteSpace(directory))
            {
                Directory.CreateDirectory(
                    directory);
            }

            if (File.Exists(fullPath))
            {
                return;
            }

            File.WriteAllText(
                fullPath,
                content,
                new UTF8Encoding(false));

            return;
        }

        CreateExternalFile(
            path,
            content);
    }

    public void Rename(
        string path,
        string newName)
    {
        if (string.IsNullOrWhiteSpace(newName))
        {
            throw new ArgumentException(
                "New name is empty.",
                nameof(newName));
        }

        if (Space == EVIFileSpace.Internal)
        {
            string source =
                ResolveInternalPath(path);

            string? directory =
                Path.GetDirectoryName(source);

            if (string.IsNullOrWhiteSpace(directory))
            {
                throw new IOException(
                    "Cannot determine source directory.");
            }

            string target =
                Path.Combine(
                    directory,
                    newName);

            if (File.Exists(target) ||
                Directory.Exists(target))
            {
                throw new IOException(
                    $"Destination already exists: {target}");
            }

            File.Move(
                source,
                target);

            return;
        }

        RenameExternal(
            path,
            newName);
    }

    public void Delete(string path)
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            if (Directory.Exists(fullPath))
            {
                Directory.Delete(
                    fullPath,
                    recursive: false);

                return;
            }

            if (File.Exists(fullPath))
            {
                File.Delete(fullPath);
                return;
            }

            throw new FileNotFoundException(
                "Path does not exist.",
                fullPath);
        }

        DeleteExternal(path);
    }

    public bool Exists(string path)
    {
        if (Space == EVIFileSpace.Internal)
        {
            string fullPath =
                ResolveInternalPath(path);

            return
                File.Exists(fullPath) ||
                Directory.Exists(fullPath);
        }

        return FindExternalEntry(path) is not null;
    }

public IReadOnlyList<(string Name, string Content)> ReadCSharpSources(
    string path,
    out bool isDirectory)
{
    var sources =
        new List<(string Name, string Content)>();

    isDirectory = false;

    string target = path ?? "";

    if (Space == EVIFileSpace.Internal)
    {
        string fullPath =
            ResolveInternalPath(target);

        if (File.Exists(fullPath))
        {
            if (IsCSharpSource(fullPath))
            {
                sources.Add((
                    Path.GetRelativePath(
                        _internalRoot,
                        fullPath),
                    File.ReadAllText(
                        fullPath,
                        Encoding.UTF8)));
            }

            return sources;
        }

        if (!Directory.Exists(fullPath))
        {
            throw new FileNotFoundException(
                "Source path was not found.",
                target);
        }

        isDirectory = true;

        foreach (string file in
            Directory
                .EnumerateFiles(
                    fullPath,
                    "*",
                    SearchOption.AllDirectories)
                .Where(IsCSharpSource)
                .OrderBy(
                    x => x,
                    StringComparer.OrdinalIgnoreCase))
        {
            sources.Add((
                Path.GetRelativePath(
                    fullPath,
                    file),
                File.ReadAllText(
                    file,
                    Encoding.UTF8)));
        }

        return sources;
    }

    EnsureExternalConfigured();

    ExternalFolderEntry? entry =
        FindExternalEntry(target);

    if (entry is not null && !entry.IsDirectory)
    {
        if (IsCSharpSource(entry.Name))
        {
            sources.Add((
                GetExternalDisplayPath(target),
                SafFileSystem.ReadTextFile(
                    _context,
                    _externalTreeUri!,
                    entry.DocumentId)));
        }

        return sources;
    }

    string directoryId;

    string displayRoot;

    if (entry is not null)
    {
        directoryId = entry.DocumentId;
        displayRoot = GetExternalDisplayPath(target);
    }
    else
    {
        directoryId =
            ResolveExternalDirectory(target);

        displayRoot =
            GetExternalDisplayPath(target);
    }

    isDirectory = true;

    CollectExternalCSharpSources(
        directoryId,
        displayRoot,
        sources);

    return sources;
}

private static bool IsCSharpSource(string path)
{
    return path.EndsWith(
        ".cs",
        StringComparison.OrdinalIgnoreCase);
}

private string GetExternalDisplayPath(string path)
{
    if (string.IsNullOrWhiteSpace(path) ||
        path == ".")
    {
        return GetExternalCurrentPath();
    }

    string normalized =
        path.Replace('\\', '/').Trim();

    if (normalized.StartsWith(
        "/",
        StringComparison.Ordinal))
    {
        return normalized;
    }

    string current =
        GetExternalCurrentPath();

    if (current == "/")
        return "/" + normalized.Trim('/');

    return current.TrimEnd('/') +
        "/" +
        normalized.Trim('/');
}

private void CollectExternalCSharpSources(
    string documentId,
    string displayPath,
    List<(string Name, string Content)> sources)
{
    foreach (ExternalFolderEntry entry in
        ListExternal(documentId))
    {
        string childPath =
            displayPath.TrimEnd('/') +
            "/" +
            entry.Name;

        if (entry.IsDirectory)
        {
            CollectExternalCSharpSources(
                entry.DocumentId,
                childPath,
                sources);

            continue;
        }

        if (!IsCSharpSource(entry.Name))
            continue;

        string content =
            SafFileSystem.ReadTextFile(
                _context,
                _externalTreeUri!,
                entry.DocumentId);

        sources.Add((
            childPath,
            content));
    }
}

    private string ResolveInternalPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return _internalRoot;

        string normalized =
            path.Replace(
                '/',
                Path.DirectorySeparatorChar);

        normalized =
            normalized.TrimStart(
                Path.DirectorySeparatorChar);

        string fullPath =
            Path.GetFullPath(
                Path.Combine(
                    _internalRoot,
                    normalized));

        string root =
            Path.GetFullPath(_internalRoot)
                .TrimEnd(
                    Path.DirectorySeparatorChar);

        if (!fullPath.Equals(
                root,
                StringComparison.Ordinal) &&
            !fullPath.StartsWith(
                root + Path.DirectorySeparatorChar,
                StringComparison.Ordinal))
        {
            throw new IOException(
                "Path escapes EVI-ID internal root.");
        }

        return fullPath;
    }

    private void EnsureExternalConfigured()
    {
        if (!IsExternalConfigured())
        {
            throw new InvalidOperationException(
                "External SAF space is not configured.");
        }
    }

    private string ReadExternalText(
        string path)
    {
        var entry =
            FindExternalEntry(path)
            ?? throw new FileNotFoundException(
                "External file was not found.",
                path);

        if (entry.IsDirectory)
        {
            throw new IOException(
                $"Path is a directory: {path}");
        }

        return SafFileSystem.ReadTextFile(
            _context,
            _externalTreeUri!,
            entry.DocumentId);
    }

    private void WriteExternalText(
        string path,
        string content)
    {
        var entry =
            FindExternalEntry(path)
            ?? throw new FileNotFoundException(
                "External file was not found.",
                path);

        if (entry.IsDirectory)
        {
            throw new IOException(
                $"Path is a directory: {path}");
        }

        SafFileSystem.WriteTextFile(
            _context,
            _externalTreeUri!,
            entry.DocumentId,
            content);
    }

    private void CreateExternalFile(
        string path,
        string content)
    {
        string parentPath =
            GetParentPath(path);

        string fileName =
            GetName(path);

        string parentId =
            ResolveExternalDirectory(
                parentPath);

        if (string.IsNullOrWhiteSpace(fileName))
        {
            throw new IOException(
                "File name is empty.");
        }

        if (FindExternalEntry(path) is not null)
        {
            return;
        }

        string uri =
            SafFileSystem.CreateFile(
                _context,
                _externalTreeUri!,
                parentId,
                fileName,
                "text/plain");

        string documentId =
            DocumentsContractHelper
                .GetDocumentId(uri);

        if (!string.IsNullOrEmpty(content))
        {
            SafFileSystem.WriteTextFile(
                _context,
                _externalTreeUri!,
                documentId,
                content);
        }
    }

    private void CreateExternalDirectory(
        string path)
    {
        string parentPath =
            GetParentPath(path);

        string directoryName =
            GetName(path);

        if (string.IsNullOrWhiteSpace(directoryName))
        {
            throw new IOException(
                "Directory name is empty.");
        }

        if (FindExternalEntry(path) is not null)
        {
            return;
        }

        string parentId =
            ResolveExternalDirectory(
                parentPath);

        SafFileSystem.CreateDirectory(
            _context,
            _externalTreeUri!,
            parentId,
            directoryName);
    }

    private void RenameExternal(
        string path,
        string newName)
    {
        var entry =
            FindExternalEntry(path)
            ?? throw new FileNotFoundException(
                "External path was not found.",
                path);

        SafFileSystem.Rename(
            _context,
            _externalTreeUri!,
            entry.DocumentId,
            newName);
    }

    private void DeleteExternal(
        string path)
    {
        var entry =
            FindExternalEntry(path)
            ?? throw new FileNotFoundException(
                "External path was not found.",
                path);

        SafFileSystem.Delete(
            _context,
            _externalTreeUri!,
            entry.DocumentId);
    }

    private string ResolveExternalDirectory(
        string path)
    {
        EnsureExternalConfigured();

        if (string.IsNullOrWhiteSpace(path) ||
            path == ".")
        {
            return _externalCurrentDocumentId!;
        }

        string normalized =
            path.Replace('\\', '/')
                .Trim();

        if (normalized == "/")
        {
            return _externalRootDocumentId!;
        }

        bool absolute =
            normalized.StartsWith(
                "/",
                StringComparison.Ordinal);

        string[] parts =
            normalized.Split(
                '/',
                StringSplitOptions.RemoveEmptyEntries);

        string currentId =
            absolute
                ? _externalRootDocumentId!
                : _externalCurrentDocumentId!;

        foreach (string part in parts)
        {
            if (part == ".")
                continue;

            if (part == "..")
            {
                throw new IOException(
                    "Parent traversal must use cd.");
            }

            var entries =
                SafFileSystem.ListDirectory(
                    _context,
                    _externalTreeUri!,
                    currentId);

            var entry =
                entries.FirstOrDefault(
                    x =>
                        x.IsDirectory &&
                        string.Equals(
                            x.Name,
                            part,
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new DirectoryNotFoundException(
                    $"External directory not found: {part}");
            }

            currentId =
                entry.DocumentId;
        }

        return currentId;
    }

    private string ResolveExternalDirectoryFromNames(
        IReadOnlyList<string> names)
    {
        string currentId =
            _externalRootDocumentId!;

        foreach (string name in names)
        {
            var entries =
                SafFileSystem.ListDirectory(
                    _context,
                    _externalTreeUri!,
                    currentId);

            var entry =
                entries.FirstOrDefault(
                    x =>
                        x.IsDirectory &&
                        string.Equals(
                            x.Name,
                            name,
                            StringComparison.OrdinalIgnoreCase));

            if (entry is null)
            {
                throw new DirectoryNotFoundException(
                    $"External directory not found: {name}");
            }

            currentId =
                entry.DocumentId;
        }

        return currentId;
    }

    private ExternalFolderEntry? FindExternalEntry(
        string path)
    {
        EnsureExternalConfigured();

        string parentPath =
            GetParentPath(path);

        string name =
            GetName(path);

        if (string.IsNullOrWhiteSpace(name))
            return null;

        string parentId =
            ResolveExternalDirectory(
                parentPath);

        var entries =
            SafFileSystem.ListDirectory(
                _context,
                _externalTreeUri!,
                parentId);

        return entries.FirstOrDefault(
            x =>
                string.Equals(
                    x.Name,
                    name,
                    StringComparison.OrdinalIgnoreCase));
    }

    private static string GetParentPath(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return ".";

        string normalized =
            path.Replace('\\', '/')
                .Trim();

        int index =
            normalized.LastIndexOf('/');

        if (index < 0)
            return ".";

        if (index == 0)
            return "/";

        return normalized[..index];
    }

    private static string GetName(
        string path)
    {
        if (string.IsNullOrWhiteSpace(path))
            return "";

        string normalized =
            path.Replace('\\', '/')
                .TrimEnd('/');

        int index =
            normalized.LastIndexOf('/');

        return index < 0
            ? normalized
            : normalized[(index + 1)..];
    }
}