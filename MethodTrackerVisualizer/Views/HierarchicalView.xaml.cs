using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class HierarchicalView : UserControl
{
    private List<LogEntry> _matchedTextEntries = new();
    private int _currentMatchTextEntriesIndex = -1;
    public EntryFile? Selected;
    public string CurrentSearchText
    {
        get => (string)GetValue(CurrentSearchTextProperty);
        set => SetValue(CurrentSearchTextProperty, value);
    }
    public static readonly DependencyProperty CurrentSearchTextProperty =
        DependencyProperty.Register(
            nameof(CurrentSearchText),
            typeof(string),
            typeof(HierarchicalView),
            new PropertyMetadata(string.Empty));

    public HierarchicalView()
    {
        InitializeComponent();
        Loaded += OnLoaded;
        FileSystem.FileSelectionChanged += (_, value) =>
        {
            Selected = value;
            RefreshTree();
        };
        FileHelper.Refresh += (_, __) => RefreshTree();
    }

    private void OnLoaded(object sender, RoutedEventArgs e)
    {
        RefreshTree();
        HierarchicalSearchBar.SearchTextChanged += SearchForText;
        HierarchicalSearchBar.PreviousClicked += PreviousText;
        HierarchicalSearchBar.NextClicked += NextText;
    }

    private void RefreshTree()
    {
        HierarchicalTreeView.ItemsSource = Selected?.Data ?? [];
        _matchedTextEntries.Clear();
        _currentMatchTextEntriesIndex = -1;
    }

    private void SearchForText(object _, string searchText)
    {
        CurrentSearchText = searchText.Trim();
        if (string.IsNullOrEmpty(CurrentSearchText))
        {
            _matchedTextEntries.Clear();
            _currentMatchTextEntriesIndex = -1;
            return;
        }

        _matchedTextEntries = Selected?.Data.FindMatchingText(CurrentSearchText) ?? [];
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

    public void NavigateToEntry(LogEntry dataItem)
    {
        HierarchicalTreeView.ExpandAllParents(dataItem);
        HierarchicalTreeView.UpdateLayout();
        Dispatcher.Invoke(() => { }, DispatcherPriority.Background);
        var tvi = HierarchicalTreeView.GetTreeViewItem(dataItem);
        if (tvi == null)
        {
            return;
        }

        tvi.IsSelected = true;
        tvi.ExpandExpanderForEntry();
        tvi.BringIntoView();
        tvi.Focus();
    }
}