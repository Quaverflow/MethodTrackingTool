using System;
using System.Collections.Concurrent;
using System.Collections.Generic;
using System.Linq;
using System.Threading.Tasks;

namespace MethodTrackerVisualizer.Helpers;

public static class LogEntryHelpers
{
    public static List<LogEntry> FindMatchingText(
           this IReadOnlyList<LogEntry> roots,
        string searchText)
    {
        if (string.IsNullOrEmpty(searchText))
        {
            return [];
        }

        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new ConcurrentBag<LogEntry>();

        Parallel.ForEach(
            Partitioner.Create(0, roots.Count),
            range =>
            {
                for (var i = range.Item1; i < range.Item2; i++)
                {
                    var stack = new Stack<LogEntry>();
                    stack.Push(roots[i]);

                    while (stack.Count > 0)
                    {
                        var e = stack.Pop();

                        if (EntryMatchesSpan(e, searchText.AsSpan(), comparison))
                        {
                            results.Add(e);
                        }

                        foreach (var c in e.Children)
                        {
                            stack.Push(c);
                        }
                    }
                }
            });

        return results.ToList();
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

        foreach (var exObj in entry.Exceptions)
        {
            if (exObj is Exception ex &&
                ex.Message.AsSpan().IndexOf(needle, comparison) >= 0)
            {
                return true;
            }
        }

        if (entry.ReturnValue is not null)
        {
            var rv = entry.ReturnValue.ToString();
            if (!string.IsNullOrEmpty(rv) &&
                rv.AsSpan().IndexOf(needle, comparison) >= 0)
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
        => entry.Exceptions.Any() || entry.Children.Any(ContainsExceptionDeep);

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

    public static LogEntry Clone(LogEntry entry) =>
        new()
        {
            MethodName = entry.MethodName,
            Exceptions = entry.Exceptions.ToArray(),
            Children = entry.Children.Select(Clone).ToList()
        };
}