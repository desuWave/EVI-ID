using System;
using System.Collections.Generic;
using System.IO;
using EVI_ID.Shell;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunEVIFileSystemSourceTests(
        List<UnitTestResult> results,
        Android.Content.Context androidContext,
        string testRoot)
    {
        EVIFileSystem CreateFileSystem(string name)
        {
            return new EVIFileSystem(
                androidContext,
                Path.Combine(
                    testRoot,
                    "EVIFileSystemSources",
                    name));
        }

        Run(
            results,
            "EVIFileSystem: single CSharp source",
            () =>
            {
                var fs = CreateFileSystem("Single");

                fs.WriteText(
                    "Program.cs",
                    "class Program {}");

                var sources = fs.ReadCSharpSources(
                    "Program.cs",
                    out bool isDirectory);

                AssertTrue(
                    !isDirectory,
                    "File was classified as directory.");

                AssertEqual(1, sources.Count);

                AssertEqual(
                    "Program.cs",
                    sources[0].Name);

                AssertEqual(
                    "class Program {}",
                    sources[0].Content);
            });

        Run(
            results,
            "EVIFileSystem: non-CSharp file",
            () =>
            {
                var fs = CreateFileSystem("NonCSharp");

                fs.WriteText(
                    "notes.txt",
                    "Not CSharp");

                var sources = fs.ReadCSharpSources(
                    "notes.txt",
                    out bool isDirectory);

                AssertTrue(
                    !isDirectory,
                    "File was classified as directory.");

                AssertEqual(0, sources.Count);
            });

        Run(
            results,
            "EVIFileSystem: recursive CSharp sources",
            () =>
            {
                var fs = CreateFileSystem("Recursive");

                fs.WriteText(
                    "src/Alpha.cs",
                    "class Alpha {}");

                fs.WriteText(
                    "src/sub/Beta.cs",
                    "class Beta {}");

                fs.WriteText(
                    "src/sub/notes.txt",
                    "Ignored");

                var sources = fs.ReadCSharpSources(
                    "src",
                    out bool isDirectory);

                AssertTrue(
                    isDirectory,
                    "Directory was classified as file.");

                AssertEqual(2, sources.Count);

                bool alphaFound = false;
                bool betaFound = false;

                foreach (var source in sources)
                {
                    if (source.Name == "Alpha.cs" &&
                        source.Content == "class Alpha {}")
                    {
                        alphaFound = true;
                    }

                    if (source.Name.Replace('\\', '/') ==
                            "sub/Beta.cs" &&
                        source.Content == "class Beta {}")
                    {
                        betaFound = true;
                    }
                }

                AssertTrue(
                    alphaFound && betaFound,
                    "Expected CSharp sources were not found.");
            });

        Run(
            results,
            "EVIFileSystem: CSharp extension case",
            () =>
            {
                var fs = CreateFileSystem("ExtensionCase");

                fs.WriteText(
                    "Example.CS",
                    "class Example {}");

                var sources = fs.ReadCSharpSources(
                    "Example.CS",
                    out bool isDirectory);

                AssertTrue(
                    !isDirectory,
                    "File was classified as directory.");

                AssertEqual(1, sources.Count);

                AssertEqual(
                    "class Example {}",
                    sources[0].Content);
            });

        Run(
            results,
            "EVIFileSystem: missing source path",
            () =>
            {
                var fs = CreateFileSystem("Missing");

                bool rejected = false;

                try
                {
                    fs.ReadCSharpSources(
                        "missing.cs",
                        out bool isDirectory);
                }
                catch (FileNotFoundException)
                {
                    rejected = true;
                }

                AssertTrue(
                    rejected,
                    "Missing source path was not rejected.");
            });
    }
}
