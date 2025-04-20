using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views
{
    public partial class HierarchicalView : UserControl
    {
        private List<LogEntry> _matchedTextEntries = new();
        private int _currentMatchTextEntriesIndex = -1;

        public HierarchicalView()
        {
            InitializeComponent();
            Loaded += OnLoaded;
            // Update tree whenever the selected file changes
            FileHelper.Refresh += (_, __) => Dispatcher.Invoke(RefreshTree);
        }

        private void OnLoaded(object sender, RoutedEventArgs e)
        {
            // Initial population of the TreeView
            RefreshTree();

            // Hook up search navigation
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
        }

        private void SearchForText(object _, string searchText)
        {
            var trimmed = searchText.Trim();
            if (string.IsNullOrEmpty(trimmed))
            {
                _matchedTextEntries.Clear();
                _currentMatchTextEntriesIndex = -1;
                return;
            }

            var data = FileHelper.Selected?.Data;
            if (data == null || data.Count == 0)
                return;

            // Fast background search
            _matchedTextEntries = data
                .FindMatchingText(trimmed)
                .ToList();

            if (_matchedTextEntries.Any())
            {
                _currentMatchTextEntriesIndex = 0;
                NavigateToEntry(_matchedTextEntries[0]);
            }
        }

        private void PreviousText(object sender, EventArgs e)
        {
            if (!_matchedTextEntries.Any()) return;

            _currentMatchTextEntriesIndex =
                (_currentMatchTextEntriesIndex - 1 + _matchedTextEntries.Count)
                % _matchedTextEntries.Count;

            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }

        private void NextText(object sender, EventArgs e)
        {
            if (!_matchedTextEntries.Any()) return;

            _currentMatchTextEntriesIndex =
                (_currentMatchTextEntriesIndex + 1) % _matchedTextEntries.Count;

            NavigateToEntry(_matchedTextEntries[_currentMatchTextEntriesIndex]);
        }

        private void NavigateToEntry(LogEntry entry)
        {
            HierarchicalTreeView.ExpandAllParents(entry);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var tvi = HierarchicalTreeView.GetTreeViewItem(entry);
                tvi?.ExpandExpanderForEntry();
                tvi?.BringIntoView();
            }), DispatcherPriority.Background);
        }
    }
}