using System;
using System.Reflection;

namespace EVI_ID.Mdl.Editor;

public sealed class EditorRuntime
{
    private Assembly? _assembly;

    public bool IsLoaded =>
        _assembly is not null;

    public Assembly? Assembly =>
        _assembly;

    public void Load(
        Assembly assembly)
    {
        _assembly = assembly;
    }

    public void Clear()
    {
        _assembly = null;
    }

    public Type? FindType(
        string fullName)
    {
        if (_assembly is null)
            return null;

        return _assembly.GetType(
            fullName,
            throwOnError: false,
            ignoreCase: false);
    }

    public object? CreateInstance(
        string fullName)
    {
        Type? type =
            FindType(fullName);

        if (type is null)
            return null;

        return Activator.CreateInstance(
            type);
    }

    public object? Invoke(
        object instance,
        string methodName,
        params object?[] arguments)
    {
        Type type =
            instance.GetType();

        MethodInfo? method =
            type.GetMethod(
                methodName,
                BindingFlags.Instance |
                BindingFlags.Static |
                BindingFlags.Public |
                BindingFlags.NonPublic);

        if (method is null)
        {
            throw new MissingMethodException(
                type.FullName,
                methodName);
        }

        return method.Invoke(
            instance,
            arguments);
    }
}