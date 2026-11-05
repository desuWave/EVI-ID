using System;
using System.Diagnostics;
using System.IO;
using System.Threading.Tasks;

namespace EVI_ID.Shell;

public sealed class CommandResult
{
    public int ExitCode { get; init; }

    public string StandardOutput { get; init; } = "";

    public string StandardError { get; init; } = "";
}

public static class CommandRunner
{
    private static readonly TimeSpan CommandTimeout =
        TimeSpan.FromSeconds(30);

    private const int ErrorExitCode = -1;
    private const int TimeoutExitCode = -2;

    public static Task<CommandResult> RunAsync(
        string executable,
        params string[] arguments)
    {
        return RunAsync(
            executable,
            null,
            arguments);
    }

    public static async Task<CommandResult> RunAsync(
        string executable,
        string? workingDirectory,
        params string[] arguments)
    {
        if (string.IsNullOrWhiteSpace(executable))
        {
            const string message =
                "Executable name is empty.";

            Logger.Warning(message);

            return new CommandResult
            {
                ExitCode = ErrorExitCode,
                StandardError = message
            };
        }

        Logger.Info(
            $"Command started: {executable}");

        try
        {
            bool executableIsPath =
                Path.IsPathRooted(executable);

            if (executableIsPath &&
                !File.Exists(executable))
            {
                string message =
                    $"Command not found: {executable}";

                Logger.Warning(message);

                return new CommandResult
                {
                    ExitCode = ErrorExitCode,
                    StandardError = message
                };
            }

            if (!string.IsNullOrWhiteSpace(workingDirectory) &&
                !Directory.Exists(workingDirectory))
            {
                string message =
                    $"Working directory not found: " +
                    $"{workingDirectory}";

                Logger.Warning(message);

                return new CommandResult
                {
                    ExitCode = ErrorExitCode,
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

            if (!string.IsNullOrWhiteSpace(workingDirectory))
            {
                startInfo.WorkingDirectory =
                    workingDirectory;
            }

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
                    ExitCode = ErrorExitCode,
                    StandardError = message
                };
            }

            Task<string> stdoutTask =
                process.StandardOutput.ReadToEndAsync();

            Task<string> stderrTask =
                process.StandardError.ReadToEndAsync();

            Task exitTask =
                process.WaitForExitAsync();

            Task timeoutTask =
                Task.Delay(CommandTimeout);

            Task completedTask =
                await Task.WhenAny(
                    exitTask,
                    timeoutTask);

            bool timedOut =
                completedTask != exitTask;

            if (timedOut)
            {
                string timeoutMessage =
                    $"Command timed out after " +
                    $"{CommandTimeout.TotalSeconds:0} seconds.";

                Logger.Warning(timeoutMessage);

                try
                {
                    if (!process.HasExited)
                        process.Kill();
                }
                catch (Exception killException)
                {
                    Logger.Error(
                        "Failed to terminate timed-out command: " +
                        $"{killException.GetType().Name}: " +
                        $"{killException.Message}");
                }
            }

            await process.WaitForExitAsync();

            string stdout =
                await stdoutTask;

            string stderr =
                await stderrTask;

            if (timedOut)
            {
                string timeoutMessage =
                    $"Command timed out after " +
                    $"{CommandTimeout.TotalSeconds:0} seconds.";

                stderr =
                    string.IsNullOrWhiteSpace(stderr)
                        ? timeoutMessage
                        : stderr.TrimEnd() +
                          Environment.NewLine +
                          timeoutMessage;
            }

            var result = new CommandResult
            {
                ExitCode =
                    timedOut
                        ? TimeoutExitCode
                        : process.ExitCode,

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
                ExitCode = ErrorExitCode,
                StandardError = message
            };
        }
    }

    public static Task<CommandResult> RunIdentityCheckAsync()
    {
        return RunAsync("/system/bin/id");
    }
}