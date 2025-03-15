using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using StepByStepLogger;

namespace MethodTrackerVisualizer
{
    public partial class TrackerControl : UserControl
    {
        private List<LogEntry> _fullLogData = new List<LogEntry>();
        private List<LogEntry> _matchedEntries = [];
        private int _currentMatchIndex = -1;
        public TrackerControl()
        {
            InitializeComponent();
            Loaded += TrackerControl_Loaded;
        }

        private void TrackerControl_Loaded(object sender, RoutedEventArgs e)
        {
            _fullLogData = FileHelper.LoadLogData();
            PopulateTreeViews();
        }

        private void PopulateTreeViews()
        {
            // Tab 1: Chronological Order – use the full tree as is.
            ChronoTreeView.ItemsSource = _fullLogData;

            // Tab 2: Exclusive Order – flatten and sort by ExclusiveElapsedTime descending.
            var flatList = FlattenLogEntries(_fullLogData);
            var exclusiveSorted = flatList.OrderByDescending(e =>
            {
                // Assume ExclusiveElapsedTime is like "123.45 ms"
                string text = e.ExclusiveElapsedTime.Replace(" ms", "").Trim();
                return double.TryParse(text, out double value) ? value : 0.0;
            }).ToList();
            ExclusiveListView.ItemsSource = exclusiveSorted;
        }
        private List<LogEntry> FlattenLogEntries(List<LogEntry> entries)
        {
            var flat = new List<LogEntry>();
            foreach (var entry in entries)
            {
                flat.Add(entry);
                flat.AddRange(FlattenLogEntries(entry.Children));
            }
            return flat;
        }
        private void PreviousButton_Click(object sender, RoutedEventArgs e)
        {
            if (_matchedEntries.Any())
            {
                _currentMatchIndex = (_currentMatchIndex - 1 + _matchedEntries.Count) % _matchedEntries.Count;
                NavigateToEntry(_matchedEntries[_currentMatchIndex]);
            }
        }

        private void NextButton_Click(object sender, RoutedEventArgs e)
        {
            if (_matchedEntries.Any())
            {
                _currentMatchIndex = (_currentMatchIndex + 1) % _matchedEntries.Count;
                NavigateToEntry(_matchedEntries[_currentMatchIndex]);
            }
        }


        private void SearchTextBox_TextChanged(object sender, TextChangedEventArgs e)
        {
            var searchText = SearchTextBox.Text.Trim();
            if (string.IsNullOrEmpty(searchText))
            {
                _matchedEntries.Clear();
                _currentMatchIndex = -1;
                return;
            }

            _matchedEntries = FindMatchingEntries(_fullLogData, searchText);
            if (_matchedEntries.Any())
            {
                _currentMatchIndex = 0;
                NavigateToEntry(_matchedEntries[_currentMatchIndex]);
            }
        }


        private void ClearButton_Click(object sender, RoutedEventArgs e)
        {
            SearchTextBox.Text = "";
            ChronoTreeView.ItemsSource = _fullLogData;
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
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InvertSelection(GetTreeViewItem(ChronoTreeView, dataItem));
            }), DispatcherPriority.Background);
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
            var tvi = container.ItemContainerGenerator.ContainerFromItem(dataItem) as TreeViewItem;
            if (tvi != null)
            {
                return tvi;
            }
            foreach (var item in container.Items)
            {
                var parentContainer = container.ItemContainerGenerator.ContainerFromItem(item) as TreeViewItem;
                if (parentContainer != null)
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
            while (parent != null && !(parent is TreeViewItem) && !(parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as ItemsControl;
        }
    }
}
