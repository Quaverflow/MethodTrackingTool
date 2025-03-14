namespace StepByStepLogger;

public class MethodLoggerOptions
{
    /// <summary>
    /// If true, performance metrics (timing data) are included.
    /// </summary>
    public bool IncludePerformanceMetrics { get; set; } = true;

    /// <summary>
    /// Format string for dates/times (used if performance metrics are enabled).
    /// </summary>
    public string DateTimeFormat { get; set; } = "HH:mm:ss:ff d/M/yyyy";

    /// <summary>
    /// Optional custom output formatter. If provided, this function is used to convert the call tree to a string.
    /// </summary>
    public Func<List<LogEntry>, string>? OutputFormatter { get; set; } = null;

    /// <summary>
    /// If true, real-time logging events will be fired as log entries are created.
    /// </summary>
    public bool EnableRealTimeLogging { get; set; } = false;

    /// <summary>
    /// Event fired when a new log entry is created (if EnableRealTimeLogging is true).
    /// </summary>
    public event Action<LogEntry>? OnLogEntry;

    internal void RaiseLogEntry(LogEntry entry)
    {
        if (EnableRealTimeLogging)
            OnLogEntry?.Invoke(entry);
    }
}