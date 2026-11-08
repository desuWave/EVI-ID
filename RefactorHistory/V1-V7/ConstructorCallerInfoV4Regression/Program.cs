using System;

const string prelude = """
using System.Runtime.CompilerServices;

public sealed class Payload
{
    public Payload(
        [CallerMemberName] string caller = "")
    {
    }
}

public class Probe
{
    public void Test()
    {
""";

const string suffix = """
    }
}
""";

var tests = new[]
{
    (
        Name: "Explicit creation with implicit caller info",
        Statement: "new Payload();",
        ExpectedStatus: "Unsupported",
        ExpectedReason: "ImplicitCallerInfoDependency"
    ),
    (
        Name: "Target-typed creation with implicit caller info",
        Statement: "Payload payload = new();",
        ExpectedStatus: "Unsupported",
        ExpectedReason: "ImplicitCallerInfoDependency"
    ),
    (
        Name: "Constructor with explicit caller argument",
        Statement: "new Payload(caller: \"Explicit\");",
        ExpectedStatus: "ReviewRequired",
        ExpectedReason: "BehavioralEquivalenceNotProven"
    ),
    (
        Name: "Ordinary constructor",
        Statement: "new object();",
        ExpectedStatus: "ReviewRequired",
        ExpectedReason: "BehavioralEquivalenceNotProven"
    )
};

Console.WriteLine(
    "=== V4 CONSTRUCTOR CALLER INFO REGRESSION ===");

int passed = 0;

foreach (var test in tests)
{
    string source =
        prelude + "\n" +
        test.Statement + "\n" +
        suffix;

    var result = SemanticExtractionValidator.Validate(
        source, "Test", 0, 1);

    bool shouldReject =
        test.ExpectedStatus == "Unsupported";

    bool ok =
        result.Status == test.ExpectedStatus &&
        result.Reason == test.ExpectedReason &&
        result.OriginalCompilationChecked &&
        result.OriginalErrors == 0 &&
        result.TransformedCompilationChecked == !shouldReject &&
        result.TransformedErrors ==
            (shouldReject ? null : 0);

    Console.WriteLine(
        $"{test.Name}: {(ok ? "PASS" : "FAIL")}");

    Console.WriteLine($"  Status: {result.Status}");
    Console.WriteLine($"  Reason: {result.Reason}");
    Console.WriteLine(
        $"  Original errors: {result.OriginalErrors?.ToString() ?? "N/A"}");
    Console.WriteLine(
        $"  Transformed checked: {result.TransformedCompilationChecked}");
    Console.WriteLine(
        $"  Transformed errors: {result.TransformedErrors?.ToString() ?? "N/A"}");

    if (ok)
        passed++;
}

Console.WriteLine(
    $"RESULT: {passed}/{tests.Length} PASS");

Environment.ExitCode =
    passed == tests.Length ? 0 : 1;
