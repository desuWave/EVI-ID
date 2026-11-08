using System;

const string source = """
using System.Runtime.CompilerServices;

public class Probe
{
    private static string Caller(
        [CallerMemberName] string caller = "")
    {
        return caller;
    }

    public void Test()
    {
        System.Console.WriteLine(Caller());
    }
}
""";

var result = SemanticExtractionValidator.Validate(
    source,
    "Test",
    0,
    1);

bool passed =
    result.Status != "Safe" &&
    result.OriginalCompilationChecked &&
    result.TransformedCompilationChecked &&
    result.OriginalErrors == 0 &&
    result.TransformedErrors == 0;

Console.WriteLine(
    "=== CALLER MEMBER NAME REGRESSION ===");

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Reason: {result.Reason}");
Console.WriteLine(
    $"Original checked: {result.OriginalCompilationChecked}");
Console.WriteLine(
    $"Transformed checked: {result.TransformedCompilationChecked}");
Console.WriteLine(
    $"Original errors: {result.OriginalErrors}");
Console.WriteLine(
    $"Transformed errors: {result.TransformedErrors}");

Console.WriteLine(
    passed ? "RESULT: PASS" : "RESULT: FAIL");

Environment.ExitCode = passed ? 0 : 1;
