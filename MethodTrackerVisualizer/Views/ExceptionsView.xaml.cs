using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class ExceptionsView : UserControl
{
    private readonly List<LogEntry> _data;
    private List<LogEntry> _matchedTextEntries = [];
    private int _currentMatchTextEntriesIndex = -1;


    public string ExceptionSearchText
    {
        get => (string)GetValue(ExceptionSearchTextProperty);
        set => SetValue(ExceptionSearchTextProperty, value);
    }

    public readonly DependencyProperty ExceptionSearchTextProperty =
        DependencyProperty.Register(nameof(ExceptionSearchText), typeof(string), typeof(ExceptionsView), new PropertyMetadata(string.Empty));

    public ExceptionsView()
    {
        InitializeComponent();

        _data = FileHelper.Data.FilterByDeepExceptions();
        ExceptionsTreeView.ItemsSource = _data;
        ExceptionSearchBar.SearchTextChanged += SearchForText;
        ExceptionSearchBar.PreviousClicked += PreviousText;
        ExceptionSearchBar.NextClicked += NextText;
    }

    private void SearchForText(object _, string searchText)
    {
        ExceptionSearchText = searchText.Trim();
        if (string.IsNullOrEmpty(ExceptionSearchText))
        {
            _matchedTextEntries.Clear();
            _currentMatchTextEntriesIndex = -1;
            return;
        }

        _matchedTextEntries = _data.FindMatchingText(ExceptionSearchText);
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

        ExceptionsTreeView.ExpandAllParents(dataItem);
        Dispatcher.BeginInvoke(new Action(() =>
        {
            var tvi = ExceptionsTreeView.GetTreeViewItem(dataItem);
            tvi.ExpandExpanderForEntry();
            tvi.BringIntoView();
        }), DispatcherPriority.Background);
    }

}