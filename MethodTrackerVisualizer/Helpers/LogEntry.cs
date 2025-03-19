using System.Collections.Generic;

namespace MethodTrackerVisualizer.Helpers;

public class LogEntry
{
    public string MethodName { get; set; }
    public List<ParameterEntry> Parameters { get; set; } = [];
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
}

public class ParameterEntry
{
    public string Name { get; set; }
    public string Type { get; set; }
    public object Value { get; set; }
}