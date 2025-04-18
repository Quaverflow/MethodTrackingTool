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
    public string StartTime { get; set; } = string.Empty;
    public string EndTime { get; set; } = string.Empty;
    public string ElapsedTime { get; set; } = string.Empty;
    public string ExclusiveElapsedTime { get; set; } = string.Empty;
    public string MemoryBefore { get; set; } = string.Empty;
    public string MemoryAfter { get; set; } = string.Empty;
    public string MemoryIncrease { get; set; } = string.Empty;
    public object[] Exceptions { get; set; } = [];
    public List<LogEntry> Children { get; set; } = new();

    [OnDeserialized]
    internal void OnDeserializedMethod(StreamingContext context)
    {
        MethodName ??= string.Empty;
        Parameters ??= new();
        ReturnType ??= string.Empty;
        StartTime ??= string.Empty;
        EndTime ??= string.Empty;
        ElapsedTime ??= string.Empty;
        ExclusiveElapsedTime ??= string.Empty;
        MemoryBefore ??= string.Empty;
        MemoryAfter ??= string.Empty;
        MemoryIncrease ??= string.Empty;
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