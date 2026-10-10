using System;
using System.Collections.Generic;
using EVI_ID.Shell;

namespace EVI_ID.Shell;

public enum OperationStatus
{
    Running,
    Completed,
    Failed,
    Cancelled
}

public sealed class ShellOperation
{
    public Guid Id { get; } = Guid.NewGuid();

    public string Name { get; }

    public OperationStatus Status { get; internal set; }

    public DateTime StartedAt { get; } = DateTime.Now;

    public DateTime? FinishedAt { get; internal set; }

    public string? Result { get; internal set; }

    internal ShellOperation(string name)
    {
        Name = name;
        Status = OperationStatus.Running;
    }
}

public static class ProcessManager
{
    private static readonly object Sync = new();

    private static readonly Dictionary<Guid, ShellOperation>
        Operations = new();

    public static ShellOperation StartOperation(string name)
    {
        if (string.IsNullOrWhiteSpace(name))
        {
            throw new ArgumentException(
                "Operation name cannot be empty.",
                nameof(name));
        }

        var operation = new ShellOperation(name);

        lock (Sync)
        {
            Operations.Add(operation.Id, operation);
        }

        Logger.Info(
            $"Operation started: {operation.Name}; ID={operation.Id}");

        return operation;
    }

    public static bool CompleteOperation(
        Guid id,
        string result = "Completed successfully.")
    {
        return FinishOperation(
            id,
            OperationStatus.Completed,
            result);
    }

    public static bool FailOperation(
        Guid id,
        string error)
    {
        return FinishOperation(
            id,
            OperationStatus.Failed,
            error);
    }

    public static bool CancelOperation(
        Guid id,
        string reason = "Cancelled.")
    {
        return FinishOperation(
            id,
            OperationStatus.Cancelled,
            reason);
    }

    public static IReadOnlyList<ShellOperation> GetOperations()
    {
        lock (Sync)
        {
            return new List<ShellOperation>(Operations.Values);
        }
    }

    private static bool FinishOperation(
        Guid id,
        OperationStatus status,
        string result)
    {
        ShellOperation operation;

        lock (Sync)
        {
            if (!Operations.TryGetValue(id, out operation!))
            {
                Logger.Warning(
                    $"Operation not found: ID={id}");
                return false;
            }

            if (operation.Status != OperationStatus.Running)
            {
                Logger.Warning(
                    $"Operation already finished: {operation.Name}");
                return false;
            }

            operation.Status = status;
            operation.Result = result;
            operation.FinishedAt = DateTime.Now;
        }

        Logger.Info(
            $"Operation {status}: {operation.Name}; {result}");

        return true;
    }
}