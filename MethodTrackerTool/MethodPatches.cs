using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Diagnostics.CodeAnalysis;
using System.Linq;
using System.Reflection;
using System.Threading;
using MethodTrackerTool.Helpers;
using MethodTrackerTool.Models;

namespace MethodTrackerTool
{
    [SuppressMessage("ReSharper", "InconsistentNaming")]
    internal static class MethodPatches
    {
        public static readonly ConcurrentDictionary<string, TestResults> ResultsByTest = new();

        private static readonly AsyncLocal<string?> CurrentTestId = new();
        private static readonly ConcurrentDictionary<string, Stack<LogEntry>> CallStacks = new();

        /// <summary>
        /// Must be called at test setup. Assigns a unique ID for this test and initializes its TestResults and call stack.
        /// </summary>
        public static void InitializeForTest(string testId)
        {
            CurrentTestId.Value = testId;
            ResultsByTest[testId] = new TestResults(testId);
            CallStacks[testId] = new Stack<LogEntry>();
        }

        /// <summary>
        /// Retrieves the TestResults for the current test.
        /// </summary>
        public static TestResults GetResultsForCurrentTest() =>
            CurrentTestId.Value is not { } id || !ResultsByTest.TryGetValue(id, out var results)
                ? throw new InvalidOperationException("TestResults not initialized for current test.")
                : results;

        public static void Prefix(MethodInfo __originalMethod, object?[]? __args, out LogEntry __state)
        {
            try
            {
                var testId = CurrentTestId.Value ?? throw new InvalidOperationException("InitializeForTest was not called.");
                var stack = CallStacks[testId];

                var parent = stack.Count > 0 ? stack.Peek() : null;

                var parameters = __originalMethod.GetParameters();
                var argsDictionary = __args?
                    .Select((arg, i) => new
                    {
                        Key = $"{parameters[i].ParameterType.FullName} {parameters[i].Name}",
                        Value = arg
                    })
                    .ToDictionary(x => x.Key, x => x.Value)
                    ?? [];

                var entry = new LogEntry
                {
                    MemoryBefore = GC.GetTotalMemory(false),
                    MethodName = $"{__originalMethod.DeclaringType?.Name}.{__originalMethod.Name}",
                    Parameters = argsDictionary,
                    RawStartTime = DateTime.UtcNow,
                    StartTime = DateTime.UtcNow.ToString("HH:mm:ss:ff d/M/yyyy"),
                    Parent = parent
                };

                stack.Push(entry);
                __state = entry;
            }
            catch (Exception e)
            {
                __state = new LogEntry { MethodName = __originalMethod.Name };
                GetResultsForCurrentTest().UnexpectedIssues.Add(e);
            }
        }

        public static void Postfix(MethodInfo __originalMethod, object? __result, LogEntry __state)
            => FinishInternal(__originalMethod, __result, __state);

        public static void VoidPostfix(MethodInfo __originalMethod, LogEntry __state)
            => FinishInternal(__originalMethod, "void", __state);

        public static void Finalizer(MethodInfo __originalMethod, Exception __exception, LogEntry __state)
        {
            if (__exception == null)
            {
                return;
            }

            FinishWithException(__originalMethod, __exception, __state);
        }

        private static void FinishInternal(MethodInfo __originalMethod, object? __result, LogEntry __state)
        {
            try
            {
                var testId = CurrentTestId.Value ?? throw new InvalidOperationException("InitializeForTest was not called.");
                var results = ResultsByTest[testId];
                var stack = CallStacks[testId];

                __state.RawEndTime = DateTime.UtcNow;
                __state.MemoryAfter = GC.GetTotalMemory(false);
                __state.EndTime = __state.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
                var elapsed = __state.RawEndTime - __state.RawStartTime;
                __state.ElapsedTime = $"{elapsed.TotalMilliseconds:F3} ms";

                __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);

                __state.ReturnValue = CommonHelpers.UnwrapTaskResult(__result);

                if (stack.Pop() != __state)
                {
                    throw new InvalidOperationException("Call stack mismatch in FinishInternal");
                }

                if (__state.Parent != null)
                {
                    __state.Parent.Children.Add(__state);
                }
                else
                {
                    results.TopLevelCalls.Add(__state);
                }
            }
            catch (Exception e)
            {
                GetResultsForCurrentTest().UnexpectedIssues.Add(e);
            }
        }

        private static void FinishWithException(MethodInfo __originalMethod, Exception exception, LogEntry __state)
        {
            try
            {
                var testId = CurrentTestId.Value ?? throw new InvalidOperationException("InitializeForTest was not called.");
                var results = ResultsByTest[testId];
                var stack = CallStacks[testId];

                __state.RawEndTime = DateTime.UtcNow;
                __state.MemoryAfter = GC.GetTotalMemory(false);
                __state.EndTime = __state.RawEndTime.ToString("HH:mm:ss:ff d/M/yyyy");
                var elapsed = __state.RawEndTime - __state.RawStartTime;
                __state.ElapsedTime = $"{elapsed.TotalMilliseconds:F003} ms";
                __state.ReturnType = TypeHelpers.BuildTypeName(__originalMethod.ReturnType);
                __state.Exceptions = [exception];

                if (stack.Pop() != __state)
                {
                    throw new InvalidOperationException("Call stack mismatch in FinishWithException");
                }

                if (__state.Parent != null)
                {
                    __state.Parent.Children.Add(__state);
                }
                else
                {
                    results.TopLevelCalls.Add(__state);
                }
            }
            catch (Exception e)
            {
                GetResultsForCurrentTest().UnexpectedIssues.Add(e);
            }
        }
    }
}
