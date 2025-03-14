using System.Collections.Generic;

namespace StepByStepLogger
{
    public class LogEntry
    {
        public string MethodName { get; set; }
        public List<string> Parameters { get; set; } = new List<string>();
        public string ReturnType { get; set; }
        public string ReturnValue { get; set; }

        public string StartTime { get; set; }
        public string EndTime { get; set; }
        public string ElapsedTime { get; set; }
        public string ExclusiveElapsedTime { get; set; }

        public List<LogEntry> Children { get; set; } = new List<LogEntry>();
    }
}