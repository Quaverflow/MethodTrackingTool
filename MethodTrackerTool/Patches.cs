using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Threading.Tasks;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;

// ReSharper disable InconsistentNaming

namespace MethodTrackerTool;

internal static class Patches
{
    public static readonly List<LogEntry> TopLevelCalls = [];
    private static readonly Stack<LogEntry> CallStack = [];
    public static readonly List<Exception> UnexpectedIssues = [];

    public static void LogMethodEntry(MethodInfo __originalMethod, object?[]? __args)
    {
        try
        {
            var parameters = __originalMethod.GetParameters();
            var argsDictionary =
                __args?.Select((arg, i) 
                    => new ParameterEntry
                    {
                        Name = parameters[i].Name,
                        Type = parameters[i].ParameterType.FullName,
                        Value = MethodLoggerHelpers.ConvertToSerializableValue(arg)
                    })
                    .ToList();

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
        catch (Exception e)
        {
            ReportIssue(e, MethodSection.Entry);
        }
    }

    public static void LogVoidMethodExit(MethodInfo __originalMethod)
    {
        try
        {
            Finalize(__originalMethod, "void");
        }
        catch (Exception e)
        {
            ReportIssue(e, MethodSection.Exit);
        }
    }

    public static void Finalizer(MethodInfo __originalMethod, Exception __exception)
    {
        try
        {
            // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
            if (__exception != null)
            {
                Finalize(__originalMethod, "n/a", __exception);
            }
        }
        catch (Exception e)
        {
            UnexpectedIssues.Add(e);
        }
    }

    public static void LogMethodExit(MethodInfo __originalMethod, object? __result)
    {
        try
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
        catch (Exception e)
        {
            ReportIssue(e, MethodSection.Exit);
        }
    }

    /// <summary>
    /// The idea is that we report the issue and:
    /// If we are in the method entry we push an empty entry in the stack, which will be popped off when the method exits.
    /// If we're in the exit or exception we then pop the method off the list.
    /// </summary>
    /// <param name="e"></param>
    /// <param name="methodSection"></param>
    private static void ReportIssue(Exception e, MethodSection methodSection)
    {
        UnexpectedIssues.Add(e);
        if (CallStack.Count == 0)
        {
            return;
        }

        try
        {
            if (methodSection == MethodSection.Exit)
            {
                CallStack.Pop();
            }
            else
            {
                CallStack.Push(new LogEntry());
            }
        }
        catch (Exception exception)
        {
            UnexpectedIssues.Add(exception);
        }
    }

    private static void AddToStack(LogEntry entry)
    {
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

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

    private enum MethodSection
    {
        Entry,
        Exit
    }
}