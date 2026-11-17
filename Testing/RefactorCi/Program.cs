using System;
using System.Collections.Generic;
using EVI_ID.Testing;

var results = new List<UnitTestResult>();

UnitTestRunner.RunCombinedExperiment(results);

int passed = 0;
int failed = 0;

foreach (var result in results)
{
    Console.WriteLine(
        $"[{(result.Passed ? "PASS" : "FAIL")}] {result.Name}");

    if (result.Passed)
        passed++;
    else
    {
        failed++;
        Console.WriteLine(result.Message);
    }
}

Console.WriteLine();
Console.WriteLine(
    $"Tests: {results.Count}; Passed: {passed}; Failed: {failed}");

return results.Count > 0 && failed == 0 ? 0 : 1;
