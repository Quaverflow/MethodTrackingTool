using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;

namespace MethodTrackerVisualizer.Components;

public partial class ChronoTree : UserControl
{
    private List<LogEntry> _matchedEntries = [];
    private int _currentMatchIndex = -1;
    public ChronoTree()
    {
        InitializeComponent();
        SearchBar.PreviousClicked += PreviousButton_Click;
        SearchBar.NextClicked += NextButton_Click;
        SearchBar.SearchTextChanged += SearchTextBox_TextChanged;
    }

    private void PreviousButton_Click(object sender, EventArgs _)
    {
        if (_matchedEntries.Any())
        {
            _currentMatchIndex = (_currentMatchIndex - 1 + _matchedEntries.Count) % _matchedEntries.Count;
            NavigateToEntry(_matchedEntries[_currentMatchIndex]);
        }
    }

    private void NextButton_Click(object sender, EventArgs _)
    {
        if (_matchedEntries.Any())
        {
            _currentMatchIndex = (_currentMatchIndex + 1) % _matchedEntries.Count;
            NavigateToEntry(_matchedEntries[_currentMatchIndex]);
        }
    }

    private void SearchTextBox_TextChanged(object sender, string searchText)
    {
        searchText = searchText.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            _matchedEntries.Clear();
            _currentMatchIndex = -1;
            return;
        }

        _matchedEntries = FindMatchingEntries(FileHelper.Data, searchText);
        if (_matchedEntries.Any())
        {
            _currentMatchIndex = 0;
            NavigateToEntry(_matchedEntries[_currentMatchIndex]);
        }
    }

    private List<LogEntry> FindMatchingEntries(List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            if (entry.MethodName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Add(entry);
            }
            result.AddRange(FindMatchingEntries(entry.Children, searchText));
        }
        return result;
    }

    private TreeViewItem _selected;
    private void NavigateToEntry(object dataItem)
    {
        ExpandAllParents(dataItem, ChronoTreeView);
        Dispatcher.BeginInvoke(new Action(() => InvertSelection(GetTreeViewItem(ChronoTreeView, dataItem))), DispatcherPriority.Background);
    }

    private static Brush _baseBackground;
    public void InvertSelection(TreeViewItem newSelection)
    {
        _baseBackground ??= newSelection?.Background;

        if (_selected != null)
        {
            _selected.Background = _baseBackground;
            _selected.IsSelected = false;
        }

        if (newSelection != null)
        {
            newSelection.IsSelected = true;
            newSelection.Background = new SolidColorBrush(Colors.DodgerBlue);
            newSelection.BringIntoView();
            _selected = newSelection;
        }
    }

    // Recursively expands all parent nodes of a data item.
    private void ExpandAllParents(object dataItem, ItemsControl container)
    {
        var tvi = GetTreeViewItem(container, dataItem);
        if (tvi != null)
        {
            tvi.IsExpanded = true;
            if (GetParent(tvi) is ItemsControl parent)
            {
                ExpandAllParents(parent.DataContext, parent);
            }
        }
    }

    // Recursively find the TreeViewItem corresponding to a data item.
    private TreeViewItem GetTreeViewItem(ItemsControl container, object dataItem)
    {
        if (container.ItemContainerGenerator.Status != GeneratorStatus.ContainersGenerated)
        {
            container.UpdateLayout();
        }

        if (container.ItemContainerGenerator.ContainerFromItem(dataItem) is TreeViewItem tvi)
        {
            return tvi;
        }
        foreach (var item in container.Items)
        {
            if (container.ItemContainerGenerator.ContainerFromItem(item) is TreeViewItem parentContainer)
            {
                parentContainer.IsExpanded = true; // Ensure children are generated.
                var childTvi = GetTreeViewItem(parentContainer, dataItem);
                if (childTvi != null)
                {
                    return childTvi;
                }
            }
        }
        return null;
    }

    // Retrieves the parent ItemsControl (if any) of a TreeViewItem.
    private ItemsControl GetParent(TreeViewItem item)
    {
        var parent = VisualTreeHelper.GetParent(item);
        while (parent is { } and not TreeViewItem and not TreeView)
        {
            parent = VisualTreeHelper.GetParent(parent);
        }
        return parent as ItemsControl;
    }
}