using System;
using System.Collections.Generic;

namespace EVI_ID.Testing;

public sealed class UnitTestResult
{
    public string Name { get; }
    public bool Passed { get; }
    public string Message { get; }

    public UnitTestResult(string name, bool passed, string message)
    {
        Name = name;
        Passed = passed;
        Message = message;
    }
}

public static partial class UnitTestRunner
{
    private static void Run(
        List<UnitTestResult> results,
        string name,
        Action test)
    {
        try
        {
            test();
            results.Add(new UnitTestResult(name, true, ""));
        }
        catch (Exception ex)
        {
            results.Add(new UnitTestResult(name, false, ex.ToString()));
        }
    }

    private static void AssertTrue(bool condition, string message)
    {
        if (!condition)
            throw new InvalidOperationException(message);
    }

    private static void AssertEqual<T>(T expected, T actual)
    {
        if (!EqualityComparer<T>.Default.Equals(expected, actual))
            throw new InvalidOperationException(
                $"Expected: {expected}; Actual: {actual}");
    }

    public static int RunPortableTests()
    {
        var results = new List<UnitTestResult>();

        RunRefactorDirectiveRegressionTests(results);
        RunRefactorDirectiveBoundaryTests(results);
        RunRefactorDirectiveBridgeTests(results);

        foreach (var result in results)
            Console.WriteLine(
                $"[{(result.Passed ? "PASS" : "FAIL")}] {result.Name}" +
                (result.Passed ? "" : "\n" + result.Message));

        Console.WriteLine(
            $"Tests: {results.Count}; Failed: {results.FindAll(x => !x.Passed).Count}");

        return results.TrueForAll(x => x.Passed) ? 0 : 1;
    }
}
