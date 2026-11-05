using System;
using System.IO;

namespace EVI_ID.Mdl.Editor;

public sealed class EditorBuffer
{
    public string FilePath { get; }

    public string OriginalText { get; private set; }

    public string CurrentText { get; private set; }

    public bool IsModified =>
        !string.Equals(
            OriginalText,
            CurrentText,
            StringComparison.Ordinal);

    public EditorBuffer(
        string filePath,
        string text)
    {
        FilePath = filePath;
        OriginalText = text;
        CurrentText = text;
    }

    public static EditorBuffer Load(
        string filePath)
    {
        string text =
            File.ReadAllText(filePath);

        return new EditorBuffer(
            filePath,
            text);
    }

    public void SetText(
        string text)
    {
        CurrentText = text;
    }

    public void Reload()
    {
        CurrentText =
            File.ReadAllText(FilePath);

        OriginalText =
            CurrentText;
    }

    public void Save()
    {
        File.WriteAllText(
            FilePath,
            CurrentText);

        OriginalText =
            CurrentText;
    }

    public void Revert()
    {
        CurrentText =
            OriginalText;
    }
}