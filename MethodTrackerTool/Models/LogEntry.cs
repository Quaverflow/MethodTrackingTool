﻿using System;
using System.Collections.Generic;
using System.Linq;

namespace MethodTrackerTool.Models;

public class LogEntry
{
    [System.Text.Json.Serialization.JsonIgnore]
    public LogEntry? Parent { get; set; }

    [System.Text.Json.Serialization.JsonIgnore]
    public bool IsEntryMethod { get; set; }
  
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime RawStartTime { get; set; }    
    
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime RawEndTime { get; set; }
    
    [System.Text.Json.Serialization.JsonIgnore]
    public double RawElapsedMilliseconds => (RawEndTime - RawStartTime).TotalMilliseconds;
   
    [System.Text.Json.Serialization.JsonIgnore]
    public double RawExclusiveElapsedMilliseconds => GetRawExclusiveElapsedMilliseconds();

    private double GetRawExclusiveElapsedMilliseconds()
    {
        var result = RawElapsedMilliseconds - Children.Sum(child => child.RawElapsedMilliseconds);

        return result < 0 ? 0 : result;
    }

    public string MethodName { get; set; } = "";
    public Dictionary<string, object> Parameters { get; set; } = [];
    public string? ReturnType { get; set; }
    public object? ReturnValue { get; set; }

    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? ElapsedTime { get; set; }
    public string? ExclusiveElapsedTime { get; set; }

    public long MemoryBefore { get; set; }
    public long MemoryAfter { get; set; }
    public long MemoryIncrease => MemoryAfter - MemoryBefore;

    public Exception[]? Exceptions { get; set; }

    public List<LogEntry> Children { get; set; } = [];

    public LogEntry Clone()
    {
        var clone = new LogEntry
        {
            MethodName = MethodName,
            Parameters = Parameters.ToDictionary(kv => kv.Key, kv => kv.Value),
            ReturnType = ReturnType,
            ReturnValue = ReturnValue,
            StartTime = StartTime,
            EndTime = EndTime,
            ElapsedTime = ElapsedTime,
            ExclusiveElapsedTime = ExclusiveElapsedTime,
            MemoryBefore = MemoryBefore,
            MemoryAfter = MemoryAfter,
            Exceptions = Exceptions?.ToArray(),
            IsEntryMethod = IsEntryMethod,
            RawStartTime = RawStartTime,
            RawEndTime = RawEndTime,
            Children = Children.Select(child => child.Clone()).ToList()
        };

        return clone;
    }
}