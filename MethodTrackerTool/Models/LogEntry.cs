using System.Collections.Generic;
using System.Linq;

namespace MethodTrackerTool.Models;

public class LogEntry
{
    [System.Text.Json.Serialization.JsonIgnore]
    public LogEntry? Parent { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEntryMethod { get; set; }
    
    public string MethodName { get; set; } = "";
    public Dictionary<string, object?> Parameters { get; set; } = [];
    public string? ReturnType { get; set; }
    public object? ReturnValue { get; set; }

    public ExceptionEntry? Exception { get; set; }

    public List<LogEntry> Children { get; set; } = [];

    public LogEntry Clone()
    {
        var clone = new LogEntry
        {
            MethodName = MethodName,
            Parameters = Parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
            ReturnType = ReturnType,
            ReturnValue = ReturnValue,
            Exception = Exception,
            IsEntryMethod = IsEntryMethod,
            Children = Children.Select(child => child.Clone()).ToList()
        };

        return clone;
    }
}

public class ExceptionEntry(string message, string[] stackTrace, ExceptionEntry? innerException)
{
    public string Message { get; } = message;
    public string[] StackTrace { get; } = stackTrace;
    public ExceptionEntry? InnerException { get; } = innerException;
}