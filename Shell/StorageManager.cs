using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;

namespace EVI_ID.Shell;

public static class StorageManager
{
    public static string RootDirectory { get; private set; } = "";

    public static string ProjectsDirectory { get; private set; } = "";

    public static string LogsDirectory { get; private set; } = "";

    private static bool _initialized;

    public static void Initialize(string appDataDirectory)
    {
        if (_initialized)
            return;

        RootDirectory = Path.Combine(
            appDataDirectory,
            "EVI-ID");

        ProjectsDirectory = Path.Combine(
            RootDirectory,
            "Projects");

        LogsDirectory = Path.Combine(
            RootDirectory,
            "Logs");

        Directory.CreateDirectory(RootDirectory);
        Directory.CreateDirectory(ProjectsDirectory);
        Directory.CreateDirectory(LogsDirectory);

        _initialized = true;

        Logger.Info("StorageManager initialized.");
        Logger.Info($"Storage root: {RootDirectory}");
        Logger.Info($"Projects directory: {ProjectsDirectory}");
        Logger.Info($"Logs directory: {LogsDirectory}");
    }

    public static IReadOnlyList<string> GetProjectDirectories()
    {
        EnsureInitialized();

        return Directory
            .GetDirectories(ProjectsDirectory)
            .OrderBy(
                path => Path.GetFileName(path),
                StringComparer.OrdinalIgnoreCase)
            .ToArray();
    }

    public static bool ProjectExists(string name)
    {
        EnsureInitialized();
        ValidateName(name);

        return Directory.Exists(
            GetProjectPath(name));
    }

    public static string CreateProjectDirectory(
        string name)
    {
        EnsureInitialized();
        ValidateName(name);

        string path =
            GetProjectPath(name);

        if (Directory.Exists(path))
        {
            throw new IOException(
                $"Project already exists: {name}");
        }

        Directory.CreateDirectory(path);

        Logger.Info(
            $"Project directory created: {name}");

        return path;
    }

    public static string RenameProject(
        string oldName,
        string newName)
    {
        EnsureInitialized();

        ValidateName(oldName);
        ValidateName(newName);

        string oldPath =
            GetProjectPath(oldName);

        string newPath =
            GetProjectPath(newName);

        if (!Directory.Exists(oldPath))
        {
            throw new DirectoryNotFoundException(
                $"Project not found: {oldName}");
        }

        if (Directory.Exists(newPath))
        {
            throw new IOException(
                $"Project already exists: {newName}");
        }

        Directory.Move(
            oldPath,
            newPath);

        Logger.Info(
            $"Project renamed: {oldName} -> {newName}");

        return newPath;
    }

    public static void RemoveProject(
        string name)
    {
        EnsureInitialized();
        ValidateName(name);

        string path =
            GetProjectPath(name);

        if (!Directory.Exists(path))
        {
            throw new DirectoryNotFoundException(
                $"Project not found: {name}");
        }

        // Project removal is intentionally recursive.
        Directory.Delete(
            path,
            recursive: true);

        Logger.Info(
            $"Project removed recursively: {name}");
    }

    private static string GetProjectPath(
        string name)
    {
        return Path.Combine(
            ProjectsDirectory,
            name);
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "StorageManager has not been initialized.");
        }
    }

    private static void ValidateName(
        string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name == "." ||
            name == ".." ||
            name.IndexOfAny(
                Path.GetInvalidFileNameChars()) >= 0 ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid project name.",
                nameof(name));
        }
    }
}