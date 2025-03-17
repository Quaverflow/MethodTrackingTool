using Microsoft.VisualStudio.OLE.Interop;
using Microsoft.VisualStudio.Text.Differencing;
using System;
using System.Globalization;
using System.Linq;
using System.Text;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers.Converters;

public class LogEntryToExceptionStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is LogEntry entry)
        {
            return GetExceptionInfo(entry);
        }
        return string.Empty;
    }

    private static string GetExceptionInfo(LogEntry entry)
    {
        var sb = new StringBuilder();

        if (ContainsExceptions(entry))
        {
            sb.AppendLine($"Method: {entry.MethodName} at {entry.StartTime}");
            sb.AppendLine($"Exceptions: {string.Join(" | ", entry.Exceptions.Select(e => e.ToString()))}");
        }
        else if (entry.Children.Any(ContainsExceptions))
        {
            var handledMethodsExceptions = entry.Children.Where(ContainsExceptions).Select(x => x.MethodName);
            sb.AppendLine($"Method: {entry.MethodName} did not throw an exception, but handled exceptions in:");
            foreach (var child in handledMethodsExceptions)
            {
                sb.Append($"    {child}");
            }
        }
        else
        {
            sb.AppendLine("An exception occurred and was handled in a lower level method.");
        }
        return sb.ToString();
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture) => throw new NotImplementedException();
    private static bool ContainsExceptions(LogEntry entry) => entry.Exceptions?.Any() == true;

}