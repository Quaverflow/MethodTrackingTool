using System;

namespace MethodTrackerVisualizer.Helpers;

public static class StringExtensions
{
    public static bool IsMatchingValue<T>(this T value, string searchText) => value?.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;
}