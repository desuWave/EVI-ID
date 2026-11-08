using System;

const string source = """
using System.Runtime.CompilerServices;

public sealed class Payload
{
    public string Caller { get; }

    public Payload(
        [CallerMemberName] string caller = "")
    {
        Caller = caller;
    }
}

public class Probe
{
    public void Test()
    {
        new Payload();
    }
}
""";

Console.WriteLine(
    "=== CONSTRUCTOR CALLER INFO REGRESSION V2 ===");

var result = SemanticExtractionValidator.Validate(
    source,
    "Test",
    0,
    1);

Console.WriteLine($"Status: {result.Status}");
Console.WriteLine($"Reason: {result.Reason}");
Console.WriteLine(
    $"Original checked: {result.OriginalCompilationChecked}");
Console.WriteLine(
    $"Transformed checked: {result.TransformedCompilationChecked}");
Console.WriteLine(
    $"Original errors: {result.OriginalErrors?.ToString() ?? "N/A"}");
Console.WriteLine(
    $"Transformed errors: {result.TransformedErrors?.ToString() ?? "N/A"}");

bool detected =
    result.Status == "Unsupported" &&
    result.Reason == "ImplicitCallerInfoDependency" &&
    result.OriginalCompilationChecked &&
    !result.TransformedCompilationChecked;

bool missed =
    result.Status == "ReviewRequired" &&
    result.Reason == "BehavioralEquivalenceNotProven" &&
    result.OriginalCompilationChecked &&
    result.TransformedCompilationChecked &&
    result.OriginalErrors == 0 &&
    result.TransformedErrors == 0;

if (detected)
{
    Console.WriteLine("RESULT: PROTECTION DETECTED");
}
else if (missed)
{
    Console.WriteLine("RESULT: CONFIRMED DIAGNOSTIC GAP");
}
else
{
    Console.WriteLine("RESULT: INCONCLUSIVE");
}

Environment.ExitCode =
    detected || missed ? 0 : 1;
