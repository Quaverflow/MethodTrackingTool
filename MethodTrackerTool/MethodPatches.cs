using System;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;

namespace MethodTrackerTool;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MethodPatches
{
    public static TestResults Result = null!;
    public static LogEntry? Current;
    public static void Prefix(MethodInfo __originalMethod, object?[]? __args, out LogEntry __state)
    {
        try
        {
            var parameters = __originalMethod.GetParameters();
            var argsDictionary = __args?
                                     .Select((arg, i) => new
                                     {
                                         Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}",
                                         Value = MethodLoggerHelpers.ConvertToSerializableValue(arg)
                                     })
                                     .ToDictionary(x => x.Key, x => x.Value)
                                 ?? new Dictionary<string, object>();

            var entry = new LogEntry
            {
                MemoryBefore = GC.GetTotalMemory(false),
                MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
                Parameters = argsDictionary,
                RawStartTime = DateTime.UtcNow,
                StartTime = DateTime.UtcNow.ToString("HH:mm:ss:ff d/M/yyyy"),
                Parent = Current
            };

            __state = entry;
            Current = entry;
        }
        catch (Exception e)
        {
            __state = new LogEntry { MethodName = __originalMethod.Name };
            Result.UnexpectedIssues.Add(e);
        }
    }

    public static void Postfix(MethodInfo __originalMethod, object? __result, LogEntry __state) 
        => PostfixInternal(__originalMethod, __result, __state);

    public static void VoidPostfix(MethodInfo __originalMethod, LogEntry __state) 
        => PostfixInternal(__originalMethod, "void", __state);

    private static void PostfixInternal(MethodInfo __originalMethod, object? __result, LogEntry __state)
    {
        try
        {
            __state.RawEndTime = DateTime.UtcNow;
            __state.MemoryAfter = GC.GetTotalMemory(false);
            __state.EndTime = __state.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
            var elapsed = __state.RawEndTime - __state.RawStartTime;
            __state.ElapsedTime = $"{elapsed.TotalMilliseconds:F3} ms";
            __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);
            __state.ReturnValue = MethodLoggerHelpers.ConvertToSerializableValue(__result);

            Current = __state.Parent;
            if (__state.Parent != null)
            {
                __state.Parent.Children.Add(__state);
            }
            else
            {
                Result.TopLevelCalls.Add(__state);
            }
        }
        catch (Exception e)
        {
            Result.UnexpectedIssues.Add(e);
        }
    }

    public static void Finalizer(MethodInfo __originalMethod, Exception __exception, LogEntry __state)
    {
        // ReSharper disable once ConditionIsAlwaysTrueOrFalseAccordingToNullableAPIContract
        if (__exception == null)
        {
            return;
        }
        try
        {
            __state.RawEndTime = DateTime.UtcNow;
            __state.MemoryAfter = GC.GetTotalMemory(false);
            __state.EndTime = __state.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
            var elapsed = __state.RawEndTime - __state.RawStartTime;
            __state.ElapsedTime = $"{elapsed.TotalMilliseconds:F3} ms";
            __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);
            __state.Exceptions = [__exception];

            Current = __state.Parent;
            if (__state.Parent != null)
            {
                __state.Parent.Children.Add(__state);
            }
            else
            {
                Result.TopLevelCalls.Add(__state);
            }
        }
        catch (Exception e)
        {
            Result.UnexpectedIssues.Add(e);
        }
    }
}