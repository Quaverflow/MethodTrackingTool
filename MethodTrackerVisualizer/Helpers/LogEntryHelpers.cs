using System;
using System.Collections.Generic;
using System.Linq;

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

        var terms = searchText
            .Split('&')
            .Select(t => t.Trim())
            .Where(t => !string.IsNullOrEmpty(t))
            .ToList();

        var comparison = StringComparison.OrdinalIgnoreCase;
        var results = new List<LogEntry>();
        var seen = new HashSet<LogEntry>();

        foreach (var root in roots)
        {
            var stack = new Stack<LogEntry>();
            stack.Push(root);

            while (stack.Count > 0)
            {
                var e = stack.Pop();

                var isMatch = terms.Count == 1
                    ? EntryMatchesSpan(e, terms[0].AsSpan(), comparison)
                    : terms.All(term => EntryMatchesSpan(e, term.AsSpan(), comparison));

                if (isMatch && seen.Add(e))
                {
                    results.Add(e);
                }

                for (var i = e.Children.Count - 1; i >= 0; i--)
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

        if (entry.Exception is { } ex &&
            ex.Message.AsSpan().IndexOf(needle, comparison) >= 0)
        {
            return true;
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

            if (kv.Value is not null)
            {
                var vs = kv.Value.ToString();
                if (!string.IsNullOrEmpty(vs) &&
                    vs.AsSpan().IndexOf(needle, comparison) >= 0)
                {
                    return true;
                }
            }
        }

        return false;
    }
}