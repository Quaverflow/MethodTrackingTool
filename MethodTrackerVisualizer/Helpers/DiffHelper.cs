using System;
using System.Collections.Generic;
using DiffPlex.DiffBuilder.Model;
using DiffPlex.DiffBuilder;
using DiffPlex;
using System.Windows.Documents;
using System.Windows.Media;
using MethodTrackerVisualizer.Helpers;
using Newtonsoft.Json;

namespace MethodTrackerVisualizer.Views;
public static class DiffDocumentBuilder
{
    public static FlowDocument BuildDiffDocument(string leftText, string rightText)
    {
        var diffBuilder = new InlineDiffBuilder(new Differ());
        var diffResult = diffBuilder.BuildDiffModel(leftText, rightText);

        var flowDoc = new FlowDocument();
        var paragraph = new Paragraph();

        foreach (var line in diffResult.Lines)
        {
            var run = new Run(line.Text);
            run.Background = line.Type switch
            {
                ChangeType.Inserted => Brushes.LightGreen,
                ChangeType.Deleted => Brushes.LightCoral,
                ChangeType.Modified => Brushes.LightBlue,
                _ => run.Background
            };
            paragraph.Inlines.Add(run);
            paragraph.Inlines.Add(new LineBreak());
        }
        flowDoc.Blocks.Add(paragraph);
        return flowDoc;
    }
}
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
            var exclusiveElapsedChanged = left.ExclusiveElapsedTime != right.ExclusiveElapsedTime;
            var memoryIncreaseChanged = left.MemoryIncrease != right.MemoryIncrease;

            if (methodChanged || parametersChanged || returnTypeChanged || returnValueChanged ||
                exceptionsChanged|| exclusiveElapsedChanged || memoryIncreaseChanged)
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