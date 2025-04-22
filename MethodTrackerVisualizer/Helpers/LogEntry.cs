using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// This is because we set everything to a default value on deserialization, hence they can be set as non nullable.

namespace MethodTrackerVisualizer.Helpers;

public class ExceptionEntry(string message, string[] stackTrace, ExceptionEntry? innerException)
{
    public string Message { get; } = message;
    public string[] StackTrace { get; } = stackTrace;
    public ExceptionEntry? InnerException { get; } = innerException;

    public override string ToString() => ToString(0);

    public string ToString(int indentLevel)
    {
        var indent = new string('\t', indentLevel);
        return $"""
                {indent}{Message}
                {indent}{string.Join(Environment.NewLine, StackTrace)}
                {indent}InnerException: {innerException?.ToString(indentLevel + 1)}
                """;
    }
}
public class LogEntry
{
    public string MethodName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = [];
    public string ReturnType { get; set; } = string.Empty;
    public object? ReturnValue { get; set; }
    public ExceptionEntry? Exception { get; set; }
    public List<LogEntry> Children { get; set; } = [];

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        MethodName ??= string.Empty;
        Parameters ??= [];
        ReturnType ??= string.Empty;
        Children ??= [];
    }
}

public class EntryFile
{
    public DateTime Updated { get; set; }
    public string FileName { get; set; } = string.Empty;
    public List<LogEntry> Data { get; set; } = [];

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        FileName ??= string.Empty;
        Data ??= [];
    }
}