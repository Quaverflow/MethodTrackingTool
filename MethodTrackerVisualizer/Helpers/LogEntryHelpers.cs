using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Helpers;

public static class LogEntryHelpers
{

    public static List<LogEntry> FindMatchingText(this List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            var found = ParametersMatch(searchText, entry)||
                        entry.ReturnValue.IsMatchingValue(searchText) ||
                        entry.MethodName.IsMatchingValue(searchText) ||
                        entry.ReturnType.IsMatchingValue(searchText)
                        || ExceptionsMatch(searchText, entry.Exceptions);


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

    private static bool ParametersMatch(string searchText, LogEntry entry)
    {
        return entry.Parameters.Any(paramValue => paramValue.Value.IsMatchingValue(searchText) || paramValue.Key.IsMatchingValue(searchText));
    }

    private static bool ExceptionsMatch(string searchText, object[] exceptions) =>
        JsonConvert.SerializeObject(exceptions).Contains(searchText);



    public static List<LogEntry> ExcludeMatching(this IEnumerable<LogEntry> data, Func<LogEntry, bool> predicate)
        => data.Where(entry => !predicate(entry))
            .Select(entry =>
            {
                var newEntry = entry.Clone();

                newEntry.Children = newEntry.Children.ExcludeMatching(predicate);
                return newEntry;
            }).ToList();
}