using System;
using System.Collections;
using System.Collections.Generic;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers;

public class LogEntry
{
    public string MethodName { get; set; }
    public Dictionary<string, object> Parameters { get; set; } = new();
    public string ReturnType { get; set; }
    public object ReturnValue { get; set; }

    public string StartTime { get; set; }
    public string EndTime { get; set; }
    public string ElapsedTime { get; set; }
    public string ExclusiveElapsedTime { get; set; }

    public List<LogEntry> Children { get; set; } = [];
}
public class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture)
    {
        if (value is IEnumerable list)
        {
            return string.Join(", ", list.Cast<object>());
        }
        return "";
    }

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}