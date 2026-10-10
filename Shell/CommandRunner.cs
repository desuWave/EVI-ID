using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EVI_ID.Shell;

public sealed class CommandResult
{
    public int ExitCode { get; init; }

    public string StandardOutput { get; init; } = "";

    public string StandardError { get; init; }

    = "";
}

public static class CommandRunner
{
    public static async Task<CommandResult> RunAsync(
        string executable,
        params string[] arguments)
    {
        Logger.Info($"Command started: {executable}");

        try
        {
            if (!File.Exists(executable))
            {
                string message =
                    $"Command not found: {executable}";

                Logger.Warning(message);

                return new CommandResult
                {
                    ExitCode = -1,
                    StandardError = message
                };
            }

            var startInfo = new ProcessStartInfo
            {
                FileName = executable,
                UseShellExecute = false,
                RedirectStandardOutput = true,
                RedirectStandardError = true,
                CreateNoWindow = true
            };

            foreach (string argument in arguments)
            {
                startInfo.ArgumentList.Add(argument);
            }

            using var process = new Process
            {
                StartInfo = startInfo
            };

            if (!process.Start())
            {
                const string message =
                    "Command failed to start.";

                Logger.Error(message);

                return new CommandResult
                {
                    ExitCode = -1,
                    StandardError = message
                };
            }

            Task<string> stdoutTask =
                process.StandardOutput.ReadToEndAsync();

            Task<string> stderrTask =
                process.StandardError.ReadToEndAsync();

            await process.WaitForExitAsync();

            string stdout = await stdoutTask;
            string stderr = await stderrTask;

            var result = new CommandResult
            {
                ExitCode = process.ExitCode,
                StandardOutput = stdout,
                StandardError = stderr
            };

            if (!string.IsNullOrWhiteSpace(stdout))
            {
                Logger.Info(
                    $"Command stdout: {stdout.Trim()}");
            }

            if (!string.IsNullOrWhiteSpace(stderr))
            {
                Logger.Warning(
                    $"Command stderr: {stderr.Trim()}");
            }

            Logger.Info(
                $"Command finished; exit code={result.ExitCode}");

            return result;
        }
        catch (Exception ex)
        {
            string message =
                $"{ex.GetType().Name}: {ex.Message}";

            Logger.Error(
                $"Command execution failed: {message}");

            return new CommandResult
            {
                ExitCode = -1,
                StandardError = message
            };
        }
    }

    public static Task<CommandResult> RunIdentityCheckAsync()
    {
        return RunAsync("/system/bin/id");
    }
}