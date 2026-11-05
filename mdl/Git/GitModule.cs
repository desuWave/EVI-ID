using System;
using System.IO;
using System.IO.Compression;
using System.Net.Http;
using System.Threading.Tasks;

namespace EVI_ID.Mdl.Git;

public sealed class GitModule
{
    private readonly string _repoRoot;

    public GitModule(string repoRoot)
    {
        _repoRoot = repoRoot;
    }

    public Task<(int ExitCode, string Output)> VersionAsync()
    {
        Directory.CreateDirectory(_repoRoot);

        return Task.FromResult(
            (
                0,
                "EVI-ID Git Module 0.1\n"
            ));
    }

    public async Task<(int ExitCode, string Output)> CloneAsync(
        string repositoryUrl,
        string? directoryName = null)
    {
        if (string.IsNullOrWhiteSpace(repositoryUrl))
            throw new ArgumentException(
                "Repository URL is empty.",
                nameof(repositoryUrl));

        if (!Uri.TryCreate(
                repositoryUrl.Trim(),
                UriKind.Absolute,
                out var uri))
        {
            throw new ArgumentException(
                "Invalid repository URL.",
                nameof(repositoryUrl));
        }

        if (!string.Equals(
                uri.Host,
                "github.com",
                StringComparison.OrdinalIgnoreCase))
        {
            throw new NotSupportedException(
                "The first Git module implementation supports GitHub repositories only.");
        }

        string[] parts =
            uri.AbsolutePath
                .Trim('/')
                .Split(
                    '/',
                    StringSplitOptions.RemoveEmptyEntries);

        if (parts.Length < 2)
        {
            throw new ArgumentException(
                "GitHub repository URL must contain owner and repository name.",
                nameof(repositoryUrl));
        }

        string owner = parts[0];
        string repository = parts[1];

        if (repository.EndsWith(
                ".git",
                StringComparison.OrdinalIgnoreCase))
        {
            repository =
                repository[..^4];
        }

        string name =
            string.IsNullOrWhiteSpace(directoryName)
                ? repository
                : directoryName.Trim();

        ValidateDirectoryName(name);

        Directory.CreateDirectory(_repoRoot);

        string destination =
            Path.Combine(
                _repoRoot,
                name);

        if (Directory.Exists(destination) &&
            Directory.EnumerateFileSystemEntries(destination).GetEnumerator().MoveNext())
        {
            throw new IOException(
                $"Repository directory already exists and is not empty: {destination}");
        }

        if (Directory.Exists(destination))
            Directory.Delete(destination, true);

        string temporaryZip =
            Path.Combine(
                _repoRoot,
                $".{name}.download.zip");

        string temporaryExtract =
            Path.Combine(
                _repoRoot,
                $".{name}.extract");

        try
        {
            if (Directory.Exists(temporaryExtract))
                Directory.Delete(temporaryExtract, true);

            if (File.Exists(temporaryZip))
                File.Delete(temporaryZip);

            Directory.CreateDirectory(temporaryExtract);

            string archiveUrl =
                $"https://github.com/{owner}/{repository}/archive/refs/heads/main.zip";

            using var client = new HttpClient();

            client.DefaultRequestHeaders.UserAgent.ParseAdd(
                "EVI-ID-Git/0.1");

            byte[] archive =
                await client.GetByteArrayAsync(archiveUrl);

            await File.WriteAllBytesAsync(
                temporaryZip,
                archive);

            ZipFile.ExtractToDirectory(
                temporaryZip,
                temporaryExtract);

            string[] extractedDirectories =
                Directory.GetDirectories(
                    temporaryExtract);

            if (extractedDirectories.Length != 1)
            {
                throw new IOException(
                    "Unexpected GitHub archive structure.");
            }

            string source =
                extractedDirectories[0];

            Directory.Move(
                source,
                destination);

            return (
                0,
                $"Repository downloaded:\n" +
                $"  {repositoryUrl}\n" +
                $"Destination:\n" +
                $"  {destination}\n");
        }
        catch (HttpRequestException ex)
        {
            return (
                1,
                $"GitHub download failed:\n{ex.Message}\n");
        }
        catch (Exception ex)
        {
            return (
                1,
                $"{ex.GetType().Name}: {ex.Message}\n");
        }
        finally
        {
            try
            {
                if (File.Exists(temporaryZip))
                    File.Delete(temporaryZip);

                if (Directory.Exists(temporaryExtract))
                    Directory.Delete(
                        temporaryExtract,
                        true);
            }
            catch
            {
                // Cleanup failure must not hide the original result.
            }
        }
    }

    private static void ValidateDirectoryName(
        string name)
    {
        if (name == "." ||
            name == ".." ||
            name.Contains('/') ||
            name.Contains('\\'))
        {
            throw new ArgumentException(
                "Invalid repository directory name.");
        }
    }
}