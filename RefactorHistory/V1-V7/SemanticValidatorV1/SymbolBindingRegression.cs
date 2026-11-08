using System;

var cases = new[]
{
    new
    {
        Name = "Instance field",
        Source = """
        public class Probe
        {
            private int counter;

            public void Test()
            {
                counter++;
            }
        }
        """,
        Reason = "InstanceFieldAccess"
    },
    new
    {
        Name = "Instance property",
        Source = """
        public class Probe
        {
            private int Counter { get; set; }

            public void Test()
            {
                Counter++;
            }
        }
        """,
        Reason = "InstancePropertyAccess"
    },
    new
    {
        Name = "Instance method",
        Source = """
        public class Probe
        {
            private void Increment() { }

            public void Test()
            {
                Increment();
            }
        }
        """,
        Reason = "InstanceMethodAccess"
    }
};

int passed = 0;

Console.WriteLine(
    "=== SYMBOL BINDING REGRESSION ===");

foreach (var test in cases)
{
    var result = SemanticExtractionValidator.Validate(
        test.Source,
        "Test",
        0,
        1);

    bool ok =
        result.Status == "Unsupported" &&
        result.Reason == test.Reason &&
        result.OriginalCompilationChecked &&
        !result.TransformedCompilationChecked &&
        result.OriginalErrors == 0 &&
        result.TransformedErrors is null;

    Console.WriteLine(
        $"{test.Name}: {(ok ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"  Status: {result.Status}");

    Console.WriteLine(
        $"  Reason: {result.Reason}");

    if (ok)
        passed++;
}

Console.WriteLine(
    $"RESULT: {passed}/{cases.Length} PASS");

Environment.ExitCode =
    passed == cases.Length ? 0 : 1;
