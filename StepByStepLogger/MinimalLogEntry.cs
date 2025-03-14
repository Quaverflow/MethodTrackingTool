namespace StepByStepLogger;

public class MinimalLogEntry
{
    public string MethodName { get; set; } = "";
    public List<string> Parameters { get; set; } = new();
    public object? ReturnValue { get; set; }
    public string ReturnValueType { get; set; } = "";
    public List<MinimalLogEntry> Children { get; set; } = new();
}