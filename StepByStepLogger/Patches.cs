using System.Reflection;
using System.Text.Json;
using MethodTrackerTool.Helpers;

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
            MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
            Parameters = argsDictionary ?? [],
            RawStartTime = DateTime.UtcNow
        };

        CallStack.Push(entry);
    }

    public static void LogVoidMethodExit(MethodInfo __originalMethod)
    {
        if (CallStack.Count > 0)
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = MethodLoggerHelpers.BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = "N/A";
            AddToStack(entry);
        }
    }

    internal static void LogMethodExit(MethodInfo __originalMethod, object? __result)
    {
        if (__result is Task task)
        {
            task.ContinueWith(t =>
            {
                var entry = CommonLogMethodExitSetup();
                var taskResult = task.GetType().GenericTypeArguments.Any() ? MethodLoggerHelpers.GetTaskResult(t) : t;

                entry.ReturnType = MethodLoggerHelpers.BuildReturnTypeString(__originalMethod);
                entry.ReturnValue = MethodLoggerHelpers.ConvertToSerializableValue(taskResult);
                AddToStack(entry);
            });
        }
        else
        {
            var entry = CommonLogMethodExitSetup();
            entry.ReturnType = MethodLoggerHelpers.BuildReturnTypeString(__originalMethod);
            entry.ReturnValue = MethodLoggerHelpers.ConvertToSerializableValue(__result);
            AddToStack(entry);

        }
    }

    private static void AddToStack(LogEntry entry)
    {
        if (CallStack.Count > 0)
        {
            CallStack.Peek().Children.Add(entry);
        }
        else
        {
            TopLevelCalls.Add(entry);
        }
    }

    private static LogEntry CommonLogMethodExitSetup()
    {
        var entry = CallStack.Pop();
        entry.RawEndTime = DateTime.UtcNow;

        entry.EndTime = entry.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
        entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
        entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
        return entry;
    }

}