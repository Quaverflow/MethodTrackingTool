using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;

namespace MethodTrackerVisualizer.Components;

public partial class ExclusiveList : UserControl
{
    public ExclusiveList()
    {
        InitializeComponent();
        Loaded += Load;
    }

    public void Load(object sender, RoutedEventArgs e)
    {
        ExclusiveListView.ItemsSource = FileHelper.Data;
        PopulateTreeViews();
    }
    private void PopulateTreeViews()
    {
        var flatList = FlattenLogEntries(FileHelper.Data);
        var exclusiveSorted = flatList.OrderByDescending(e =>
        {
            var text = e.ExclusiveElapsedTime.Replace(" ms", "").Trim();
            return double.TryParse(text, out var value) ? value : 0.0;
        }).ToList();
        ExclusiveListView.ItemsSource = exclusiveSorted;
    }

    private static List<LogEntry> FlattenLogEntries(List<LogEntry> entries)
    {
        var flat = new List<LogEntry>();
        foreach (var entry in entries)
        {
            flat.Add(entry);
            flat.AddRange(FlattenLogEntries(entry.Children));
        }
        return flat;
    }
}