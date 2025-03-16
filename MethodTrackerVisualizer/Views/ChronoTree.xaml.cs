using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class ChronoTree : UserControl
{
    private List<LogEntry> _matchedTextEntries = [];
    private int _currentMatchTextEntriesIndex = -1;


    public string CurrentSearchText
    {
        get => (string)GetValue(CurrentSearchTextProperty);
        set => SetValue(CurrentSearchTextProperty, value);
    }

    public static readonly DependencyProperty CurrentSearchTextProperty =
        DependencyProperty.Register(nameof(CurrentSearchText), typeof(string), typeof(ChronoTree), new PropertyMetadata(string.Empty));

    public ChronoTree()
    {
        InitializeComponent();
        ChronoTreeView.ItemsSource = FileHelper.Data;
        ValueSearchBar.SearchTextChanged += SearchForText;
        ValueSearchBar.PreviousClicked += PreviousText;
        ValueSearchBar.NextClicked += NextText;
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

        _matchedTextEntries = FileHelper.Data.FindMatchingText(CurrentSearchText);
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

    private void NavigateToEntry(object dataItem)
    {

        ChronoTreeView.ExpandAllParents(dataItem);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var tvi = ChronoTreeView.GetTreeViewItem(dataItem);
            tvi.ExpandExpanderForEntry();
            tvi.BringIntoView();
        }), DispatcherPriority.Background);
    }

}