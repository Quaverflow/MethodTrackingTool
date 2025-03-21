using System;
using System.Collections.Generic;
using System.Linq;
using System.Windows;
using System.Windows.Controls;
using System.Windows.Threading;
using System.ComponentModel;
using MethodTrackerVisualizer.Helpers;

namespace MethodTrackerVisualizer.Views
{
    public partial class ComparerPanelView : INotifyPropertyChanged
    {
        public event EventHandler FileSelectionChanged;
        public event PropertyChangedEventHandler PropertyChanged;

        public EntryFile Selected = FileHelper.Selected;
        private List<FileItem> _fileItems = new();
        private string _currentSearchText = string.Empty;

        public string CurrentSearchText
        {
            get => _currentSearchText;
            set
            {
                if (_currentSearchText != value)
                {
                    _currentSearchText = value;
                    OnPropertyChanged(nameof(CurrentSearchText));
                }
            }
        }

        public ComparerPanelView()
        {
            InitializeComponent();
            Loaded += Load;
            FileHelper.Refresh += Refresh;
            DataContext = this; // Make sure bindings work properly
        }

        private void Refresh(object sender, EventArgs eventArgs)
        {
            _fileItems = FileHelper.Data
                .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
                .OrderByDescending(x => x.Selected)
                .ToList();

            FilesDataGrid.ItemsSource = _fileItems;
            if (Selected != null)
            {
                HierarchicalTreeView.ItemsSource = Selected.Data;
            }
        }

        private void OnFileSelectionChanged() => FileSelectionChanged?.Invoke(this, EventArgs.Empty);

        public void Load(object sender, RoutedEventArgs e)
        {
            _fileItems = FileHelper.Data
                .Select(x => new FileItem { FileName = x.FileName, Updated = x.Updated, Selected = false })
                .OrderByDescending(x => x.Selected)
                .ToList();

            FilesDataGrid.ItemsSource = _fileItems;
            if (Selected != null)
            {
                HierarchicalTreeView.ItemsSource = Selected.Data;
            }
            FileSystemSearchBar.SearchTextChanged += (s, text) => CurrentSearchText = text;
        }

        private void SearchForText(object _, string searchText)
        {
            CurrentSearchText = searchText.Trim();
            FilesDataGrid.ItemsSource = string.IsNullOrEmpty(CurrentSearchText)
                ? _fileItems
                : _fileItems.Where(x => x.FileName.IndexOf(CurrentSearchText, StringComparison.CurrentCultureIgnoreCase) != -1).ToList();
        }

        private void ToggleButton_OnChecked(object sender, RoutedEventArgs e)
        {
            if (sender is CheckBox { DataContext: FileItem selectedItem } btn)
            {
                if (btn.IsChecked == true)
                {
                    foreach (var item in _fileItems)
                    {
                        if (item != selectedItem)
                        {
                            item.Selected = false;
                        }
                    }

                    selectedItem.Selected = true;
                    var selectedFile = FileHelper.Data.FirstOrDefault(x => x.FileName == selectedItem.FileName);

                    if (selectedFile != null)
                    {
                        Selected = selectedFile;
                        HierarchicalTreeView.ItemsSource = Selected.Data;
                        OnFileSelectionChanged();
                    }
                }
                else
                {
                    selectedItem.Selected = false;
                }

                FilesDataGrid.ItemsSource = null;
                FilesDataGrid.ItemsSource = _fileItems.OrderByDescending(x => x.Selected);
            }
        }

        public void NavigateToEntry(object dataItem)
        {
            HierarchicalTreeView.ExpandAllParents(dataItem);
            Dispatcher.BeginInvoke(new Action(() =>
            {
                var tvi = HierarchicalTreeView.GetTreeViewItem(dataItem);
                tvi.ExpandExpanderForEntry();
                tvi.BringIntoView();
            }), DispatcherPriority.Background);
        }

        private void OnPropertyChanged(string propertyName)
        {
            PropertyChanged?.Invoke(this, new PropertyChangedEventArgs(propertyName));
        }
    }
}
