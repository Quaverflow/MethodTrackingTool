using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers.Converters;

public class LogEntryToFormattedStringConverter : IValueConverter
{
    public object Convert(object? value, Type targetType, object? parameter, CultureInfo culture)
    {
        if (value is not LogEntry entry)
        {
            return string.Empty;
        }

        var sb = new StringBuilder();
        sb.AppendLine($"Method Name: {entry.MethodName}");
        sb.AppendLine($"Parameters: {string.Join(", ", entry.Parameters.Select(kvp => $"{kvp.Key}: {kvp.Value}"))}");
        sb.AppendLine($"Return Type: {entry.ReturnType}");
        sb.AppendLine($"Return Value: {entry.ReturnValue}");
        sb.AppendLine($"Exceptions: {string.Join(", ", entry.Exception)}");
        return sb.ToString();
    }

    public object ConvertBack(object? value, Type targetType, object? parameter, CultureInfo culture) 
        => throw new NotImplementedException();
}