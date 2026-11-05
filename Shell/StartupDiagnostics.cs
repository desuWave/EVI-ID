using System;
using System.IO;
using System.Text;

namespace EVI_ID.Shell;

public static class StartupDiagnostics
{
    private static string _logPath = "";

    public static void Initialize(string appDataDirectory)
    {
        _logPath = Path.Combine(
            appDataDirectory,
            "EVI-ID-startup.log");

        Write("=== EVI-ID startup ===");
    }

    public static void Write(string message)
    {
        string line =
            DateTime.Now.ToString("yyyy-MM-dd HH:mm:ss.fff") +
            " " + message + Environment.NewLine;

        try
        {
            if (!string.IsNullOrEmpty(_logPath))
                File.AppendAllText(_logPath, line, Encoding.UTF8);
        }
        catch
        {
        }

        try
        {
            string directory =
                "/storage/emulated/0/EVI-ID-Diagnostics";

            Directory.CreateDirectory(directory);

            File.AppendAllText(
                Path.Combine(directory, "startup.log"),
                line,
                Encoding.UTF8);
        }
        catch
        {
        }
    }

    public static void WriteException(
        string stage,
        Exception exception)
    {
        Write(
            "ERROR at " + stage + Environment.NewLine +
            exception);
    }
}