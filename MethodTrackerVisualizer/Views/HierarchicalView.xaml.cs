using System;
using System.Collections.Generic;
using System.Windows;
using System.Windows.Controls;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class HierarchicalView : UserControl
{
    private List<LogEntry> _matchedTextEntries = new();
    private int _currentMatchTextEntriesIndex = -1;

    // Expose the currently selected match for binding:
    public LogEntry? CurrentMatch
    {
        get => (LogEntry?)GetValue(CurrentMatchProperty);
        set => SetValue(CurrentMatchProperty, value);
    }
    public static readonly DependencyProperty CurrentMatchProperty =
        DependencyProperty.Register(
            nameof(CurrentMatch),
            typeof(LogEntry),
            typeof(HierarchicalView),
            new PropertyMetadata(null));

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
        HierarchicalTreeView.ItemsSource =
            FileHelper.Selected?.Data ?? new List<LogEntry>();
        _matchedTextEntries.Clear();
        _currentMatchTextEntriesIndex = -1;
        CurrentMatch = null;
    }

    private void SearchForText(object _, string raw)
    {
        CurrentSearchText = raw.Trim();
        if (string.IsNullOrEmpty(CurrentSearchText))
        {
            _matchedTextEntries.Clear();
            CurrentMatch = null;
            return;
        }

        var data = FileHelper.Selected?.Data;
        if (data == null) return;

        _matchedTextEntries = data.FindMatchingText(CurrentSearchText);
        if (_matchedTextEntries.Count > 0)
        {
            _currentMatchTextEntriesIndex = 0;
            CurrentMatch = _matchedTextEntries[0];
        }
    }

    private void PreviousText(object _, EventArgs __)
    {
        if (_matchedTextEntries.Count == 0) return;
        _currentMatchTextEntriesIndex =
            (_currentMatchTextEntriesIndex - 1 + _matchedTextEntries.Count)
            % _matchedTextEntries.Count;
        CurrentMatch = _matchedTextEntries[_currentMatchTextEntriesIndex];
    }

    private void NextText(object _, EventArgs __)
    {
        if (_matchedTextEntries.Count == 0) return;
        _currentMatchTextEntriesIndex =
            (_currentMatchTextEntriesIndex + 1)
            % _matchedTextEntries.Count;
        CurrentMatch = _matchedTextEntries[_currentMatchTextEntriesIndex];
    }
}