using System;

var tests = new[]
{
    (
        Name: "Local variable nameof",
        Source: """
public class Probe
{
    public void Test()
    {
        int value = 123;
        System.Console.WriteLine(nameof(value));
    }
}
""",
        Start: 0,
        Count: 2
    ),
    (
        Name: "Static method nameof",
        Source: """
public class Probe
{
    private static void Execute() { }

    public void Test()
    {
        System.Console.WriteLine(nameof(Execute));
    }
}
""",
        Start: 0,
        Count: 1
    ),
    (
        Name: "Type nameof",
        Source: """
public class Probe
{
    public void Test()
    {
        System.Console.WriteLine(nameof(System.String));
    }
}
""",
        Start: 0,
        Count: 1
    )
};

Console.WriteLine("=== NAMEOF REGRESSION ===");

foreach (var test in tests)
{
    var result = SemanticExtractionValidator.Validate(
        test.Source,
        "Test",
        test.Start,
        test.Count);

    Console.WriteLine($"\n{test.Name}");
    Console.WriteLine($"  Status: {result.Status}");
    Console.WriteLine($"  Reason: {result.Reason}");
    Console.WriteLine(
        $"  Original checked: {result.OriginalCompilationChecked}");
    Console.WriteLine(
        $"  Transformed checked: {result.TransformedCompilationChecked}");
    Console.WriteLine(
        $"  Original errors: {result.OriginalErrors?.ToString() ?? "N/A"}");
    Console.WriteLine(
        $"  Transformed errors: {result.TransformedErrors?.ToString() ?? "N/A"}");
}

Console.WriteLine("\nRESULT: DIAGNOSTIC COMPLETE");
