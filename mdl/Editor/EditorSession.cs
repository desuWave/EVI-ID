using System;

namespace EVI_ID.Mdl.Editor;

public sealed class EditorSession
{
    private readonly EditorCompiler _compiler;
    private readonly EditorRuntime _runtime;

    public EditorBuffer? Buffer { get; private set; }

    public EditorCompileResult? LastCompile { get; private set; }

    public EditorSession()
    {
        _compiler =
            new EditorCompiler();
            
        _runtime =
    new EditorRuntime();
    
    }

    public EditorBuffer Open(
        string filePath)
    {
        Buffer =
            EditorBuffer.Load(
                filePath);

        LastCompile = null;

        return Buffer;
    }

    public void SetText(
        string text)
    {
        if (Buffer is null)
        {
            throw new InvalidOperationException(
                "No file is open.");
        }

        Buffer.SetText(text);
    }

    public EditorCompileResult Compile()
    {
        if (Buffer is null)
        {
            throw new InvalidOperationException(
                "No file is open.");
        }

        string assemblyName =
            "EVI_ID.Editor.JIT." +
            Guid.NewGuid()
                .ToString("N");

        LastCompile =
            _compiler.Compile(
                Buffer.CurrentText,
                assemblyName);

        return LastCompile;
    }

    public void Save()
    {
        if (Buffer is null)
        {
            throw new InvalidOperationException(
                "No file is open.");
        }

        Buffer.Save();
    }

    public void Revert()
    {
        if (Buffer is null)
        {
            throw new InvalidOperationException(
                "No file is open.");
        }

        Buffer.Revert();

        LastCompile = null;
    }
    
    public bool Apply()
{
    if (LastCompile is null ||
        !LastCompile.Success ||
        LastCompile.Assembly is null)
    {
        return false;
    }

    _runtime.Load(
        LastCompile.Assembly);

    return true;
}
    
    public EditorRuntime Runtime =>
    _runtime;
    
}