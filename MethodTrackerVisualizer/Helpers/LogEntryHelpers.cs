using System;
using System.Collections.Generic;
using System.Linq;
using System.Reflection;
using System.Runtime.InteropServices;
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


    private static bool ContainsExceptionDeep(LogEntry entry)
    {
        if (entry.Exceptions != null && entry.Exceptions.Any())
        {
            return true;
        }

        return entry.Children.Any(ContainsExceptionDeep);
    }

    public static List<LogEntry> FilterByDeepExceptions(this IEnumerable<LogEntry> data)
    {
        return data
            .Where(ContainsExceptionDeep)
            .Select(entry =>
            {
                var newEntry = Clone(entry);
                newEntry.Children = newEntry.Children.FilterByDeepExceptions();
                return newEntry;
            })
            .ToList();
    }

    public static LogEntry Clone(LogEntry entry)
    {
        return new LogEntry
        {
            MethodName = entry.MethodName,
            Exceptions = entry.Exceptions?.ToArray() ?? [],
            Children = entry.Children.Select(Clone).ToList()
        };
    }
}