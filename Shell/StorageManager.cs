
using System;
using System.Collections.Generic;
using System.IO;

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

        return Directory.GetDirectories(
            ProjectsDirectory);
    }

    public static bool ProjectExists(string name)
    {
        EnsureInitialized();
        ValidateName(name);

        return Directory.Exists(
            Path.Combine(ProjectsDirectory, name));
    }

    public static string CreateProjectDirectory(string name)
    {
        EnsureInitialized();
        ValidateName(name);

        string path = Path.Combine(
            ProjectsDirectory,
            name);

        Directory.CreateDirectory(path);

        Logger.Info($"Project directory created: {name}");

        return path;
    }

    private static void EnsureInitialized()
    {
        if (!_initialized)
        {
            throw new InvalidOperationException(
                "StorageManager has not been initialized.");
        }
    }

    private static void ValidateName(string name)
    {
        if (string.IsNullOrWhiteSpace(name) ||
            name == "." ||
            name == ".." ||
            name.IndexOfAny(Path.GetInvalidFileNameChars()) >= 0 ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid project directory name.",
                nameof(name));
        }
    }
}
