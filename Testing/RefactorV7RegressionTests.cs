using System.Collections.Generic;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    private static void RunRefactorV7RegressionTests(
        List<UnitTestResult> results)
    {
        Run(
            results,
            "RefactorV7: invalid input",
            () =>
            {
                var result =
                    SemanticExtractionValidator.Validate(
                        "",
                        "M",
                        0,
                        1);

                AssertEqual("Unsupported", result.Status);
                AssertEqual("InvalidInput", result.Reason);

                AssertTrue(
                    !result.OriginalCompilationChecked &&
                    !result.TransformedCompilationChecked,
                    "Invalid input unexpectedly triggered compilation.");
            });

        Run(
            results,
            "RefactorV7: ambiguous method",
            () =>
            {
                const string source =
                    "class C {" +
                    "void M() {} " +
                    "void M(int x) {}" +
                    "}";

                var result =
                    SemanticExtractionValidator.Validate(
                        source,
                        "M",
                        0,
                        1);

                AssertEqual("Unsupported", result.Status);
                AssertEqual("MethodNotUnique", result.Reason);

                AssertTrue(
                    !result.OriginalCompilationChecked &&
                    !result.TransformedCompilationChecked,
                    "Ambiguous method unexpectedly triggered compilation.");
            });
    }
}
