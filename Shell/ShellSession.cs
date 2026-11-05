using System;
using System.IO;
using System.Text;

namespace EVI_ID.Shell;

public sealed class ShellSession
{
    public string CurrentDirectory { get; private set; }

    public ShellSession(string initialDirectory)
    {
        if (string.IsNullOrWhiteSpace(initialDirectory))
            throw new ArgumentException(
                "Initial directory is empty.",
                nameof(initialDirectory));

        string fullPath =
            Path.GetFullPath(initialDirectory);

        if (!Directory.Exists(fullPath))
        {
            throw new DirectoryNotFoundException(
                $"Shell directory not found: {fullPath}");
        }

        CurrentDirectory = fullPath;
    }

    public string GetWorkingDirectory()
    {
        return CurrentDirectory;
    }

    public bool ChangeDirectory(
        string? path,
        out string message)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            message = CurrentDirectory;
            return true;
        }

        try
        {
            string targetPath = ResolvePath(path);

            if (!Directory.Exists(targetPath))
            {
                message =
                    $"cd: directory not found: {targetPath}";

                return false;
            }

            CurrentDirectory = targetPath;
            message = CurrentDirectory;

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"cd: invalid path: {ex.Message}";

            return false;
        }
    }

    public bool CreateDirectory(
        string path,
        out string message)
    {
        try
        {
            string targetPath = ResolvePath(path);

            Directory.CreateDirectory(targetPath);

            message =
                $"Directory created: {targetPath}";

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"mkdir: {ex.Message}";

            return false;
        }
    }

    public bool CreateFile(
        string path,
        out string message)
    {
        try
        {
            string targetPath = ResolvePath(path);

            // touch semantics:
            // do not destroy existing file contents.
            if (!File.Exists(targetPath))
            {
                using (File.Create(targetPath))
                {
                }

                message =
                    $"File created: {targetPath}";
            }
            else
            {
                message =
                    $"File already exists: {targetPath}";
            }

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"touch: {ex.Message}";

            return false;
        }
    }

    public bool ReadFile(
        string path,
        out string content,
        out string message)
    {
        try
        {
            string targetPath = ResolvePath(path);

            if (!File.Exists(targetPath))
            {
                content = "";

                message =
                    $"cat: file not found: {targetPath}";

                return false;
            }

            content = File.ReadAllText(
                targetPath,
                Encoding.UTF8);

            message = "";

            return true;
        }
        catch (Exception ex)
        {
            content = "";

            message =
                $"cat: {ex.Message}";

            return false;
        }
    }

    public bool WriteFile(
        string path,
        string content,
        out string message)
    {
        try
        {
            string targetPath = ResolvePath(path);

            File.WriteAllText(
                targetPath,
                content,
                Encoding.UTF8);

            message =
                $"File written: {targetPath}";

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"write: {ex.Message}";

            return false;
        }
    }

    public bool AppendFile(
        string path,
        string content,
        out string message)
    {
        try
        {
            string targetPath = ResolvePath(path);

            File.AppendAllText(
                targetPath,
                content,
                Encoding.UTF8);

            message =
                $"Text appended: {targetPath}";

            return true;
        }
        catch (Exception ex)
        {
            message =
                $"append: {ex.Message}";

            return false;
        }
    }

    public bool Rename(
        string sourcePath,
        string destinationPath,
        out string message)
    {
        try
        {
            string source =
                ResolvePath(sourcePath);

            string destination =
                ResolvePath(destinationPath);

            if (File.Exists(source))
            {
                if (File.Exists(destination) ||
                    Directory.Exists(destination))
                {
                    message =
                        $"rename: destination already exists: " +
                        $"{destination}";

                    return false;
                }

                File.Move(source, destination);

                message =
                    $"File renamed: {source} -> {destination}";

                return true;
            }

            if (Directory.Exists(source))
            {
                if (File.Exists(destination) ||
                    Directory.Exists(destination))
                {
                    message =
                        $"rename: destination already exists: " +
                        $"{destination}";

                    return false;
                }

                Directory.Move(source, destination);

                message =
                    $"Directory renamed: {source} -> {destination}";

                return true;
            }

            message =
                $"rename: source not found: {source}";

            return false;
        }
        catch (Exception ex)
        {
            message =
                $"rename: {ex.Message}";

            return false;
        }
    }

    public bool Remove(
        string path,
        out string message)
    {
        try
        {
            string targetPath =
                ResolvePath(path);

            if (File.Exists(targetPath))
            {
                File.Delete(targetPath);

                message =
                    $"File removed: {targetPath}";

                return true;
            }

            if (Directory.Exists(targetPath))
            {
                // Deliberately non-recursive.
                Directory.Delete(targetPath);

                message =
                    $"Directory removed: {targetPath}";

                return true;
            }

            message =
                $"rm: path not found: {targetPath}";

            return false;
        }
        catch (Exception ex)
        {
            message =
                $"rm: {ex.Message}";

            return false;
        }
    }

    private string ResolvePath(string path)
    {
        if (string.IsNullOrWhiteSpace(path))
        {
            throw new ArgumentException(
                "Path is empty.",
                nameof(path));
        }

        return Path.IsPathRooted(path)
            ? Path.GetFullPath(path)
            : Path.GetFullPath(
                Path.Combine(
                    CurrentDirectory,
                    path));
    }
}