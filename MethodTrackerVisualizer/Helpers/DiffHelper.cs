using System;
using System.Collections.Generic;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Helpers;

public static class DiffHelper
{
    public static bool DeepEquals(object? a, object? b)
    {
        try
        {
            var aStr = JsonConvert.SerializeObject(a);
            var bStr = JsonConvert.SerializeObject(b);
            return string.Equals(aStr, bStr, StringComparison.Ordinal);
        }
        catch
        {
            return string.Equals(a?.ToString(), b?.ToString(), StringComparison.Ordinal);
        }
    }

    public static DiffNode DiffLogEntries(LogEntry? left, LogEntry? right)
    {
        var diff = new DiffNode
        {
            Left = left,
            Right = right
        };

        if (left == null)
        {
            diff.DiffType = DiffType.Added;
        }
        else if (right == null)
        {
            diff.DiffType = DiffType.Removed;
        }
        else
        {
            var methodChanged = left.MethodName != right.MethodName;
            var parametersChanged = !DeepEquals(left.Parameters, right.Parameters);
            var returnTypeChanged = left.ReturnType != right.ReturnType;
            var returnValueChanged = !DeepEquals(left.ReturnValue, right.ReturnValue);
            var exceptionsChanged = !DeepEquals(left.Exception, right.Exception);
            var hasChanged = methodChanged || parametersChanged || returnTypeChanged || returnValueChanged || exceptionsChanged;
            diff.DiffType = hasChanged ? DiffType.Modified : DiffType.Unchanged;
        }

        var leftCount = left?.Children.Count ?? 0;
        var rightCount = right?.Children.Count ?? 0;
        var maxCount = Math.Max(leftCount, rightCount);

        for (var i = 0; i < maxCount; i++)
        {
            var leftChild = (left != null && i < left.Children.Count) ? left.Children[i] : null;
            var rightChild = (right != null && i < right.Children.Count) ? right.Children[i] : null;
            var childDiff = DiffLogEntries(leftChild, rightChild);
            diff.Children.Add(childDiff);
        }

        return diff;
    }
  
    public enum DiffType
    {
        Unchanged,
        Modified,
        Added,
        Removed
    }

    public class DiffNode
    {
        public LogEntry? Left { get; set; }
        public LogEntry? Right { get; set; }
        public DiffType DiffType { get; set; }
        public List<DiffNode> Children { get; } = [];
    }
}