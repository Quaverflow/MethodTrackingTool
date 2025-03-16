using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class ChronoTree : UserControl
{
    private List<LogEntry> _matchedMethods = [];
    private List<LogEntry> _matchedTextEntries = [];
    private int _currentMatchMethodIndex = -1;
    private int _currentMatchTextEntriesIndex = -1;
    public ChronoTree()
    {
        InitializeComponent();
        ChronoTreeView.ItemsSource = FileHelper.Data;
        MethodSearchBar.PreviousClicked += PreviousMethod;
        MethodSearchBar.NextClicked += NextMethod;
        MethodSearchBar.SearchTextChanged += SearchForMethod;
        ValueSearchBar.SearchTextChanged += SearchForText;
    }

    private void SearchForText(object _, string searchText)
    {
        searchText = searchText.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            _matchedTextEntries.Clear();
            _currentMatchTextEntriesIndex = -1;
            return;
        }

        _matchedTextEntries = FindMatchingText(FileHelper.Data, searchText);
        if (_matchedTextEntries.Any())
        {
            _currentMatchTextEntriesIndex = 0;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
    }
    private void PreviousText(object sender, EventArgs _)
    {
        if (_matchedTextEntries.Any())
        {
            _currentMatchTextEntriesIndex = (_currentMatchTextEntriesIndex - 1 + _matchedTextEntries.Count) % _matchedTextEntries.Count;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
    }

    private void NextText(object sender, EventArgs _)
    {
        if (_matchedTextEntries.Any())
        {
            _currentMatchTextEntriesIndex = (_currentMatchTextEntriesIndex + 1) % _matchedTextEntries.Count;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
    }
    private void PreviousMethod(object sender, EventArgs _)
    {
        if (_matchedMethods.Any())
        {
            _currentMatchMethodIndex = (_currentMatchMethodIndex - 1 + _matchedMethods.Count) % _matchedMethods.Count;
            NavigateToEntry(_matchedMethods[_currentMatchMethodIndex]);
        }
    }

    private void NextMethod(object sender, EventArgs _)
    {
        if (_matchedMethods.Any())
        {
            _currentMatchMethodIndex = (_currentMatchMethodIndex + 1) % _matchedMethods.Count;
            NavigateToEntry(_matchedMethods[_currentMatchMethodIndex]);
        }
    }

    private void SearchForMethod(object sender, string searchText)
    {
        searchText = searchText.Trim();
        if (string.IsNullOrEmpty(searchText))
        {
            _matchedMethods.Clear();
            _currentMatchMethodIndex = -1;
            return;
        }

        _matchedMethods = FindMatchingMethod(FileHelper.Data, searchText);
        if (_matchedMethods.Any())
        {
            _currentMatchMethodIndex = 0;
            NavigateToEntry(_matchedMethods[_currentMatchMethodIndex]);
        }
    }

    private List<LogEntry> FindMatchingMethod(List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            if (entry.MethodName.IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0)
            {
                result.Add(entry);
            }
            result.AddRange(FindMatchingMethod(entry.Children, searchText));
        }
        return result;
    }

    private List<LogEntry> FindMatchingText(List<LogEntry> entries, string searchText)
    {
        var result = new List<LogEntry>();
        foreach (var entry in entries)
        {
            var found = entry.Parameters?.Any(paramValue => IsMatchingValue(paramValue.Value, searchText)) is true ||
                        IsMatchingValue(entry.ReturnValue, searchText);

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

    private bool IsMatchingValue<T>(T value, string searchText) => value?.ToString().IndexOf(searchText, StringComparison.OrdinalIgnoreCase) >= 0;

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