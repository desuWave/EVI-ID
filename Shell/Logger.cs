using System;
using System.Collections.Generic;
using System.IO;

namespace EVI_ID.Shell;

public static class Logger
{
    private static readonly object Sync = new();
    private static readonly List<string> _entries = new();

    private static string? _logFilePath;
    private static bool _initialized;

    public static IReadOnlyList<string> Entries
    {
        get
        {
            lock (Sync)
            {
                return _entries.ToArray();
            }
        }
    }

    public static void Initialize(string directory)
    {
        lock (Sync)
        {
            if (_initialized)
                return;

            Directory.CreateDirectory(directory);

            _logFilePath = Path.Combine(
                directory,
                "evi-id.log");

            if (File.Exists(_logFilePath))
            {
                foreach (var line in File.ReadAllLines(_logFilePath))
                {
                    _entries.Add(line);
                }
            }

            _initialized = true;
        }
    }

    public static void Info(string message)
    {
        Write("INFO", message);
    }

    public static void Warning(string message)
    {
        Write("WARN", message);
    }

    public static void Error(string message)
    {
        Write("ERROR", message);
    }

    private static void Write(string level, string message)
    {
        lock (Sync)
        {
            var entry =
                $"[{DateTime.Now:yyyy-MM-dd HH:mm:ss}] " +
                $"[{level}] {message}";

            _entries.Add(entry);
            Console.WriteLine(entry);

            if (_initialized && _logFilePath != null)
            {
                try
                {
                    File.AppendAllText(
                        _logFilePath,
                        entry + Environment.NewLine);
                }
                catch (IOException ex)
                {
                    Console.WriteLine(
                        $"[LOGGER ERROR] {ex.Message}");
                }
                catch (UnauthorizedAccessException ex)
                {
                    Console.WriteLine(
                        $"[LOGGER ERROR] {ex.Message}");
                }
            }
        }
    }
}