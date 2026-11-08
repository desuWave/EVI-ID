using System;

var cases = new[]
{
    new
    {
        Name = "Independent local variables",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
                x++;
            }
        }
        """,
        Start = 0,
        Count = 2,
        Status = "ReviewRequired",
        Reason = "BehavioralEquivalenceNotProven",
        OriginalChecked = true,
        TransformedChecked = true
    },
    new
    {
        Name = "External output dependency",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
                x++;
                System.Console.WriteLine(x);
            }
        }
        """,
        Start = 0,
        Count = 2,
        Status = "Unsupported",
        Reason = "FlowDependenciesPresent",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "External input dependency",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
                x++;
                x++;
            }
        }
        """,
        Start = 1,
        Count = 1,
        Status = "Unsupported",
        Reason = "FlowDependenciesPresent",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Invalid statement range",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
            }
        }
        """,
        Start = 5,
        Count = 1,
        Status = "Unsupported",
        Reason = "InvalidStatementRange",
        OriginalChecked = false,
        TransformedChecked = false
    },
    new
    {
        Name = "Original compilation error",
        Source = """
        public class Probe
        {
            public void Test()
            {
                MissingWidget x = null;
            }
        }
        """,
        Start = 0,
        Count = 1,
        Status = "Unsupported",
        Reason = "OriginalCompilationErrors",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Helper name conflict",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
                x++;
            }

            private void __EVI_ExtractionValidation_Helper()
            {
            }
        }
        """,
        Start = 0,
        Count = 2,
        Status = "Unsupported",
        Reason = "HelperNameConflict",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Return statement",
        Source = """
        public class Probe
        {
            public void Test()
            {
                return;
            }
        }
        """,
        Start = 0,
        Count = 1,
        Status = "Unsupported",
        Reason = "ComplexExecution",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Lambda capture",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 10;
                System.Action action = () =>
                    System.Console.WriteLine(x);
                action();
            }
        }
        """,
        Start = 0,
        Count = 3,
        Status = "Unsupported",
        Reason = "ComplexExecution",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "This expression",
        Source = """
        public class Probe
        {
            public void Test()
            {
                System.Console.WriteLine(this.ToString());
            }
        }
        """,
        Start = 0,
        Count = 1,
        Status = "Unsupported",
        Reason = "ComplexExecution",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Try finally",
        Source = """
        public class Probe
        {
            public void Test()
            {
                try
                {
                    System.Console.WriteLine("work");
                }
                finally
                {
                    System.Console.WriteLine("cleanup");
                }
            }
        }
        """,
        Start = 0,
        Count = 1,
        Status = "Unsupported",
        Reason = "ComplexExecution",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Local function",
        Source = """
        public class Probe
        {
            public void Test()
            {
                void Local()
                {
                    System.Console.WriteLine("local");
                }

                Local();
            }
        }
        """,
        Start = 0,
        Count = 2,
        Status = "Unsupported",
        Reason = "ComplexExecution",
        OriginalChecked = true,
        TransformedChecked = false
    },
    new
    {
        Name = "Ambiguous method name",
        Source = """
        public class Probe
        {
            public void Test()
            {
                int x = 1;
            }

            public void Test(int value)
            {
                int y = value;
            }
        }
        """,
        Start = 0,
        Count = 1,
        Status = "Unsupported",
        Reason = "MethodNotUnique",
        OriginalChecked = false,
        TransformedChecked = false
    }
};

int passed = 0;

Console.WriteLine(
    "=== SEMANTIC VALIDATOR V1 HARDENING ===");

foreach (var test in cases)
{
    var result = SemanticExtractionValidator.Validate(
        test.Source,
        "Test",
        test.Start,
        test.Count);

    bool validCounters =
        result.OriginalCompilationChecked ==
            result.OriginalErrors.HasValue &&
        result.TransformedCompilationChecked ==
            result.TransformedErrors.HasValue;

    bool ok =
        result.Status == test.Status &&
        result.Reason == test.Reason &&
        result.OriginalCompilationChecked ==
            test.OriginalChecked &&
        result.TransformedCompilationChecked ==
            test.TransformedChecked &&
        validCounters;

    Console.WriteLine(
        $"{test.Name}: {(ok ? "PASS" : "FAIL")}");

    Console.WriteLine(
        $"  Status: {result.Status}");

    Console.WriteLine(
        $"  Reason: {result.Reason}");

    Console.WriteLine(
        $"  Original checked: " +
        $"{result.OriginalCompilationChecked}");

    Console.WriteLine(
        $"  Transformed checked: " +
        $"{result.TransformedCompilationChecked}");

    Console.WriteLine(
        $"  Original errors: " +
        $"{result.OriginalErrors?.ToString() ?? "N/A"}");

    Console.WriteLine(
        $"  Transformed errors: " +
        $"{result.TransformedErrors?.ToString() ?? "N/A"}");

    if (ok)
        passed++;
}

Console.WriteLine(
    $"RESULT: {passed}/{cases.Length} PASS");

Environment.ExitCode =
    passed == cases.Length ? 0 : 1;
