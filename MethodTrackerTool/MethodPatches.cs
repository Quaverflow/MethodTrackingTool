using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;

namespace MethodTrackerTool;

[SuppressMessage("ReSharper", "InconsistentNaming")]
internal static class MethodPatches
{
    public static readonly ConcurrentDictionary<string, TestResults> ResultsByTest
        = new();

    public static readonly AsyncLocal<string?> CurrentTestId = new();
    private static readonly AsyncLocal<bool> _accepting = new() { Value = false };

    private static readonly ConcurrentDictionary<string, ConcurrentDictionary<int, Stack<LogEntry>>> CallStacks = new();

    private static readonly AsyncLocal<LogEntry?> _spawnParent = new();

    public static void Initialize(string testId)
    {
        CurrentTestId.Value = testId;
        ResultsByTest[testId] = new TestResults(testId);
        _accepting.Value = true;

        CallStacks[testId] = new ConcurrentDictionary<int, Stack<LogEntry>>();

        _spawnParent.Value = null;
    }

    public static void Teardown()
    {
        _accepting.Value = false;
        CurrentTestId.Value = null;
    }

    private static bool IsActive =>
        _accepting.Value
        && CurrentTestId.Value is { } tid
        && CallStacks.ContainsKey(tid);

    private static Stack<LogEntry> GetThreadStack()
    {
        var testId = CurrentTestId.Value
                     ?? throw new InvalidOperationException("Initialize was not called.");
        var map = CallStacks[testId];
        var tid = Thread.CurrentThread.ManagedThreadId;
        return map.GetOrAdd(tid, _ => new Stack<LogEntry>());
    }

    public static void Prefix(
        MethodInfo __originalMethod,
        object?[]? __args,
        out LogEntry __state)
    {
        if (!IsActive)
        {
            __state = new LogEntry { MethodName = __originalMethod.Name };
            return;
        }

        var stack = GetThreadStack();
        var parent = stack.Count > 0
            ? stack.Peek()
            : _spawnParent.Value;

        var parameters = __originalMethod.GetParameters();
        var argsDict = __args?
                           .Select((arg, i) => new {
                               Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}",
                               Value = arg
                           })
                           .ToDictionary(x => x.Key, x => x.Value)
                       ?? [];

        __state = new LogEntry
        {
            MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
            Parameters = argsDict,
            Parent = parent
        };

        stack.Push(__state);

        _spawnParent.Value = __state;
    }

    public static void Postfix(
        MethodInfo __originalMethod,
        object? __result,
        LogEntry __state)
        => FinishInternal(__originalMethod, __result, __state);

    public static void VoidPostfix(
        MethodInfo __originalMethod,
        LogEntry __state)
        => FinishInternal(__originalMethod, "void", __state);

    public static void Finalizer(
        MethodInfo __originalMethod,
        Exception? __exception,
        LogEntry __state)
    {
        if (!IsActive || __exception == null)
        {
            return;
        }

        FinishWithException(__originalMethod, __exception, __state);
    }

    private static void FinishInternal(
        MethodInfo __originalMethod,
        object? __result,
        LogEntry __state)
    {
        if (!IsActive)
        {
            return;
        }

        var stack = GetThreadStack();
        var results = GetResultsForCurrentTest();

        __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);
        __state.ReturnValue = CommonHelpers.UnwrapTaskResult(__result);

        if (stack.Count > 0 && stack.Peek() == __state)
        {
            stack.Pop();

            if (__state.Parent != null)
            {
                __state.Parent.Children.Add(__state);
            }
            else
            {
                results.TopLevelCalls.Add(__state);
            }
        }
    }

    private static void FinishWithException(
        MethodInfo __originalMethod,
        Exception exception,
        LogEntry __state)
    {
        if (!IsActive)
        {
            return;
        }

        var stack = GetThreadStack();
        var results = GetResultsForCurrentTest();

        __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);
        __state.Exception = new ExceptionEntry(
            exception.Message,
            exception.StackTrace?
                .Split(["\r\n", "\n"], StringSplitOptions.None)
                .ToArray() ?? [],
            exception.InnerException is { } ie
                ? new ExceptionEntry(
                    ie.Message,
                    ie.StackTrace?
                        .Split(["\r\n", "\n"], StringSplitOptions.None)
                        .ToArray() ?? [],
                    null)
                : null
        );

        if (stack.Count > 0 && stack.Peek() == __state)
        {
            stack.Pop();

            if (__state.Parent != null)
            {
                __state.Parent.Children.Add(__state);
            }
            else
            {
                results.TopLevelCalls.Add(__state);
            }
        }
    }

    private static TestResults GetResultsForCurrentTest() =>
        CurrentTestId.Value is { } id
        && ResultsByTest.TryGetValue(id, out var r)
            ? r
            : throw new InvalidOperationException("TestResults not initialized");
}