using System;

namespace EVI_ID.Mdl.Editor;

public static class EditorJitTest
{
    public static string Run()
    {
        const string source = """
namespace EVI_ID.JitTest;

public sealed class TestModule
{
    public string Hello()
    {
        return "JIT OK";
    }
}
""";

        EditorCompiler compiler =
            new EditorCompiler();

        EditorCompileResult result =
            compiler.Compile(
                source,
                "EVI_ID.JitTest");

        if (!result.Success ||
            result.Assembly is null)
        {
            return
                "JIT COMPILE FAILED\n" +
                string.Join(
                    "\n",
                    result.Diagnostics);
        }

        EditorRuntime runtime =
            new EditorRuntime();

        runtime.Load(
            result.Assembly);

        object? instance =
            runtime.CreateInstance(
                "EVI_ID.JitTest.TestModule");

        if (instance is null)
        {
            return
                "JIT INSTANCE FAILED";
        }

        object? value =
            runtime.Invoke(
                instance,
                "Hello");

        return
            value?.ToString() ??
            "JIT RESULT IS NULL";
    }
}