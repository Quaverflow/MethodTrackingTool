using System;
using System.Collections;
using System.Globalization;
using System.Linq;
using System.Windows.Data;

namespace MethodTrackerVisualizer.Helpers;

public class ListToStringConverter : IValueConverter
{
    public object Convert(object value, Type targetType, object parameter, CultureInfo culture) 
        => value is IEnumerable list ? string.Join(", ", list.Cast<object>()) : "";

    public object ConvertBack(object value, Type targetType, object parameter, CultureInfo culture)
    {
        throw new NotImplementedException();
    }
}