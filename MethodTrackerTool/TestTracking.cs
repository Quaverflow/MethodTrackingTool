using System.Collections.Concurrent;
using System.Diagnostics;
using System.Reflection;
using System.Threading;
using System.Threading.Tasks;
using MethodTrackerTool.Public;

namespace MethodTrackerTool;

public static class TestTracking
{
    private static readonly AsyncLocal<string?> _currentTest = new();
    private static readonly ConcurrentDictionary<int, string> _testIdByThread = new();
    private static readonly ConcurrentDictionary<int, string> _testIdByTask = new();

    public static string GetOrAssignTestId()
    {
        if (_currentTest.Value != null)
        {
            return _currentTest.Value;
        }

        var threadId = Thread.CurrentThread.ManagedThreadId;
        if (_testIdByThread.TryGetValue(threadId, out var testName))
        {
            _currentTest.Value = testName;
            return testName;
        }

        var currentTaskId = Task.CurrentId;
        if (currentTaskId.HasValue && _testIdByTask.TryGetValue(currentTaskId.Value, out testName))
        {
            _currentTest.Value = testName;
            return testName;
        }

        testName = IdentifyCallingTest();
        if (testName != null)
        {
            _testIdByThread[threadId] = testName;
            if (currentTaskId.HasValue)
            {
                _testIdByTask[currentTaskId.Value] = testName;
            }

            _currentTest.Value = testName;
        }

        return _currentTest.Value ?? $"UnknownTest-{threadId}";
    }

    private static string? IdentifyCallingTest()
    {
        var stackTrace = new StackTrace(skipFrames: 2, fNeedFileInfo: false);
        var frames = stackTrace.GetFrames();
        if (frames == null)
        {
            return null;
        }

        foreach (var frame in frames)
        {
            var method = frame.GetMethod();
            if (method == null)
            {
                continue;
            }
            if (method.GetCustomAttribute<TestToWatchAttribute>() != null)
            {
                return $"{method.DeclaringType?.FullName}.{method.Name}";
            }
        }
        return null;
    }
}