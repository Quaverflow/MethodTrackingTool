using System.Reflection;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;

// ReSharper disable InconsistentNaming

namespace MethodTrackerTool;

public static class Patches
{
    internal static readonly List<LogEntry> TopLevelCalls = [];
    internal static readonly Stack<LogEntry> CallStack = new();

    internal static void LogMethodEntry(MethodInfo __originalMethod, object?[]? __args)
    {
        var parameters = __originalMethod.GetParameters();
        var argsDictionary =
            __args?.Select((arg, i) => new
            {
                Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}",
                Value = MethodLoggerHelpers.ConvertToSerializableValue(arg)
            })
                .ToDictionary(x => x.Key, x => x.Value);


        var entry = new LogEntry
        {
            MemoryBefore = GC.GetTotalMemory(false),
            MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
            Parameters = argsDictionary ?? [],
            RawStartTime = DateTime.UtcNow
        };
            entry.StartTime = entry.RawStartTime.ToString("HH:mm:ss:ff d/M/yyyy");
        
            CallStack.Push(entry);
    }

    public static void LogVoidMethodExit(MethodInfo __originalMethod)
        => Finalize(__originalMethod, "void");

    public static void Finalizer(MethodInfo __originalMethod, Exception __exception)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (__exception != null)
        {
            Finalize(__originalMethod, "n/a", __exception);
        }
    }

    internal static void LogMethodExit(MethodInfo __originalMethod, object? __result)
    {
        if (__result is Task task)
        {
            task.ContinueWith(t =>
            {
                if (t.Exception?.InnerExceptions is { Count: > 0 } exceptions)
                {
                    Finalize(__originalMethod, "n/a", [.. exceptions]);
                }
                else
                {
                    var taskResult = t.GetType().GenericTypeArguments.Any()
                        ? MethodLoggerHelpers.GetTaskResult(t)
                        : t;
                    Finalize(__originalMethod, taskResult);
                }
            });
        }
        else
        {
            Finalize(__originalMethod, __result);
        }
    }

    private static void AddToStack(LogEntry entry)
    {
        ArgumentNullException.ThrowIfNull(entry);

        if (CallStack.Count > 0)
        {
            CallStack.Peek().Children.Add(entry);
        }
        else
        {
            TopLevelCalls.Add(entry);
        }
    }

    private static void Finalize(MethodInfo originalMethod, object? result, params Exception?[]? exceptions)
    {
        if (CallStack.Count == 0)
        {
            return;
        }

        var entry = CallStack.Pop();
        entry.RawEndTime = DateTime.UtcNow;
        entry.MemoryAfter = GC.GetTotalMemory(false);
        entry.EndTime = entry.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
        entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
        entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
        entry.ReturnType = MethodLoggerHelpers.BuildReturnTypeString(originalMethod);
        entry.Exceptions = exceptions?.OfType<Exception>().ToArray();
        entry.ReturnValue = MethodLoggerHelpers.ConvertToSerializableValue(result);
        AddToStack(entry);
    }
}