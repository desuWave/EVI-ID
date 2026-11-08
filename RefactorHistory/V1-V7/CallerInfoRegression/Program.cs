using System;

const string memberName = """
using System.Runtime.CompilerServices;
public class Probe
{
    private static void Log(
        [CallerMemberName] string name = "") { }

    public void Test()
    {
        Log();
    }
}
""";

const string filePath = """
using System.Runtime.CompilerServices;
public class Probe
{
    private static void Log(
        [CallerFilePath] string path = "") { }

    public void Test()
    {
        Log();
    }
}
""";

const string lineNumber = """
using System.Runtime.CompilerServices;
public class Probe
{
    private static void Log(
        [CallerLineNumber] int line = 0) { }

    public void Test()
    {
        Log();
    }
}
""";

const string argumentExpression = """
using System.Runtime.CompilerServices;
public class Probe
{
    private static void Log(
        int value,
        [CallerArgumentExpression("value")]
        string expression = "") { }

    public void Test()
    {
        Log(123);
    }
}
""";

const string explicitArgument = """
using System.Runtime.CompilerServices;
public class Probe
{
    private static void Log(
        [CallerMemberName] string name = "") { }

    public void Test()
    {
        Log(name: "Explicit");
    }
}
""";

const string ordinaryCall = """
public class Probe
{
    private static void Log(int value) { }

    public void Test()
    {
        Log(123);
    }
}
""";

var tests = new[]
{
    ("CallerMemberName", memberName, true),
    ("CallerFilePath", filePath, true),
    ("CallerLineNumber", lineNumber, true),
    ("CallerArgumentExpression", argumentExpression, true),
    ("Explicit caller argument", explicitArgument, false),
    ("Ordinary invocation", ordinaryCall, false)
};

int passed = 0;

Console.WriteLine("=== CALLER INFO REGRESSION ===");

foreach (var (name, source, shouldReject) in tests)
{
    var result = SemanticExtractionValidator.Validate(
        source, "Test", 0, 1);

    string expectedStatus = shouldReject
        ? "Unsupported"
        : "ReviewRequired";

    string expectedReason = shouldReject
        ? "ImplicitCallerInfoDependency"
        : "BehavioralEquivalenceNotProven";

    bool ok =
        result.Status == expectedStatus &&
        result.Reason == expectedReason &&
        result.OriginalCompilationChecked &&
        result.OriginalErrors == 0 &&
        result.TransformedCompilationChecked == !shouldReject &&
        result.TransformedErrors ==
            (shouldReject ? null : 0);

    Console.WriteLine(
        $"{name}: {(ok ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"  Status: {result.Status}");

    Console.WriteLine(
        $"  Reason: {result.Reason}");

    Console.WriteLine(
        $"  Original errors: {result.OriginalErrors}");

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
