using System;
using System.Collections.Generic;
using System.Linq;
using MethodTrackerVisualizer.Views;

namespace MethodTrackerVisualizer.Helpers;

public static class LogEntryHelpers
{

    public static List<LogEntry> FindMatchingMethod(this List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            if (entry.MethodName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Add(entry);
            }
            result.AddRange(FindMatchingMethod(entry.Children, searchText));
        }
        return result;
    }

    public static List<LogEntry> FindMatchingText(this List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            var found = entry.Parameters?.Any(paramValue => StringExtensions.IsMatchingValue<object>(paramValue.Value, searchText)) is true ||
                        entry.ReturnValue.IsMatchingValue( searchText);

            if (found)
            {
                result.Add(entry);
            }

            if (entry.Children != null && entry.Children.Any())
            {
                var childMatches = FindMatchingText(entry.Children, searchText);
                result.AddRange(childMatches);
            }
        }
        return result;
    }

    public static List<LogEntry> ExcludeMatching(this IEnumerable<LogEntry> data, Func<LogEntry, bool> predicate)
        => data.Where(entry => !predicate(entry))
            .Select(entry =>
            {
                var newEntry = entry.Clone();

                newEntry.Children = newEntry.Children.ExcludeMatching(predicate);
                return newEntry;
            }).ToList();
}