using System;
using System.IO;

namespace EVI_Refactor;

public static class Program
{
    public static int Main(string[] args)
    {
        try
        {
            if (args.Length == 0)
            {
                PrintUsage();
                return 1;
            }

            string command = args[0];

            if (!command.Equals(
                    "analyze",
                    StringComparison.OrdinalIgnoreCase))
            {
                Console.WriteLine(
                    $"Unknown command: {command}");

                PrintUsage();

                return 1;
            }

            if (args.Length < 2)
            {
                Console.WriteLine(
                    "analyze: source path is required.");

                PrintUsage();

                return 1;
            }

            string sourcePath =
                Path.GetFullPath(args[1]);

            int maxFileLines =
                GetOption(
                    args,
                    "--max-file-lines",
                    RefactorEngine.DefaultMaxFileLines);

            int maxMethodLines =
                GetOption(
                    args,
                    "--max-method-lines",
                    RefactorEngine.DefaultMaxMethodLines);

            int maxDepth =
                GetOption(
                    args,
                    "--max-depth",
                    RefactorEngine.DefaultMaxDepth);

            RefactorEngine engine =
                new();

            return engine.Analyze(
                sourcePath,
                maxFileLines,
                maxMethodLines,
                maxDepth);
        }
        catch (Exception ex)
        {
            Console.Error.WriteLine(
                $"EVI-Refactor error: " +
                $"{ex.GetType().Name}: {ex.Message}");

            return 2;
        }
    }

    private static int GetOption(
        string[] args,
        string option,
        int defaultValue)
    {
        for (int i = 0; i < args.Length - 1; i++)
        {
            if (!args[i].Equals(
                    option,
                    StringComparison.OrdinalIgnoreCase))
            {
                continue;
            }

            if (int.TryParse(
                    args[i + 1],
                    out int value) &&
                value > 0)
            {
                return value;
            }

            throw new ArgumentException(
                $"Invalid value for {option}.");
        }

        return defaultValue;
    }

    private static void PrintUsage()
    {
        Console.WriteLine(
            "EVI-Refactor");

        Console.WriteLine();

        Console.WriteLine(
            "Usage:");

        Console.WriteLine(
            "  EVI-Refactor analyze <file-or-directory> " +
            "[options]");

        Console.WriteLine();

        Console.WriteLine(
            "Options:");

        Console.WriteLine(
            "  --max-file-lines N");

        Console.WriteLine(
            $"      Default: " +
            $"{RefactorEngine.DefaultMaxFileLines}");

        Console.WriteLine(
            "  --max-method-lines N");

        Console.WriteLine(
            $"      Default: " +
            $"{RefactorEngine.DefaultMaxMethodLines}");

        Console.WriteLine(
            "  --max-depth N");

        Console.WriteLine(
            $"      Default: " +
            $"{RefactorEngine.DefaultMaxDepth}");
    }
}