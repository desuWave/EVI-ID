using System;
using System.Collections.Generic;
using System.IO;
using EVI_ID.Shell;

namespace EVI_ID.Testing;

public sealed class UnitTestResult
{
    public string Name { get; }

    public bool Passed { get; }

    public string Message { get; }

    public UnitTestResult(
        string name,
        bool passed,
        string message)
    {
        Name = name;
        Passed = passed;
        Message = message;
    }
}

public static partial class UnitTestRunner
{
    private sealed class TestContext
    {
        public string RootDirectory { get; }

        public ShellSession Shell { get; }

        public TestContext(string rootDirectory)
        {
            RootDirectory = rootDirectory;

            Directory.CreateDirectory(
                RootDirectory);

            Shell = new ShellSession(
                RootDirectory);
        }
    }

    public static string RunAll(
        Android.Content.Context androidContext,
        bool verbose = false)
    {
        var results =
            new List<UnitTestResult>();

        string testRoot =
            Path.Combine(
                Path.GetTempPath(),
                "EVI-ID-utst-" +
                Guid.NewGuid().ToString("N"));

        try
        {
            var context =
                new TestContext(testRoot);

            Run(
                results,
                "ShellSession: initial directory",
                () =>
                {
                    AssertEqual(
                        testRoot,
                        context.Shell.CurrentDirectory);
                });

            Run(
                results,
                "ShellSession: mkdir",
                () =>
                {
                    bool success =
                        context.Shell.CreateDirectory(
                            "TestDir",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertTrue(
                        Directory.Exists(
                            Path.Combine(
                                testRoot,
                                "TestDir")),
                        "TestDir was not created.");
                });

            Run(
                results,
                "ShellSession: cd",
                () =>
                {
                    bool success =
                        context.Shell.ChangeDirectory(
                            "TestDir",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertEqual(
                        Path.GetFullPath(
                            Path.Combine(
                                testRoot,
                                "TestDir")),
                        context.Shell.CurrentDirectory);
                });

            Run(
                results,
                "ShellSession: touch",
                () =>
                {
                    bool success =
                        context.Shell.CreateFile(
                            "hello.txt",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertTrue(
                        File.Exists(
                            Path.Combine(
                                testRoot,
                                "TestDir",
                                "hello.txt")),
                        "hello.txt was not created.");
                });

            Run(
                results,
                "ShellSession: write",
                () =>
                {
                    bool success =
                        context.Shell.WriteFile(
                            "hello.txt",
                            "Hello EVI-ID",
                            out string message);

                    AssertTrue(
                        success,
                        message);
                });

            Run(
                results,
                "ShellSession: cat",
                () =>
                {
                    bool success =
                        context.Shell.ReadFile(
                            "hello.txt",
                            out string content,
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertEqual(
                        "Hello EVI-ID",
                        content);
                });

            Run(
                results,
                "ShellSession: append",
                () =>
                {
                    bool success =
                        context.Shell.AppendFile(
                            "hello.txt",
                            " Test",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    context.Shell.ReadFile(
                        "hello.txt",
                        out string content,
                        out message);

                    AssertEqual(
                        "Hello EVI-ID Test",
                        content);
                });

            Run(
                results,
                "ShellSession: rename",
                () =>
                {
                    bool success =
                        context.Shell.Rename(
                            "hello.txt",
                            "renamed.txt",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertTrue(
                        File.Exists(
                            Path.Combine(
                                context.Shell.CurrentDirectory,
                                "renamed.txt")),
                        "renamed.txt was not created.");
                });

            Run(
                results,
                "ShellSession: rm file",
                () =>
                {
                    bool success =
                        context.Shell.Remove(
                            "renamed.txt",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertTrue(
                        !File.Exists(
                            Path.Combine(
                                context.Shell.CurrentDirectory,
                                "renamed.txt")),
                        "File still exists after rm.");
                });

            Run(
                results,
                "ShellSession: cd parent",
                () =>
                {
                    bool success =
                        context.Shell.ChangeDirectory(
                            "..",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertEqual(
                        Path.GetFullPath(
                            testRoot),
                        context.Shell.CurrentDirectory);
                });

            Run(
                results,
                "ShellSession: rm empty directory",
                () =>
                {
                    bool success =
                        context.Shell.Remove(
                            "TestDir",
                            out string message);

                    AssertTrue(
                        success,
                        message);

                    AssertTrue(
                        !Directory.Exists(
                            Path.Combine(
                                testRoot,
                                "TestDir")),
                        "Directory still exists.");
                });

            Run(
                results,
                "HelpSystem: general help",
                () =>
                {
                    string help =
                        HelpSystem.GetGeneralHelp();

                    AssertTrue(
                        help.Contains("Supported commands:"),
                        "General help does not contain command list.");

                    AssertTrue(
                        help.Contains("pwd"),
                        "General help does not contain pwd.");
                });

            Run(
                results,
                "HelpSystem: command help",
                () =>
                {
                    string help =
                        HelpSystem.GetCommandHelp("cd");

                    AssertTrue(
                        help.Contains("Usage:"),
                        "Command help has no Usage section.");

                    AssertTrue(
                        help.Contains("cd [path]"),
                        "cd usage is missing.");
                });

            Run(
                results,
                "HelpSystem: unknown command",
                () =>
                {
                    string help =
                        HelpSystem.GetCommandHelp(
                            "definitely_unknown_command");

                    AssertTrue(
                        help.Contains("unknown command"),
                        "Unknown command was not reported.");
                });

            Run(
                results,
                "StorageManager: initialization",
                () =>
                {
                    AssertTrue(
                        !string.IsNullOrWhiteSpace(
                            StorageManager.RootDirectory),
                        "Storage root is empty.");

                    AssertTrue(
                        Directory.Exists(
                            StorageManager.RootDirectory),
                        "Storage root does not exist.");

                    AssertTrue(
                        Directory.Exists(
                            StorageManager.ProjectsDirectory),
                        "Projects directory does not exist.");

                    AssertTrue(
                        Directory.Exists(
                            StorageManager.LogsDirectory),
                        "Logs directory does not exist.");
                });

            Run(
                results,
                "StorageManager: project create",
                () =>
                {
                    string name =
                        "UTST_" +
                        Guid.NewGuid().ToString("N");

                    string path =
                        StorageManager.CreateProjectDirectory(
                            name);

                    AssertTrue(
                        Directory.Exists(path),
                        "Project directory was not created.");

                    StorageManager.RemoveProject(
                        name);
                });

            Run(
                results,
                "StorageManager: project lifecycle",
                () =>
                {
                    string name =
                        "UTST_Lifecycle_" +
                        Guid.NewGuid().ToString("N");

                    string renamed =
                        name + "_Renamed";

                    string path =
                        StorageManager.CreateProjectDirectory(
                            name);

                    string file =
                        Path.Combine(
                            path,
                            "test.txt");

                    File.WriteAllText(
                        file,
                        "unit test");

                    string newPath =
                        StorageManager.RenameProject(
                            name,
                            renamed);

                    AssertTrue(
                        Directory.Exists(newPath),
                        "Renamed project does not exist.");

                    AssertTrue(
                        File.Exists(
                            Path.Combine(
                                newPath,
                                "test.txt")),
                        "Project contents were not preserved.");

                    StorageManager.RemoveProject(
                        renamed);

                    AssertTrue(
                        !Directory.Exists(newPath),
                        "Project still exists after removal.");
                });

            RunEVIFileSystemTests(
                results,
                androidContext,
                testRoot);

            RunEVIFileSystemSourceTests(
                results,
                androidContext,
                testRoot);

            RunRefactorEngineTests(results);
            RunRefactorEngineReferenceTests(results);
            RunMetadataReferenceResolverTests(results);
            RunRefactorCommandOptionsTests(results);
            RunRefactorSafetyAnalyzerTests(results);
            RunRefactorSemanticBridgeTests(results);
            RunRefactorV7RegressionTests(results);
            RunRefactorV7SemanticRuntimeTests(results, androidContext);
            RunRefactorV7PreviewFormattingTests(results, androidContext);

            return FormatResults(
                results,
                verbose);
        }
        catch (Exception ex)
        {
            results.Add(
                new UnitTestResult(
                    "UNIT TEST RUNNER",
                    false,
                    ex.ToString()));

            return FormatResults(
                results,
                verbose);
        }
        finally
        {
            try
            {
                if (Directory.Exists(testRoot))
                {
                    Directory.Delete(
                        testRoot,
                        recursive: true);
                }
            }
            catch
            {
                // Test cleanup must not hide
                // the actual test result.
            }
        }
    }

    private static void Run(
        List<UnitTestResult> results,
        string name,
        Action test)
    {
        try
        {
            test();

            results.Add(
                new UnitTestResult(
                    name,
                    true,
                    ""));
        }
        catch (Exception ex)
        {
            results.Add(
                new UnitTestResult(
                    name,
                    false,
                    ex.ToString()));
        }
    }

    private static void AssertTrue(
        bool condition,
        string message)
    {
        if (!condition)
        {
            throw new InvalidOperationException(
                message);
        }
    }

    private static void AssertEqual<T>(
        T expected,
        T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(
                expected,
                actual))
        {
            throw new InvalidOperationException(
                $"Expected: {expected}; " +
                $"Actual: {actual}");
        }
    }

    private static string FormatResults(
        List<UnitTestResult> results,
        bool verbose)
    {
        int passed = 0;
        int failed = 0;

        var output =
            new System.Text.StringBuilder();

        output.AppendLine(
            "EVI-ID Unit Test");

        output.AppendLine(
            $"Tests: {results.Count}");

        output.AppendLine();

        int testNumber = 0;

        foreach (UnitTestResult result in results)
        {
            testNumber++;

            if (result.Passed)
            {
                passed++;

                output.AppendLine(
                    $"[{testNumber:D3}] [PASS] {result.Name}");
            }
            else
            {
                failed++;

                output.AppendLine(
                    $"[{testNumber:D3}] [FAIL] {result.Name}");

                if (!string.IsNullOrWhiteSpace(
                        result.Message))
                {
                    output.AppendLine(
                        $"       {result.Message}");
                }
            }
        }

        output.AppendLine();
        output.AppendLine(
            $"Passed: {passed}");

        output.AppendLine(
            $"Failed: {failed}");

        output.AppendLine();

        output.AppendLine(
            failed == 0
                ? "UNIT TEST: PASS"
                : "UNIT TEST: FAILED");

        return output.ToString();
    }
}
