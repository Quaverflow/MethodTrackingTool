using System;
using System.Collections.Generic;
using MethodTrackerVisualizer.Helpers;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Views;
public static class DiffHelper
{
    public static bool DeepEquals(object a, object b)
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

    public static DiffNode DiffLogEntries(LogEntry left, LogEntry right)
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
            var exceptionsChanged = !DeepEquals(left.Exceptions, right.Exceptions);

            if (methodChanged || 
                parametersChanged || 
                returnTypeChanged || 
                returnValueChanged || 
                exceptionsChanged)
            {
                diff.DiffType = DiffType.Modified;
            }
            else
            {
                diff.DiffType = DiffType.Unchanged;
            }
        }

        // Recursively compare children.
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
        public LogEntry Left { get; set; }   // null if added in right run
        public LogEntry Right { get; set; }  // null if removed in right run
        public DiffType DiffType { get; set; }
        public List<DiffNode> Children { get; } = new List<DiffNode>();
    }

}