using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers.Converters;

public class LogEntryToFormattedStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Method Name: {entry.MethodName}");
            sb.AppendLine($"Parameters: {(entry.Parameters != null ? string.Join(", ", entry.Parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}")) : "none")}");
            sb.AppendLine($"Return Type: {entry.ReturnType}");
            sb.AppendLine($"Return Value: {entry.ReturnValue}");
            sb.AppendLine($"Start: {entry.StartTime}");
            sb.AppendLine($"Memory Before: {entry.MemoryBefore}");
            sb.AppendLine($"End: {entry.EndTime}");
            sb.AppendLine($"Memory After: {entry.MemoryAfter}");
            sb.AppendLine($"Total Elapsed: {entry.ElapsedTime}");
            sb.AppendLine($"Exclusive Elapsed: {entry.ExclusiveElapsedTime}");
            sb.AppendLine($"Memory Increase: {entry.MemoryIncrease}");
            sb.AppendLine($"Exceptions: {(entry.Exceptions != null ? string.Join(", ", entry.Exceptions) : "none")}");
            return sb.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
}