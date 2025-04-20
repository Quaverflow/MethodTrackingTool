using System;
using System.Collections.Generic;
using System.Runtime.Serialization;
// ReSharper disable NullCoalescingConditionIsAlwaysNotNullAccordingToAPIContract
// This is because we set everything to a default value on deserialization, hence they can be set as non nullable.

namespace MethodTrackerVisualizer.Helpers;

public class LogEntry
{
    public string MethodName { get; set; } = string.Empty;
    public Dictionary<string, object> Parameters { get; set; } = [];
    public string ReturnType { get; set; } = string.Empty;
    public object? ReturnValue { get; set; }
    public object[] Exceptions { get; set; } = [];
    public List<LogEntry> Children { get; set; } = new();

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        MethodName ??= string.Empty;
        Parameters ??= [];
        ReturnType ??= string.Empty;
        Exceptions ??= [];
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