using System;
using System.Collections.Generic;

namespace MethodTrackerTool.Models;

internal class TestResults(string name)
{
    public string Name { get; } = name;
    public readonly List<LogEntry> TopLevelCalls = new();
    public readonly List<Exception> UnexpectedIssues = new();
}