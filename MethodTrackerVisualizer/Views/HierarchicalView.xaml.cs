using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Media;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views;

public partial class HierarchicalView : UserControl
{
    private List<LogEntry> _matchedTextEntries = [];
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
        FileSystem.AllFiledDeleted += (_, _) =>
        {
            Selected = null;
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
        Dispatcher.BeginInvoke(() =>
        {
            var fileSelected = Selected != null;
            HierarchicalSearchBar.PreviousButton.IsEnabled = fileSelected;
            HierarchicalSearchBar.NextButton.IsEnabled = fileSelected;
            HierarchicalSearchBar.SearchButton.IsEnabled = fileSelected;
            HierarchicalTreeView.ItemsSource = Selected?.Data ?? [];
            _matchedTextEntries.Clear();
            _currentMatchTextEntriesIndex = -1;
        });
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

        UpdateNavButtons();
        if (_matchedTextEntries.Any())
        {
            _currentMatchTextEntriesIndex = 0;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
        else
        {
            MessageBox.Show($"No matches for the term(s): {string.Join(", ", searchText.Split('&'))}.", "Load Error", MessageBoxButton.OK, MessageBoxImage.Information);
        }
    }

    private void PreviousText(object sender, string searchText)
    {
        if (CurrentSearchText != searchText)
        {
            SearchForText(sender, searchText);
            return;
        }

        if (_currentMatchTextEntriesIndex > 0)
        {
            _currentMatchTextEntriesIndex--;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
        UpdateNavButtons();
    }

    private void NextText(object sender, string searchText)
    {
        if (CurrentSearchText != searchText)
        {
            SearchForText(sender, searchText);
            return;
        }

        if (_currentMatchTextEntriesIndex < _matchedTextEntries.Count - 1)
        {
            _currentMatchTextEntriesIndex++;
            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }
        UpdateNavButtons();
    }

    private void UpdateNavButtons()
    {
        var hasAny = _matchedTextEntries.Count > 0;
        var atFirst = _currentMatchTextEntriesIndex <= 0;
        var atLast = _currentMatchTextEntriesIndex >= _matchedTextEntries.Count - 1;

        HierarchicalSearchBar.PreviousButton.IsEnabled = hasAny && !atFirst;
        HierarchicalSearchBar.NextButton.IsEnabled = hasAny && !atLast;
    }

    public async void NavigateToEntry(LogEntry target)
    {
        var path = FindPath(Selected?.Data ?? [], target);
        if (path == null)
        {
            return;
        }

        foreach (var node in path)
        {
            HierarchicalTreeView.ExpandAllParents(node);
        }

        HierarchicalTreeView.UpdateLayout();
        await Dispatcher.Yield(DispatcherPriority.Background);
        var sv = FindVisualChild<ScrollViewer>(HierarchicalTreeView);
        if (sv == null)
        {
            BringTargetIntoView(target);
            return;
        }

        TreeViewItem? tvi = null;
        double lastOffset = -1;
        while (true)
        {
            tvi = HierarchicalTreeView.GetTreeViewItem(target);
            if (tvi != null)
            {
                break;
            }

            if (Math.Abs(sv.VerticalOffset - lastOffset) < 0.1)
            {
                break;
            }

            lastOffset = sv.VerticalOffset;
            sv.PageDown();
            await Dispatcher.Yield(DispatcherPriority.Background);
        }

        if (tvi != null)
        {
            tvi.IsSelected = true;
            tvi.ExpandExpanderForEntry();
            tvi.BringIntoView();
            tvi.Focus();
        }
        else
        {
            BringTargetIntoView(target);
        }
    }

    private void BringTargetIntoView(LogEntry target)
    {
        var tvi = HierarchicalTreeView.GetTreeViewItem(target);
        if (tvi != null)
        {
            tvi.IsSelected = true;
            tvi.ExpandExpanderForEntry();
            tvi.BringIntoView();
            tvi.Focus();
        }
    }

    private List<LogEntry>? FindPath(
        IList<LogEntry> roots,
        LogEntry target)
    {
        var stack = new List<LogEntry>();
        if (Recurse(roots, target, stack))
        {
            return stack;
        }

        return null;
    }

    private static T? FindVisualChild<T>(DependencyObject parent) where T : DependencyObject
    {
        for (var i = 0; i < VisualTreeHelper.GetChildrenCount(parent); i++)
        {
            var child = VisualTreeHelper.GetChild(parent, i);

            if (child is T typed)
            {
                return typed;
            }

            var desc = FindVisualChild<T>(child);
            if (desc != null)
            {
                return desc;
            }
        }
        return null;
    }

    private bool Recurse(
        IList<LogEntry> nodes,
        LogEntry target,
        List<LogEntry> path)
    {
        foreach (var node in nodes)
        {
            path.Add(node);
            if (ReferenceEquals(node, target))
            {
                return true;
            }

            if (Recurse(node.Children, target, path))
            {
                return true;
            }

            path.RemoveAt(path.Count - 1);
        }
        return false;
    }
}
