using System;
using System.Collections.Generic;
using System.IO;
using EVI_ID.Shell;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunEVIFileSystemTests(
        List<UnitTestResult> results,
        Android.Content.Context androidContext,
        string testRoot)
    {
        EVIFileSystem CreateTestFileSystem(
            string testName)
        {
            string root = Path.Combine(
                testRoot,
                "EVIFileSystem",
                testName);

            return new EVIFileSystem(
                androidContext,
                root);
        }

        Run(
            results,
            "EVIFileSystem: initialization",
            () =>
            {
                string root = Path.Combine(
                    testRoot,
                    "EVIFileSystem",
                    "Initialization");

                var fs = CreateTestFileSystem(
                    "Initialization");

                AssertTrue(
                    Directory.Exists(root),
                    "Internal root was not created.");

                AssertEqual(
                    EVIFileSpace.Internal,
                    fs.Space);
            });

        Run(
            results,
            "EVIFileSystem: write and read",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "WriteRead");

                fs.WriteText(
                    "notes/test.txt",
                    "Hello EVI");

                AssertEqual(
                    "Hello EVI",
                    fs.ReadText("notes/test.txt"));

                AssertTrue(
                    fs.Exists("notes/test.txt"),
                    "Written file does not exist.");
            });

        Run(
            results,
            "EVIFileSystem: append",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "Append");

                fs.WriteText(
                    "log.txt",
                    "First");

                fs.AppendText(
                    "log.txt",
                    " Second");

                AssertEqual(
                    "First Second",
                    fs.ReadText("log.txt"));
            });

        Run(
            results,
            "EVIFileSystem: create directory",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "CreateDirectory");

                fs.CreateDirectory(
                    "alpha/beta");

                AssertTrue(
                    fs.Exists("alpha/beta"),
                    "Directory was not created.");
            });

        Run(
            results,
            "EVIFileSystem: create file",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "CreateFile");

                fs.CreateFile(
                    "nested/example.txt",
                    "Original");

                AssertEqual(
                    "Original",
                    fs.ReadText("nested/example.txt"));

                fs.CreateFile(
                    "nested/example.txt",
                    "Replacement");

                AssertEqual(
                    "Original",
                    fs.ReadText("nested/example.txt"));
            });

        Run(
            results,
            "EVIFileSystem: rename",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "Rename");

                fs.CreateFile(
                    "old.txt",
                    "Rename test");

                fs.Rename(
                    "old.txt",
                    "new.txt");

                AssertTrue(
                    !fs.Exists("old.txt"),
                    "Old file still exists.");

                AssertEqual(
                    "Rename test",
                    fs.ReadText("new.txt"));
            });

        Run(
            results,
            "EVIFileSystem: delete",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "Delete");

                fs.CreateDirectory("folder");

                fs.CreateFile(
                    "folder/test.txt",
                    "Delete test");

                fs.Delete("folder/test.txt");

                AssertTrue(
                    !fs.Exists("folder/test.txt"),
                    "File was not deleted.");

                fs.Delete("folder");

                AssertTrue(
                    !fs.Exists("folder"),
                    "Empty directory was not deleted.");
            });

        Run(
            results,
            "EVIFileSystem: missing path",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "MissingPath");

                AssertTrue(
                    !fs.Exists("missing.txt"),
                    "Missing file was reported as existing.");
            });

        Run(
            results,
            "EVIFileSystem: path traversal",
            () =>
            {
                var fs = CreateTestFileSystem(
                    "PathTraversal");

                bool rejected = false;

                try
                {
                    fs.WriteText(
                        "../outside.txt",
                        "Must not escape");
                }
                catch (IOException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Path traversal was not rejected.");
            });
    }
}
