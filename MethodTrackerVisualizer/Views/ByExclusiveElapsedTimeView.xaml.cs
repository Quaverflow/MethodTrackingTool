﻿using System.Collections.Generic;
using System.Linq;
using System.Windows;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class ByExclusiveElapsedTimeView
{
    public ByExclusiveElapsedTimeView()
    {
        InitializeComponent();
        Loaded += Load;
    }

    public void Load(object sender, RoutedEventArgs e)
    {
        ExclusiveListView.ItemsSource = FileHelper.Selected.Data;
        PopulateTreeViews();
    }
    private void PopulateTreeViews()
    {
        var flatList = FlattenLogEntries(FileHelper.Selected.Data);
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