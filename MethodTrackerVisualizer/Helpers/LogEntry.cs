using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodTrackerVisualizer.Helpers;

public class LogEntry
{
    public string MethodName { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = [];
    public string ReturnType { get; set; }
    public object ReturnValue { get; set; }
    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string ElapsedTime { get; set; }
    public string ExclusiveElapsedTime { get; set; }
    public string MemoryBefore { get; set; }
    public string MemoryAfter { get; set; }
    public string MemoryIncrease { get; set; }
    public object[] Exceptions { get; set; } = [];
    public List<LogEntry> Children { get; set; } = [];

    public LogEntry Clone()
    {
        var clonedParameters = Parameters.ToDictionary(kvp => kvp.Key, kvp => kvp.Value);
        var clonedExceptions = Exceptions?.ToArray() ?? [];

        return new LogEntry
        {
            MethodName = MethodName,
            Parameters = clonedParameters,
            ReturnType = ReturnType,
            ReturnValue = ReturnValue,
            StartTime = StartTime,
            EndTime = EndTime,
            ElapsedTime = ElapsedTime,
            ExclusiveElapsedTime = ExclusiveElapsedTime,
            MemoryBefore = MemoryBefore,
            MemoryAfter = MemoryAfter,
            MemoryIncrease = MemoryIncrease,
            Exceptions = clonedExceptions,
            Children = Children.Select(child => child.Clone()).ToList()
        };
    }
}