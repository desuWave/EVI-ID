using System.Collections.Generic;

namespace EVI_ID.Testing;

public static partial class UnitTestRunner
{
    public static void RunCombinedExperiment(
        List<UnitTestResult> results)
    {
        RunRefactorDirectiveRegressionTests(results);
        RunRefactorDirectiveBoundaryTests(results);
        RunRefactorDirectiveBridgeTests(results);

        RunRefactorSemanticBridgeTests(results);
        RunRefactorEngineReferenceTests(results);

        RunLegacyCoverageTests(results);
    }
}
