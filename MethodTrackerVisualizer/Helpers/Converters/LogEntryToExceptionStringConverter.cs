using System;
using System.Globalization;
using System.Text;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers.Converters;

public class LogEntryToExceptionStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEntry entry)
        {
            var sb = new StringBuilder();
            sb.AppendLine($"Method Name: {entry.MethodName}");
            sb.AppendLine($"Exceptions: {(entry.Exceptions != null ? string.Join(", ", entry.Exceptions) : "none")}");
            sb.AppendLine($"Start: {entry.StartTime}");
            sb.AppendLine($"End: {entry.EndTime}");
            sb.AppendLine($"Memory Increase: {entry.MemoryIncrease}");
            return sb.ToString();
        }
        return string.Empty;
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}