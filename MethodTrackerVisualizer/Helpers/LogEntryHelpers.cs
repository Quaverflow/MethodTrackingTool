using System;
using System.Collections.Generic;
using System.Linq;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Helpers;

public static class LogEntryHelpers
{
    public static List<LogEntry> FindMatchingText(
        this IReadOnlyList<LogEntry> roots,
        string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return new List<LogEntry>();
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new List<LogEntry>(capacity: 128);
        var seen = new HashSet<LogEntry>();

        foreach (var root in roots)
        {
            var stack = new Stack<LogEntry>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var e = stack.Pop();

                if (EntryMatchesSpan(e, searchText.AsSpan(), comparison))
                {
                    if (seen.Add(e))
                    {
                        results.Add(e);
                    }
                }

                for (int i = e.Children.Count - 1; i >= 0; i--)
                {
                    stack.Push(e.Children[i]);
                }
            }
        }

        return results;
    }

    private static bool EntryMatchesSpan(
        LogEntry entry,
        ReadOnlySpan<char> needle,
        StringComparison comparison)
    {
        if (entry.MethodName.AsSpan().IndexOf(needle, comparison) >= 0)
        {
            return true;
        }

        if (entry.ReturnType.AsSpan().IndexOf(needle, comparison) >= 0)
        {
            return true;
        }

        if (entry.Exception is { } ex && ex.Message.AsSpan().IndexOf(needle, comparison) >= 0)
        {
            return true;
        }

        if (entry.ReturnValue is not null)
        {
            var rv = entry.ReturnValue.ToString();
            if (!string.IsNullOrEmpty(rv) && rv.AsSpan().IndexOf(needle, comparison) >= 0)
            {
                return true;
            }
        }

        foreach (var kv in entry.Parameters)
        {
            if (kv.Key.AsSpan().IndexOf(needle, comparison) >= 0)
            {
                return true;
            }

            if (kv.Value is not { } vo)
            {
                continue;
            }

            var vs = vo.ToString();
            if (!string.IsNullOrEmpty(vs) && vs.AsSpan().IndexOf(needle, comparison) >= 0)
            {
                return true;
            }
        }

        return false;
    }
    private static bool ContainsExceptionDeep(LogEntry entry)
        => entry.Exception != null || entry.Children.Any(ContainsExceptionDeep);

    public static List<LogEntry> FilterByDeepExceptions(this IEnumerable<LogEntry> data) =>
        data
            .Where(ContainsExceptionDeep)
            .Select(entry =>
            {
                var newEntry = Clone(entry);
                newEntry.Children = newEntry.Children.FilterByDeepExceptions();
                return newEntry;
            })
            .ToList();

    public static LogEntry Clone(LogEntry entry) => JsonConvert.DeserializeObject<LogEntry>(JsonConvert.SerializeObject(entry));
}