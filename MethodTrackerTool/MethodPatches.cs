using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Reflection;
using System.Text.Json;
using System.Threading.Tasks;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;
using MethodTrackerTool.Public;

// ReSharper disable InconsistentNaming

namespace MethodTrackerTool;
internal class TestResults(string name)
{
    public readonly List<LogEntry> TopLevelCalls = [];
    public readonly Stack<LogEntry> CallStack = [];
    public readonly List<Exception> UnexpectedIssues = [];
    public string Name { get; set; } = name;
}

internal static class TestPatches
{
    public static readonly List<MethodInfo> Tests = [];
    private const BindingFlags _bindingFlags = BindingFlags.Instance | BindingFlags.Public | BindingFlags.NonPublic | BindingFlags.Static;
    
    public static void PatchTests()
    {
        var assemblies = AppDomain.CurrentDomain.GetAssemblies();
        foreach (var method in assemblies.SelectMany(x => x.GetTypes().SelectMany(y => y.GetMethods(_bindingFlags))))
        {
            if (method.GetCustomAttribute<TestToWatchAttribute>() != null)
            {
                Tests.Add(method);
                MethodPatches.Tests.TryAdd(method.DeclaringType?.FullName + "." + method.Name, new(method.DeclaringType?.FullName + "." + method.Name));
            }
        }
    }
}
internal static class MethodPatches
{
    public static readonly ConcurrentDictionary<string, TestResults> Tests = [];

    public static void LogMethodEntry(MethodInfo __originalMethod, object?[]? __args)
    {
        try
        {
            var testResults = GetTestResults();

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
            if (testResults.CallStack.Count == 0)
            {
                entry.IsEntryMethod = true;
            }
            entry.StartTime = entry.RawStartTime.ToString("HH:mm:ss:ff d/M/yyyy");

            testResults.CallStack.Push(entry);
        }
        catch (Exception e)
        {
            ReportIssue(e, MethodSection.Entry);
        }
    }

    private static TestResults GetTestResults()
    {
        var testId = TestTracking.GetOrAssignTestId();
        return Tests[testId];
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
        var testResults = GetTestResults();
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
            testResults.UnexpectedIssues.Add(e);
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
        var testResults = GetTestResults();
        testResults.UnexpectedIssues.Add(e);
        if (testResults.CallStack.Count == 0)
        {
            return;
        }

        try
        {
            if (methodSection == MethodSection.Exit)
            {
                testResults.CallStack.Pop();
            }
            else
            {
                testResults.CallStack.Push(new LogEntry());
            }
        }
        catch (Exception exception)
        {
            testResults.UnexpectedIssues.Add(exception);
        }
    }


    private static void Finalize(MethodInfo originalMethod, object? result, params Exception?[]? exceptions)
    {
        var testResults = GetTestResults();
        if (testResults.CallStack.Count == 0)
        {
            return;
        }

        var entry = testResults.CallStack.Pop();
        entry.RawEndTime = DateTime.UtcNow;
        entry.MemoryAfter = GC.GetTotalMemory(false);
        entry.EndTime = entry.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
        entry.ElapsedTime = $"{entry.RawElapsedMilliseconds:F3} ms";
        entry.ExclusiveElapsedTime = $"{entry.RawExclusiveElapsedMilliseconds:F3} ms";
        entry.ReturnType = MethodLoggerHelpers.BuildReturnTypeString(originalMethod);
        entry.Exceptions = exceptions?.OfType<Exception>().ToArray();
        entry.ReturnValue = MethodLoggerHelpers.ConvertToSerializableValue(result);
        if (entry == null)
        {
            throw new ArgumentNullException(nameof(entry));
        }

        if (testResults.CallStack.Count > 0)
        {
            testResults.CallStack.Peek().Children.Add(entry);
        }
        else
        {
            testResults.TopLevelCalls.Add(entry);
        }

    }

    private enum MethodSection
    {
        Entry,
        Exit
    }
}
