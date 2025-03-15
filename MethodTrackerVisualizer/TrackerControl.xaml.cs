using System;
using System.Collections.Generic;
using System.IO;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Controls.Primitives;
using System.Windows.Media;
using System.Windows.Threading;
using Microsoft.VisualStudio.PlatformUI;
using Newtonsoft.Json;
using StepByStepLogger;

namespace MethodTrackerVisualizer
{
    /// <summary>
    /// Interaction logic for TrackerControl.
    /// </summary>
    public partial class TrackerControl : UserControl
    {
        private static readonly string FilePath = GetLogFilePath();
        private List<LogEntry> _fullLogData = new List<LogEntry>{new LogEntry()};
        private List<LogEntry> _matchedEntries = new List<LogEntry>();
        private int _currentMatchIndex = -1;

        public static string GetLogFilePath()
        {
            var folder = Path.Combine(Environment.GetFolderPath(Environment.SpecialFolder.ApplicationData), "MethodLogger");
            if (!Directory.Exists(folder))
            {
                Directory.CreateDirectory(folder);
            }
            return Path.Combine(folder, "loggeroutput.json");
        }

        /// <summary>
        /// Initializes a new instance of the <see cref="TrackerControl"/> class.
        /// </summary>
        public TrackerControl()
        {
            InitializeComponent();
            this.Loaded += LoggerToolWindowControl_Loaded;
        }

        private void LoggerToolWindowControl_Loaded(object sender, RoutedEventArgs e)
        {
            LoadLogData();
        }

        private void RefreshButton_Click(object sender, RoutedEventArgs e)
        {
            LoadLogData();
        }

        private void LoadLogData()
        {
            if (!File.Exists(FilePath))
            {
                MessageBox.Show("Log file not found at: " + FilePath);
                return;
            }
            try
            {
                string json = File.ReadAllText(FilePath);
                _fullLogData = JsonConvert.DeserializeObject<List<LogEntry>>(json, new JsonSerializerSettings
                {
                    MissingMemberHandling = MissingMemberHandling.Ignore,
                });
                LogTreeView.ItemsSource = _fullLogData;
            }
            catch (Exception ex)
            {
                MessageBox.Show("Error loading log data: " + ex.Message);
            }
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
            string searchText = SearchTextBox.Text.Trim();
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
            LogTreeView.ItemsSource = _fullLogData;
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
            ExpandAllParents(dataItem, LogTreeView);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                InvertSelection(GetTreeViewItem(LogTreeView, dataItem));
            }), DispatcherPriority.Background);
        }

        private void TreeViewItem_Selected(object sender, RoutedEventArgs e)
        {
            if (sender is TreeViewItem tvi && tvi.IsSelected)
            {
                InvertSelection(tvi);
            }
        }

        public void InvertSelection(TreeViewItem newSelection)
        {
            if (_selected != null)
            {
                _selected.Background = new SolidColorBrush(Colors.White);
                _selected.Foreground = new SolidColorBrush(Colors.Black);
            }

            if (newSelection != null)
            {
                newSelection.IsSelected = false;
                newSelection.Background = new SolidColorBrush(Colors.LightSkyBlue);
                newSelection.Foreground = new SolidColorBrush(Colors.Black);
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
                        return childTvi;
                }
            }
            return null;
        }

        // Retrieves the parent ItemsControl (if any) of a TreeViewItem.
        private ItemsControl GetParent(TreeViewItem item)
        {
            DependencyObject parent = VisualTreeHelper.GetParent(item);
            while (parent != null && !(parent is TreeViewItem) && !(parent is TreeView))
            {
                parent = VisualTreeHelper.GetParent(parent);
            }
            return parent as ItemsControl;
        }
    }
}
