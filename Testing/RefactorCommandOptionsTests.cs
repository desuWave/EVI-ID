using System;
using System.Collections.Generic;
using EVI_ID.Shell;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorCommandOptionsTests(
        List<UnitTestResult> results)
    {
        Run(results, "Refactor options: legacy command", () =>
        {
            var options = RefactorCommandOptions.Parse(
                new[] { "Example.cs" });

            AssertEqual("Example.cs", options.Path);
            AssertEqual(0, options.AssemblyNames.Count);
        });

        Run(results, "Refactor options: one DLL", () =>
        {
            var options = RefactorCommandOptions.Parse(
                new[] { "Example.cs", "--refs", "System.Runtime.dll" });

            AssertEqual(1, options.AssemblyNames.Count);
            AssertEqual(
                "System.Runtime.dll",
                options.AssemblyNames[0]);
        });

        Run(results, "Refactor options: two DLLs", () =>
        {
            var options = RefactorCommandOptions.Parse(
                new[]
                {
                    "Example.cs",
                    "--refs",
                    "System.Runtime.dll,System.Console.dll"
                });

            AssertEqual(2, options.AssemblyNames.Count);
        });

        Run(results, "Refactor options: empty DLL list", () =>
        {
            bool rejected = false;

            try
            {
                RefactorCommandOptions.Parse(
                    new[] { "Example.cs", "--refs", "" });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            AssertTrue(rejected, "Empty DLL list was accepted.");
        });

        Run(results, "Refactor options: duplicate DLL", () =>
        {
            bool rejected = false;

            try
            {
                RefactorCommandOptions.Parse(
                    new[]
                    {
                        "Example.cs",
                        "--refs",
                        "System.Runtime.dll,system.runtime.dll"
                    });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            AssertTrue(rejected, "Duplicate DLL was accepted.");
        });

        Run(results, "Refactor options: DLL path rejected", () =>
        {
            bool rejected = false;

            try
            {
                RefactorCommandOptions.Parse(
                    new[]
                    {
                        "Example.cs",
                        "--refs",
                        "../System.Runtime.dll"
                    });
            }
            catch (ArgumentException)
            {
                rejected = true;
            }

            AssertTrue(rejected, "DLL path was accepted.");
        });
    }
}
