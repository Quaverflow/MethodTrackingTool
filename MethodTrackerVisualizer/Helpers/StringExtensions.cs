using System;
using System.Linq;

namespace MethodTrackerVisualizer.Helpers;

public static class StringExtensions
{
    public static bool IsMatchingValue<T>(this T value, string searchText)
    {
        var str = value?.ToString();
        if (value == null)
        {
            return false;
        }

        var keywords = searchText
            .Split(['&'], StringSplitOptions.RemoveEmptyEntries)
            .Select(x => x.Trim())
            .Distinct(StringComparer.InvariantCultureIgnoreCase)
            .ToArray();

        return keywords.All(kw => str.IndexOf(kw, StringComparison.OrdinalIgnoreCase) >= 0);
    }
}