using System;
using System.Collections.Generic;
using System.Text;

namespace EVI_ID.Shell;

public static class HelpSystem
{
    private sealed record CommandHelp(
        string Name,
        string Summary,
        string Usage,
        string Description,
        string Limitations,
        string[] Examples);

    private static readonly Dictionary<string, CommandHelp>
        Commands =
            new(StringComparer.OrdinalIgnoreCase)
            {
                ["pwd"] = new CommandHelp(
                    "pwd",
                    "Show the current working directory.",
                    "pwd",
                    "Prints the absolute path of the current shell directory.",
                    "No arguments are currently supported.",
                    new[]
                    {
                        "pwd"
                    }),

                ["cd"] = new CommandHelp(
                    "cd",
                    "Change the current working directory.",
                    "cd [path]",
                    "Changes the shell working directory.",
                    "The target directory must already exist. " +
                    "Relative and absolute paths are supported.",
                    new[]
                    {
                        "cd Projects",
                        "cd ..",
                        "cd /data/user/0/..."
                    }),

                ["ls"] = new CommandHelp(
                    "ls",
                    "List directory contents.",
                    "ls",
                    "Shows files and directories in the current directory.",
                    "The current builtin implementation does not yet " +
                    "support options, filters, sorting or recursive listing.",
                    new[]
                    {
                        "ls"
                    }),

                ["mkdir"] = new CommandHelp(
                    "mkdir",
                    "Create a directory.",
                    "mkdir <path>",
                    "Creates a directory at the specified path.",
                    "Relative and absolute paths are supported. " +
                    "Existing directories are not an error.",
                    new[]
                    {
                        "mkdir Test",
                        "mkdir Projects/NewProject"
                    }),

                ["touch"] = new CommandHelp(
                    "touch",
                    "Create an empty file.",
                    "touch <path>",
                    "Creates a file if it does not already exist.",
                    "The current implementation does not modify the " +
                    "contents of an existing file.",
                    new[]
                    {
                        "touch hello.txt"
                    }),

                ["cat"] = new CommandHelp(
                    "cat",
                    "Read a text file.",
                    "cat <path>",
                    "Reads the complete text contents of a file and " +
                    "prints them to the shell.",
                    "The current implementation is intended for text files.",
                    new[]
                    {
                        "cat hello.txt"
                    }),

                ["write"] = new CommandHelp(
                    "write",
                    "Write text to a file.",
                    "write <path> <text>",
                    "Writes the specified text to a file, replacing its " +
                    "previous contents.",
                    "The current command operates on text and does not " +
                    "provide shell redirection syntax.",
                    new[]
                    {
                        "write hello.txt Hello EVI-ID"
                    }),

                ["append"] = new CommandHelp(
                    "append",
                    "Append text to a file.",
                    "append <path> <text>",
                    "Adds the specified text to the end of a text file.",
                    "The current command operates on text.",
                    new[]
                    {
                        "append hello.txt More text"
                    }),

                ["rename"] = new CommandHelp(
                    "rename",
                    "Rename a file or directory.",
                    "rename <old-path> <new-path>",
                    "Changes the name or path of an existing file or directory.",
                    "The current command does not provide wildcard or " +
                    "batch rename operations.",
                    new[]
                    {
                        "rename hello.txt greeting.txt",
                        "rename Test TestRenamed"
                    }),

                ["rm"] = new CommandHelp(
                    "rm",
                    "Remove a file or empty directory.",
                    "rm <path>",
                    "Removes a file or an empty directory.",
                    "Directory removal is currently non-recursive. " +
                    "A directory containing files or subdirectories is " +
                    "not removed.",
                    new[]
                    {
                        "rm hello.txt",
                        "rm EmptyDirectory"
                    }),

                ["project"] = new CommandHelp(
                    "project",
                    "Manage EVI-ID projects.",
                    "project <operation> [arguments]",
                    "Provides higher-level operations for EVI-ID project " +
                    "directories.",
                    "Only operations explicitly implemented by the current " +
                    "EVI-ID version are supported.",
                    new[]
                    {
                        "project list",
                        "project create MyProject",
                        "project rename OldName NewName",
                        "project remove MyProject"
                    }),

["clear"] = new CommandHelp(
    "clear",
    "Clear the console.",
    "clear",
    "Clears all currently displayed console output.",
    "The command only clears the visible console text.",
    new[] { "clear" }),

["cls"] = new CommandHelp(
    "cls",
    "Clear the console.",
    "cls",
    "Clears all currently displayed console output.",
    "The command only clears the visible console text.",
    new[] { "cls" }),

                ["help"] = new CommandHelp(
                    "help",
                    "Show command help.",
                    "help [command]",
                    "Without an argument, displays the list of supported " +
                    "commands. With a command name, displays detailed " +
                    "documentation for that command.",
                    "Help describes only functionality implemented by the " +
                    "current EVI-ID Shell.",
                    new[]
                    {
                        "help",
                        "help cd",
                        "help write",
                        "help project"
                    })
            };

    public static string GetGeneralHelp()
    {
        var builder = new StringBuilder();

        builder.AppendLine("EVI-ID Shell");
        builder.AppendLine();
        builder.AppendLine("Supported commands:");
        builder.AppendLine();

        foreach (CommandHelp command in Commands.Values)
        {
            builder.Append("  ");
            builder.Append(command.Name.PadRight(10));
            builder.AppendLine(command.Summary);
        }

        builder.AppendLine();
        builder.AppendLine(
            "Use 'help <command>' for detailed command help.");

        return builder.ToString();
    }

    public static string GetCommandHelp(
        string commandName)
    {
        if (string.IsNullOrWhiteSpace(commandName))
            return GetGeneralHelp();

        if (!Commands.TryGetValue(
                commandName.Trim(),
                out CommandHelp? command))
        {
            return
                $"help: unknown command: {commandName}\n" +
                "Use 'help' to list supported commands.";
        }

        var builder = new StringBuilder();

        builder.AppendLine(
            $"EVI-ID Shell — {command.Name}");

        builder.AppendLine();
        builder.AppendLine("Usage:");
        builder.AppendLine(
            $"  {command.Usage}");

        builder.AppendLine();
        builder.AppendLine("Description:");
        builder.AppendLine(
            $"  {command.Description}");

        builder.AppendLine();
        builder.AppendLine("Current limitations:");
        builder.AppendLine(
            $"  {command.Limitations}");

        builder.AppendLine();
        builder.AppendLine("Examples:");

        foreach (string example in command.Examples)
        {
            builder.AppendLine(
                $"  {example}");
        }

        return builder.ToString();
    }
}