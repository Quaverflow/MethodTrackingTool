namespace StepByStepLogger;

public class LogEntry
{
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime RawStartTime { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public DateTime RawEndTime { get; set; }
    [System.Text.Json.Serialization.JsonIgnore]
    public double RawElapsedMilliseconds => (RawEndTime - RawStartTime).TotalMilliseconds;
    [System.Text.Json.Serialization.JsonIgnore]

    public double RawExclusiveElapsedMilliseconds => RawElapsedMilliseconds - Children.Sum(child => child.RawElapsedMilliseconds);
    public string MethodName { get; set; } = "";
    public List<string> Parameters { get; set; } = new();
    public object? ReturnValue { get; set; }
    public string? ReturnValueType { get; set; }

    public string? StartTime { get; set; }
    public string? EndTime { get; set; }
    public string? ElapsedTime { get; set; }
    public string? ExclusiveElapsedTime { get; set; }
    public List<LogEntry> Children { get; set; } = new();
}